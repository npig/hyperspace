using System;
using System.Collections.Generic;
using System.Reflection;

namespace Hyperspace
{
	/// <summary>
	/// A token used for blocking events. To create one call CreateBlockerToken(...).
	/// These are just a weak reference used for blocking events. A single token can be used to block multiple events.
	/// If you wish to remove the blocker earlier than the lifespan of the owning object, you should store the token and call Revoke().
	///		(This will of course remove the blocker from all of the events that it is blocking)
	///	You can also just use the stored token to call the normal EventBus.RemoveBlocker(...) to remove the blocker from a single event.
	///	An event can be blocked from multiple sources at a time and will not fire until they are all cleared.
	/// </summary>
	public class EventBlockerToken
	{
		private System.WeakReference tokenHolder = null;
		public bool WasRevoked { get; private set; } = false;

		/// <summary>
		/// Token is still valid if it hasn't been Revoked and the owning object still exists.
		/// </summary>
		public bool IsStillValid
		{
			get
			{
				if (tokenHolder == null)
					return false;

				if (!tokenHolder.IsAlive)
					return false;

				if (tokenHolder.Target is UnityEngine.Object)
				{
					return (tokenHolder.Target as UnityEngine.Object) != null;
				}

				return tokenHolder.Target != null;
			}
		}

		/// <summary>
		/// Use CreateBlockerToken instead.
		/// </summary>
		private EventBlockerToken() { }

		/// <summary>
		/// Create a new blocker using the blockerObject as our reference.
		/// If blocker object is garbage collected, the blocker will no longer function and will be cleaned up automatically.
		/// </summary>
		/// <param name="blockerObject">The reference to use to maintain validity of this token</param>
		/// <returns>A new event blocker token</returns>
		public static EventBlockerToken CreateBlockerToken(object blockerObject)
		{
			UnityEngine.Assertions.Assert.IsNotNull(blockerObject);

			EventBlockerToken token = new EventBlockerToken();
			token.tokenHolder = new System.WeakReference(blockerObject);
			return token;
		}

		/// <summary>
		/// Cancel this blocker token.
		/// All blocked events will no longer be blocked by this, and will remove this token when fired.
		/// </summary>
		public void Revoke()
		{
			tokenHolder = null;
			WasRevoked = true;
		}
	}
	
	public abstract class EventBinding
	{
		public abstract bool HasAnyPersistentListeners();

		public abstract void ClearNonPersistentListeners();

		public abstract void ClearAllBlockers();
	}
	
	/// <summary>
	/// Extend this to create different event types
	/// </summary>
	public class Event { }

	/// <summary>
	/// Extend this to define feature blocks.
	/// These are events that should never be emitted, or subscribed to, but can be used to block or check if blocked.
	/// </summary>
	public class FeatureBlock : Event { }	
	
	/// <summary>
	/// The main event bus of the app.
	/// Supports Subscribing and Unsubscribing from events.
	/// Also supports blocking events using EventBlockerTokens.
	/// To fire an event use Emit(...) i.e.
	///      Engine.Events.Emit<SomeEvent>(Pool.GetItem<SomeEvent>().Init(...));
	/// </summary>
	public sealed class EventBus : EngineService 
	{
		#region Event Binding Class

		// Dummy concrete base class


		public class WeakEventHandler<TEventArgs>
		{
			private readonly WeakReference targetReference;
			private readonly MethodInfo method;

			private object[] cachedEvtWrapper = null;

			public bool IsValid
			{
				get
				{
					if(targetReference.Target is UnityEngine.Object)
					{
						return (targetReference.Target as UnityEngine.Object) != null;
					}
					return targetReference.IsAlive && targetReference.Target != null;
				}
			}

			public WeakEventHandler(Action<TEventArgs> callback)
			{
				method = callback.Method;
				targetReference = new WeakReference(callback.Target, false);
			}

			public void Invoke(TEventArgs e)
			{
				bool isAlive = targetReference.IsAlive;
				
				if(isAlive && targetReference.Target is UnityEngine.Object)
				{
					isAlive = (targetReference.Target as UnityEngine.Object) != null;
				}
				
				if (isAlive)
				{
					if (cachedEvtWrapper == null)
						cachedEvtWrapper = new object[] { e };
					else
						cachedEvtWrapper[0] = e;

					method.Invoke(targetReference.Target, cachedEvtWrapper);

					cachedEvtWrapper[0] = null;
				}

			}

			public bool IsSameAs(Action<TEventArgs> eventHandler)
			{
				if(eventHandler.Method != method)
				{
					return false;
				}
				if(eventHandler.Target is UnityEngine.Object || targetReference.Target is UnityEngine.Object)
				{
					return (eventHandler.Target as UnityEngine.Object) == (targetReference.Target as UnityEngine.Object);
				}
				return eventHandler.Target == targetReference.Target;
			}
		}

		// Stores the event listeners and blockers for a particular event type
		private class EventBinding<TEventType> : EventBinding where TEventType : Event
		{
			public List<WeakEventHandler<TEventType>> Listeners = new List<WeakEventHandler<TEventType>>();
			public List<Action<TEventType>> PersistentListeners = new List<Action<TEventType>>();
			public HashSet<EventBlockerToken> Blockers = new HashSet<EventBlockerToken>();

			public void Invoke(TEventType evt)
			{
				if (IsBlocked())
					return;
			
				foreach (var pListener in PersistentListeners)
					pListener.Invoke(evt);

				for (int i = 0; i < Listeners.Count; ++i)
				{
					if (!Listeners[i].IsValid)
					{
						//UnityEngine.Debug.Log("Cleanup listener: " + i);
						Listeners.RemoveAt(i);
						--i;
						continue;
					}
					Listeners[i].Invoke(evt);
				}
			}

			public override bool HasAnyPersistentListeners() => PersistentListeners.Count > 0;

			public override void ClearNonPersistentListeners()
			{
				Listeners.Clear();
			}

			public override void ClearAllBlockers()
			{
				Blockers.Clear();
			}

			public bool IsBlocked()
			{
				// @TODO REVIEW | Should this early out or do we want to check for dead blockers?
				bool bIsBlocked = false;
				foreach (var blocker in Blockers)
				{
					if (blocker.IsStillValid)
					{
						bIsBlocked = true;
					}
					else
					{
						// Check if blocker was GCd without being revoked by user code
						UnityEngine.Assertions.Assert.IsTrue(blocker.WasRevoked, ($"EventBus Blocker for event of type '{typeof(TEventType).Name}' was GCd! this should be revoked manually!"));
					}
				}

				// Cleanup dead blockers
				Blockers.RemoveWhere((blocker) => !blocker.IsStillValid);

				return bIsBlocked;
			}
		}

		#endregion Event Binding Class

		private Dictionary<Type, EventBinding> eventBindings = new Dictionary<Type, EventBinding>();

		public delegate void OnEventBlockedDelegate(Type eventType);
		public delegate void OnEventUnblockedDelegate(Type eventType);

		public event OnEventBlockedDelegate	  OnEventBlocked;
		public event OnEventUnblockedDelegate OnEventUnblocked;

		// Cleanup the bindings to remove any hanging assets
		public void OnShutdown()
		{
			ClearAllEventBindings();
		}

		private void ClearAllEventBindings()
		{
			eventBindings.Clear();
		}

		public void ClearScenarioEventBindings()
		{
			foreach (var binding in eventBindings)
			{
				binding.Value.ClearNonPersistentListeners();
			}
		}

		/// <summary>
		/// Subscribe to a particular event type for the duration of a scenario.
		/// NOTE: Events bound this way are cleared when a scenario is loaded. For a binding that survives longer than this, use SubscribePersistent<>().
		/// </summary>
		/// <typeparam name="TEventType">The type to receive calls from</typeparam>
		/// <param name="eventHandler">The callback that handles this event</param>
		public void Subscribe<TEventType>(Action<TEventType> eventHandler) where TEventType : Event
		{
			if (eventBindings.TryGetValue(typeof(TEventType), out var binding))
			{
				(binding as EventBinding<TEventType>).Listeners.Add(new WeakEventHandler<TEventType>(eventHandler));
			}
			else
			{
				var newBinding = new EventBinding<TEventType>();
				newBinding.Listeners.Add(new WeakEventHandler<TEventType>(eventHandler));
				eventBindings.Add(typeof(TEventType), newBinding);
			}
		}

		/// <summary>
		/// Subscribe to a particular event type.
		/// NOTE: Events bound this way are -NOT- cleared when a scenario is loaded, and will need to be unsubscribed from (using UnsubscribePersistent) if you wish to stop this event.
		///       For a binding that only survives during a scenario, use the general Subscribe<>() function.
		/// </summary>
		/// <typeparam name="TEventType">The type to receive calls from</typeparam>
		/// <param name="eventHandler">The callback that handles this event</param>
		public void SubscribePersistent<TEventType>(Action<TEventType> eventHandler) where TEventType : Event
		{
			if (eventBindings.TryGetValue(typeof(TEventType), out var binding))
			{
				(binding as EventBinding<TEventType>).PersistentListeners.Add(eventHandler);
			}
			else
			{
				var newBinding = new EventBinding<TEventType>();
				newBinding.PersistentListeners.Add(eventHandler);
				eventBindings.Add(typeof(TEventType), newBinding);
			}
		}

		/// <summary>
		/// Stop receiving events from a particular type.
		/// NOTE: Only use this if you registered the event listener using Subscribe<>(). For events registered with SubscribePersistent<>(), use UnsubscribePersistent<>().
		/// </summary>
		/// <typeparam name="TEventType">The type to no-longer receive events from</typeparam>
		/// <param name="eventHandler">The callback to stop receiving the event</param>
		public void Unsubscribe<TEventType>(Action<TEventType> eventHandler) where TEventType : Event
		{
			if (eventBindings.TryGetValue(typeof(TEventType), out var binding))
			{
				var b = binding as EventBinding<TEventType>;
				for (int i = 0; i < b.Listeners.Count; ++i)
				{
					var list = b.Listeners[i];
					if (list.IsSameAs(eventHandler))
					{
						b.Listeners.RemoveAt(i);
						return;
					}
				}
			}
		}

		/// <summary>
		/// Stop receiving events from a particular type.
		/// NOTE: Only use this if you registered the event listener using SubscribePersistent<>(). For events registered with the normal Subscribe<>(), use Unsubscribe<>().
		/// </summary>
		/// <typeparam name="TEventType">The type to no-longer receive events from</typeparam>
		/// <param name="eventHandler">The callback to stop receiving the event</param>
		public void UnsubscribePersistent<TEventType>(Action<TEventType> eventHandler) where TEventType : Event
		{
			if (eventBindings.TryGetValue(typeof(TEventType), out var binding))
			{
				(binding as EventBinding<TEventType>).PersistentListeners.Remove(eventHandler);
			}
		}

		/// <summary>
		/// Block an event type from firing
		/// </summary>
		/// <typeparam name="TEventType">The type to block</typeparam>
		/// <param name="blocker">A <seealso cref="EventBlockerToken">blocker token</seealso> to use to control the block </param>
		public void AddBlocker<TEventType>(EventBlockerToken blocker) where TEventType : Event
		{
			if (eventBindings.TryGetValue(typeof(TEventType), out var binding))
			{
				var eventBinding = (binding as EventBinding<TEventType>);
				bool wasBlocked = eventBinding.IsBlocked();

				eventBinding.Blockers.Add(blocker);

				if (!wasBlocked)
					OnEventBlocked?.Invoke(typeof(TEventType));
			}
			else
			{
				var newBinding = new EventBinding<TEventType>();
				newBinding.Blockers.Add(blocker);
				eventBindings.Add(typeof(TEventType), newBinding);
				
				OnEventBlocked?.Invoke(typeof(TEventType));
			}
		}

		/// <summary>
		/// Allow an event type to resume firing
		/// </summary>
		/// <typeparam name="TEventType">The type to unblock</typeparam>
		/// <param name="blocker">A <seealso cref="EventBlockerToken">blocker token</seealso> that has been used to block this event</param>
		public void RemoveBlocker<TEventType>(EventBlockerToken blocker) where TEventType : Event
		{
			if (eventBindings.TryGetValue(typeof(TEventType), out var binding))
			{
				var eventBinding = (binding as EventBinding<TEventType>);
				eventBinding.Blockers.Remove(blocker);

				if (!eventBinding.IsBlocked())
					OnEventUnblocked?.Invoke(typeof(TEventType));
			}
		}

		/// <summary>
		/// Simple check to see if an event type is currently blocked
		/// </summary>
		/// <typeparam name="TEventType">The event type</typeparam>
		/// <returns>True if blocked</returns>
		public bool IsEventBlocked<TEventType>() where TEventType : Event
		{
			if (eventBindings.TryGetValue(typeof(TEventType), out var binding))
			{
				return (binding as EventBinding<TEventType>).IsBlocked();
			}

			return false;
		}

		/// <summary>
		/// Fire off an event of a given type.
		/// Note: If this event has been blocked, the actual event will not fire.
		/// </summary>
		/// <typeparam name="TEventType">The event type</typeparam>
		/// <param name="evt">The event payload</param>
		public void Emit<TEventType>(TEventType evt) where TEventType : Event
		{
			if (eventBindings.TryGetValue(evt.GetType(), out var binding))
				(binding as EventBinding<TEventType>).Invoke(evt);
		}
	}
}