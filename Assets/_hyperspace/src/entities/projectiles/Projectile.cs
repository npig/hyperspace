using Photon.Bolt;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private const float PROJECTILE_SPEED = 14;
    private Vector3? _craftVelocty = null;
    public void Update()
    {
        Vector3 projVel = transform.forward * PROJECTILE_SPEED * BoltNetwork.FrameDeltaTime;
        Vector3 craftVel = _craftVelocty.Value * BoltNetwork.FrameDeltaTime;
        transform.position += projVel + craftVel;
    }

    public void SetVelocity(Vector3 craftStateAcceleration)
    {
        _craftVelocty = craftStateAcceleration;
    }
}
