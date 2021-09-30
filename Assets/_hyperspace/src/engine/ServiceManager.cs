using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Hyperspace 
{
    public static class ServiceManager
    {
        private const string SERVICE_MANAGER_NAME = "Service: Manager";
        private static bool hasBeenCreated = false;
        private static Dictionary<Type, EngineService> serviceCache = new Dictionary<Type, EngineService>();
        private static List<EngineService> tickQueue = new List<EngineService>();
        private static ServiceEventListener instance;
        
        public static ServiceEventListener Initialise()
        {
            // hasBeenCreated stops instance from being recreated after cleanup
            if (instance == null)
            {
                instance = new GameObject(SERVICE_MANAGER_NAME).AddComponent<ServiceEventListener>();
                instance.OnUpdateAction += DoUpdateTick;
                instance.OnDestroyAction += OnDestroy;
            }
            
            return instance;
        }
        
        private static void DoUpdateTick()
        {
            for (int i = 0; i < tickQueue.Count; ++i)
                tickQueue[i].OnTick();
        }
        
        public static T GetService<T>() where T : EngineService
        {
            // ServiceManager container = Get();
            EngineService service = GetFromCache(typeof(T));
            if (service == null)
            {
                return CreateService<T>();
            }
            return service as T;
        }

        public static bool HasService<T>() where T : EngineService
        {
            Type type = typeof(T);
            return serviceCache != null && serviceCache.ContainsKey(type);
        }

        public static T CreateService<T>() where T : EngineService
        {
            T service = InstanceFactory.CreateInstance(typeof(T)) as T;
            service.OnInitialised();
            AddService(service);
            return service;
        }
        
        public static T CreateService<T, TArg1>(TArg1 arg1) where T : EngineService
        {
            T service = InstanceFactory.CreateInstance<TArg1>(typeof(T), arg1) as T;
            service.OnInitialised();
            AddService(service);
            return service;
        }
        
        private static void AddService(EngineService service)
        {
            Debug.Log($"Add service to cache: {service.GetType().Name}");
            serviceCache.Add(service.GetType(), service);
            
            MethodInfo m = service.GetType().GetMethod("OnTick");
            if (m.GetBaseDefinition().DeclaringType != m.DeclaringType)
            {
                Debug.Log($"Add service to tick queue: {service.GetType().Name}");
                tickQueue.Add(service);
            }
        }

        private static EngineService GetFromCache(Type t)
        {
            EngineService service;
            if (serviceCache.TryGetValue(t, out service))
            {
                return service;
            }
            return null;
        }

        private static void OnDestroy()
        {
            if (instance == null)
            {
                instance.OnUpdateAction -= DoUpdateTick;
                instance.OnDestroyAction -= OnDestroy;
            }
            
            foreach (var serviceKVP in serviceCache)
            {
                serviceKVP.Value.OnShutdown();
            }
        }
    }
}