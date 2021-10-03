using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Hyperspace
{
    public abstract class EngineService
    {
        public virtual UniTaskVoid OnInitialised()
        {
            return default;
        }
        
        public virtual void OnTick(){}
        public virtual void OnShutdown(){}
        public virtual void OnPauseStateChanged(bool isPaused) {}
    }
}