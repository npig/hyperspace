using System;
using System.Reflection;
using UnityEngine;

namespace Hyperspace 
{
	/// <summary>
	/// A simple wrapper around an event type that handles the generic implementation.
	/// Handles notifications when the event fires, is blocked, or unblocked.
	/// Is also able to Emit simple event types (with either no Init() or an Init() with no parameters)
	/// </summary>
	public abstract class EventWrapper
	{
		public event Action<Event> OnEvent;
		public event Action OnBlocked;
		public event Action OnUnblocked;

		public abstract void Bind();
		public abstract void Unbind();
		public abstract bool IsBlocked();
		public abstract void EmitEvent();

		// Can we emit this event via EmitEvent()?
		// For this to be true, the event type must have no Init() method, or the method must have no parameters and return the event type
		public bool CanEmit { get; protected set; }

		/// <summary>
		/// Create a wrapper around an event type.
		/// </summary>
		/// <param name="eventType">The event type to wrap</param>
		/// <returns>A wrapper, or null if an error occurs</returns>
		public static EventWrapper CreateEventWrapper(Type eventType)
		{
			if (!IsValidTypeToWrap(eventType))
			{
				Debug.LogError("Incorrect type for event type (does not inherit from EventBus.Event)");
				return null;
			}

			var wrapperGenericType = typeof(EventWrapperImplementation<>);
			var wrapperTyped = wrapperGenericType.MakeGenericType(eventType);
			return (EventWrapper)Activator.CreateInstance(wrapperTyped);
		}

		/// <summary>
		/// Simple check to make sure that the type is actually a valid event type
		/// </summary>
		public static bool IsValidTypeToWrap(Type eventType)
		{
			return eventType != null && eventType.IsSubclassOf(typeof(Event));
		}


		#region Generic Implementation
		private class EventWrapperImplementation<TEventType> : EventWrapper where TEventType : Event
		{
			private bool hasInit;
			private MethodInfo eventInitMethod;

			public EventWrapperImplementation()
			{
				if (!IsValidTypeToWrap(typeof(TEventType)))
					throw new Exception("Invalid event type for wrapper. Events Init function is too complex. (Must be no params, or no init method)");

				eventInitMethod = typeof(TEventType).GetMethod("Init", BindingFlags.Public | BindingFlags.Instance);
				CanEmit = eventInitMethod == null || (eventInitMethod.GetParameters().Length == 0 && eventInitMethod.ReturnType == typeof(TEventType));

				if (!CanEmit)
					eventInitMethod = null;
				else
					hasInit = eventInitMethod != null;
			}

			public override void Bind()
			{
				Engine.Events.Subscribe<TEventType>(OnEventReceived);
				Engine.Events.OnEventBlocked += OnAnyBlocked;
				Engine.Events.OnEventUnblocked += OnAnyUnblocked;
			}

			public override void Unbind()
			{
				Engine.Events.Unsubscribe<TEventType>(OnEventReceived);
				Engine.Events.OnEventBlocked -= OnAnyBlocked;
				Engine.Events.OnEventUnblocked -= OnAnyUnblocked;
			}

			public override bool IsBlocked()
			{
				return Engine.Events.IsEventBlocked<TEventType>();
			}

			public override void EmitEvent()
			{
				if (!CanEmit)
					return;

				if (!hasInit)
					Emit_NoInit();
				else
					Emit_WithInit();
			}

			private void Emit_NoInit()
			{
				Engine.Events.Emit<TEventType>(ObjectPool.Get<TEventType>());
			}

			private void Emit_WithInit()
			{
				var pooled = ObjectPool.Get<TEventType>();
				pooled = (TEventType)eventInitMethod.Invoke(pooled, null);
				Engine.Events.Emit<TEventType>(pooled);
			}

			private void OnEventReceived(TEventType evt)
			{
				OnEvent?.Invoke(evt);
			}

			private void OnAnyBlocked(Type eventType)
			{
				if (eventType == typeof(TEventType))
					OnBlocked?.Invoke();
			}

			private void OnAnyUnblocked(Type eventType)
			{
				if (eventType == typeof(TEventType))
					OnUnblocked?.Invoke();
			}
		}
		#endregion
	}
}