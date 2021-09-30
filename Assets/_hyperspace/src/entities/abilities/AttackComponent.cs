using UnityEngine;

namespace Hyperspace.Entities
{
   public abstract class AttackComponent
   {
       public abstract void Activate();
       
       public abstract float GetCooldown { get; internal set; }
   
       protected void Fire(float speed)
       {
           Debug.Log("Moving with speed " + speed + "!(速度)");
       }
   
       protected void PlaySound(string coolSound)
       {
           Debug.Log("Playing sound " + coolSound+"!(音效)");
       }
   
       protected void SpawnParticles(string particles)
       {
           Debug.Log("Spawn Particles "+ particles+"!(粒子特效)");
       }
   } 
}
