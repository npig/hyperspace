using System;
using UnityEngine;

namespace Hyperspace
{
    public class ServiceEventListener : MonoBehaviour
    {
        public Action OnUpdateAction;
        public Action OnDestroyAction;
        
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Update()
        {
            OnUpdateAction?.Invoke();
        }

        private void OnDestroy()
        {
            OnDestroyAction?.Invoke();
        }
    }
}