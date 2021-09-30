using System;
using System.Collections.Generic;

namespace Hyperspace
{
	public class ObjectPool
	{
		public class ObjectPoolGroup
		{
			// @TODO Push this to a per-type config, or use a growth formula
			private const int ALLOC_COUNT = 1;

			private Type itemType;
			private List<object> allocatedItems = new List<object>(0);
			private Stack<object> freeItems = new Stack<object>();


			public ObjectPoolGroup(Type itemType, int capacity = 0)
			{
				this.itemType = itemType;
				if (capacity > 0)
					Allocate(capacity);
			}

			public object Get()
			{
				if (freeItems.Count == 0)
				{
					Allocate(ALLOC_COUNT);
				}

				return freeItems.Pop();
			}

			public void Release(object obj)
			{
				freeItems.Push(obj);
			}

			public void CleanUp()
			{
				freeItems.Clear();
				allocatedItems.Clear();
			}

			private void Allocate(int num)
			{
				allocatedItems.Capacity += num;
				for (int i = 0; i < num; ++i)
				{
					var obj = Activator.CreateInstance(itemType);
					allocatedItems.Add(obj);
					freeItems.Push(obj);
				}
			}
		}



		private static Dictionary<Type, ObjectPoolGroup> objectGroupLookup = new Dictionary<Type, ObjectPoolGroup>();


		public static void Allocate(Type t, int capacity)
		{
			if (objectGroupLookup.ContainsKey(t))
				return;

			ObjectPoolGroup group = new ObjectPoolGroup(t, capacity);
			objectGroupLookup.Add(t, group);
		}

		public static T Get<T>()
		{
			Type type = typeof(T);
			ObjectPoolGroup group;
			if (!objectGroupLookup.TryGetValue(type, out group))
			{
				group = new ObjectPoolGroup(type);
				objectGroupLookup.Add(type, group);
			}

			return (T)group.Get();
		}

		public static object GetByType(Type type)
		{
			ObjectPoolGroup group;
			if (!objectGroupLookup.TryGetValue(type, out group))
			{
				group = new ObjectPoolGroup(type);
				objectGroupLookup.Add(type, group);
			}

			return group.Get();
		}

		public static void Release(object obj)
		{
			ObjectPoolGroup group;
			if (objectGroupLookup.TryGetValue(obj.GetType(), out group))
				group.Release(obj);
		}
	}
}
