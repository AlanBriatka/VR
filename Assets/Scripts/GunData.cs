using UnityEngine;

[CreateAssetMenu(fileName = "GunData", menuName = "VR Combat/Gun Data")]
public class GunData : ScriptableObject
{
    [Header("Ballistics")]
    [Tooltip("Damage dealt per shot")]
    public float damage = 30f;
    
    [Tooltip("Maximum effective range in meters")]
    public float range = 100f;
    
    [Tooltip("Minimum time between shots")]
    public float fireRate = 0.15f;
    
    [Header("Recoil")]
    [Tooltip("Backward force applied when firing")]
    public float recoilForce = 3f;
    
    [Tooltip("Upward rotational kick")]
    public float recoilTorque = 2f;
    
    [Header("Slide Mechanics")]
    [Tooltip("How far the slide moves back when firing")]
    public float slideKickbackDistance = 0.05f;
    
    [Tooltip("Speed at which slide returns to rest position")]
    public float slideReturnSpeed = 20f;
    
    [Header("Shell Ejection")]
    [Tooltip("Force applied to ejected shell casings")]
    public float shellEjectionForce = 3f;
    
    [Tooltip("Direction shells eject in local space")]
    public Vector3 shellEjectionDirection = new Vector3(1, 1, 0);
    
    [Tooltip("Random rotational force on shells")]
    public float shellEjectionTorque = 10f;
    
    [Tooltip("How long before shells despawn")]
    public float shellLifetime = 5f;
}
