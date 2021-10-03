//-------------------------------------------------------------------------------------
//	SkyLaunch.cs
//-------------------------------------------------------------------------------------

namespace Hyperspace.Entities
{
    public class GravityWell : AttackComponent
    {
        public override float GetCooldown { get; internal set; } = 0.5f;

        public override void Activate()
        {
            Fire(10f);
            PlaySound("SkyLaunch");
            SpawnParticles("SkyLaunch Particles");
        }
    }
}