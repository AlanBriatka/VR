using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class PhysicsWeapon : MonoBehaviour
{
    [Header("Combat Settings")]
    public float bladeDamageMultiplier = 15.0f;
    public float handleDamageMultiplier = 3.0f;
    public float minimumVelocityForDamage = 1.5f;
    public LayerMask damageableLayers;
    
    [Header("Blade Settings")]
    public Transform bladeStart;
    public Transform bladeEnd;
    public Collider bladeCollider;
    
    [Header("Advanced Combat")]
    public bool allowStabbing = true;
    public float stabDamageMultiplier = 2.0f;
    public float stabAngleThreshold = 30f;
    
    [Header("Audio")]
    public AudioClip[] hitSounds;
    public AudioClip[] slashSounds;
    
    private Rigidbody rb;
    private XRGrabInteractable grabInteractable;
    private Vector3 previousPosition;
    private Vector3 velocity;
    private Vector3 angularVelocityVec;
    private AudioSource audioSource;
    private float lastHitTime;
    private const float hitCooldown = 0.1f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1.0f;
        audioSource.maxDistance = 20f;
    }
    
    private void Start()
    {
        previousPosition = transform.position;
    }
    
    private void FixedUpdate()
    {
        velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
        previousPosition = transform.position;
        angularVelocityVec = rb.angularVelocity;
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & damageableLayers) == 0)
            return;
        
        if (Time.time - lastHitTime < hitCooldown)
            return;
        
        float speed = velocity.magnitude;
        
        if (speed < minimumVelocityForDamage)
            return;
        
        bool isBlade = collision.collider == bladeCollider || 
                       (bladeCollider != null && collision.GetContact(0).thisCollider == bladeCollider);
        
        float baseDamage = speed * (isBlade ? bladeDamageMultiplier : handleDamageMultiplier);
        
        bool isStab = false;
        if (allowStabbing && isBlade && bladeStart != null && bladeEnd != null)
        {
            Vector3 bladeDirection = (bladeEnd.position - bladeStart.position).normalized;
            Vector3 movementDirection = velocity.normalized;
            float angle = Vector3.Angle(bladeDirection, movementDirection);
            
            if (angle < stabAngleThreshold)
            {
                isStab = true;
                baseDamage *= stabDamageMultiplier;
            }
        }
        
        float angularBonus = angularVelocityVec.magnitude * 0.5f;
        float totalDamage = baseDamage + angularBonus;
        
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null)
        {
            Vector3 hitPoint = collision.contacts[0].point;
            Vector3 hitDirection = velocity.normalized;
            
            DamageType damageType = isStab ? DamageType.Stab : DamageType.Slash;
            damageable.TakeDamage(totalDamage, hitPoint, hitDirection, damageType);
        }
        
        SendHitHaptics(isBlade, speed);

        if (ImpactManager.Instance != null)
        {
            ContactPoint contact = collision.GetContact(0);
            ImpactManager.Instance.PlayImpact(contact.point, contact.normal, collision.gameObject.tag, collision.transform);
        }
        else
        {
            PlayHitSound(isBlade);
        }
        lastHitTime = Time.time;
        
        string hitType = isStab ? "STAB" : (isBlade ? "slash" : "pommel");
        Debug.Log($"[{hitType}] {collision.gameObject.name} - Damage: {totalDamage:F1} (Speed: {speed:F1})");
    }
    
    private void SendHitHaptics(bool isBlade, float speed)
    {
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            float intensity = Mathf.Clamp01((speed / 10f) * (isBlade ? 1f : 0.5f));
            float duration = isBlade ? 0.08f : 0.12f;

            foreach (var interactor in grabInteractable.interactorsSelecting)
            {
                bool isLeft = interactor.transform.name.ToLower().Contains("left");
                HapticsUtility.SendHapticImpulse(intensity, duration, isLeft ? HapticsUtility.Controller.Left : HapticsUtility.Controller.Right);
            }
        }
    }

    private void PlayHitSound(bool isBlade)
    {
        if (hitSounds != null && hitSounds.Length > 0)
        {
            AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.PlayOneShot(clip, 0.7f);
        }
    }
    
    public void PlaySlashSound()
    {
        if (slashSounds != null && slashSounds.Length > 0 && velocity.magnitude > 5f)
        {
            AudioClip clip = slashSounds[Random.Range(0, slashSounds.Length)];
            audioSource.PlayOneShot(clip, 0.3f);
        }
    }
}

public enum DamageType
{
    Slash,
    Stab,
    Blunt,
    Bullet
}
