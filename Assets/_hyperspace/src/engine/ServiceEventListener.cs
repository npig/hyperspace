using System;
using UnityEngine;

namespace Hyperspace
{
    public class ServiceEventListener : MonoBehaviour
    {
        public Action OnUpdateAction;
        public Action OnDestroyAction;
        public Action OnFixedUpdateAction;

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

        private void FixedUpdate()
        {
            OnFixedUpdateAction?.Invoke();
        }
    }
}