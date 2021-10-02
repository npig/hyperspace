using UnityEngine;

public class Projectile : MonoBehaviour 
{
    public void Update()
    {
        transform.position += transform.forward * .1f;
    }
}
