using UnityEngine;

namespace Hyperspace
{
    public abstract class EngineService
    {
        public virtual void OnInitialised(){}
        public virtual void OnTick(){}
        public virtual void OnShutdown(){}
        public virtual void OnPauseStateChanged(bool isPaused) {}
    }
}