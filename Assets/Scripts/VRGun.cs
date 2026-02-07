using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class VRGun : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private GunData gunData;
    
    [Header("Physical Parts (Required)")]
    [SerializeField] private Transform slideTransform;
    [SerializeField] private Transform ejectionPort;
    [SerializeField] private Transform muzzlePoint;
    
    [Header("Magazine System")]
    [SerializeField] private Transform magazineWell;
    private GunMagazine currentMagazine;
    
    [Header("Collision Detection")]
    [SerializeField] private LayerMask damageableLayers = -1;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private GameObject bulletHolePrefab;
    [SerializeField] private GameObject shellPrefab;
    
    [Header("Audio Clips")]
    [SerializeField] private AudioClip fireSound;
    [SerializeField] private AudioClip emptySound;
    [SerializeField] private AudioClip slideRackSound;
    
    private enum GunState { Ready, Empty, NoMagazine }
    
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private AudioSource audioSource;
    private GunState currentState = GunState.NoMagazine;
    
    private bool triggerPressed;
    private bool chamberedRound;
    private float nextFireTime;
    
    private Vector3 slideOriginalPosition;
    private float currentSlidePosition;
    
    private bool isInitialized;
    
    public bool HasChamberedRound => chamberedRound;
    public bool HasMagazine => currentMagazine != null;
    public int AmmoCount => currentMagazine != null ? currentMagazine.CurrentAmmo : 0;
    
    private void Awake()
    {
        CacheComponents();
        InitializeSlide();
        ValidateSetup();
    }
    
    private void CacheComponents()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        audioSource.spatialBlend = 1.0f;
        audioSource.maxDistance = 50f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }
    
    private void InitializeSlide()
    {
        if (slideTransform != null)
        {
            slideOriginalPosition = slideTransform.localPosition;
        }
    }
    
    private void ValidateSetup()
    {
        if (gunData == null)
        {
            Debug.LogError($"[{name}] GunData is not assigned! Create a GunData asset.", this);
            enabled = false;
            return;
        }
        
        if (ejectionPort == null)
        {
            Debug.LogWarning($"[{name}] Ejection Port not assigned. Using transform root.", this);
            ejectionPort = transform;
        }
        
        if (muzzlePoint == null)
        {
            Debug.LogWarning($"[{name}] Muzzle Point not assigned. Using ejection port.", this);
            muzzlePoint = ejectionPort;
        }
        
        if (magazineWell == null)
        {
            Debug.LogWarning($"[{name}] Magazine Well not assigned. Magazine insertion may not work.", this);
        }
        
        isInitialized = true;
    }
    
    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.AddListener(OnTriggerPressed);
            grabInteractable.deactivated.AddListener(OnTriggerReleased);
        }
    }
    
    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.RemoveListener(OnTriggerPressed);
            grabInteractable.deactivated.RemoveListener(OnTriggerReleased);
        }
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        UpdateState();
        HandleTrigger();
        UpdateSlideAnimation();
    }
    
    private void UpdateState()
    {
        if (currentMagazine == null)
        {
            currentState = GunState.NoMagazine;
        }
        else if (!chamberedRound && !currentMagazine.HasAmmo())
        {
            currentState = GunState.Empty;
        }
        else
        {
            currentState = GunState.Ready;
        }
    }
    
    private void HandleTrigger()
    {
        if (triggerPressed && Time.time >= nextFireTime)
        {
            AttemptFire();
        }
    }
    
    private void UpdateSlideAnimation()
    {
        if (slideTransform == null) return;
        
        currentSlidePosition = Mathf.Lerp(
            currentSlidePosition, 
            0f, 
            Time.deltaTime * gunData.slideReturnSpeed
        );
        
        slideTransform.localPosition = slideOriginalPosition + Vector3.back * currentSlidePosition;
    }
    
    private void OnTriggerPressed(ActivateEventArgs args)
    {
        triggerPressed = true;
    }
    
    private void OnTriggerReleased(DeactivateEventArgs args)
    {
        triggerPressed = false;
    }
    
    private void AttemptFire()
    {
        switch (currentState)
        {
            case GunState.Ready:
                if (chamberedRound)
                {
                    Fire();
                }
                else
                {
                    PlayDryFire();
                }
                break;
                
            case GunState.Empty:
            case GunState.NoMagazine:
                PlayDryFire();
                break;
        }
    }
    
    private void Fire()
    {
        if (!chamberedRound) return;
        
        nextFireTime = Time.time + gunData.fireRate;
        chamberedRound = false;
        
        PerformFireSequence();
        
        if (currentMagazine != null && currentMagazine.HasAmmo())
        {
            currentMagazine.ConsumeRound();
            chamberedRound = true;
        }
    }
    
    private void PerformFireSequence()
    {
        PlayFireSound();
        AnimateSlideKickback();
        EjectShell();
        ApplyRecoil();
        CastBulletRay();
        SpawnMuzzleFlash();
    }
    
    private void CastBulletRay()
    {
        Vector3 origin = muzzlePoint.position;
        Vector3 direction = muzzlePoint.forward;
        
        if (Physics.Raycast(origin, direction, out RaycastHit hit, gunData.range, damageableLayers))
        {
            ProcessHit(hit, direction);
        }
    }
    
    private void ProcessHit(RaycastHit hit, Vector3 shootDirection)
    {
        IDamageable damageable = hit.collider.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(gunData.damage, hit.point, shootDirection, DamageType.Bullet);
        }
        
        SpawnBulletHole(hit);
        
        Debug.DrawLine(muzzlePoint.position, hit.point, Color.yellow, 1f);
    }
    
    private void AnimateSlideKickback()
    {
        currentSlidePosition = gunData.slideKickbackDistance;
    }
    
    private void EjectShell()
    {
        if (shellPrefab == null || ejectionPort == null) return;
        
        GameObject shell = TrySpawnFromPool() ?? InstantiateShell();
        ConfigureShellPhysics(shell);
    }
    
    private GameObject TrySpawnFromPool()
    {
        if (ObjectPool.Instance != null)
        {
            GameObject shell = ObjectPool.Instance.Spawn("Shell", ejectionPort.position, ejectionPort.rotation);
            if (shell != null)
            {
                ObjectPool.Instance.Despawn("Shell", shell, gunData.shellLifetime);
                return shell;
            }
        }
        return null;
    }
    
    private GameObject InstantiateShell()
    {
        GameObject shell = Instantiate(shellPrefab, ejectionPort.position, ejectionPort.rotation);
        Destroy(shell, gunData.shellLifetime);
        return shell;
    }
    
    private void ConfigureShellPhysics(GameObject shell)
    {
        Rigidbody shellRb = shell.GetComponent<Rigidbody>();
        if (shellRb == null)
        {
            shellRb = shell.AddComponent<Rigidbody>();
            shellRb.mass = 0.01f;
            shellRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        
        Vector3 ejectionDir = ejectionPort.TransformDirection(gunData.shellEjectionDirection.normalized);
        Vector3 randomOffset = Random.insideUnitSphere * 0.5f;
        
        shellRb.linearVelocity = Vector3.zero;
        shellRb.angularVelocity = Vector3.zero;
        shellRb.AddForce((ejectionDir + randomOffset) * gunData.shellEjectionForce, ForceMode.Impulse);
        shellRb.AddTorque(Random.insideUnitSphere * gunData.shellEjectionTorque, ForceMode.Impulse);
    }
    
    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab == null) return;
        
        GameObject flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Destroy(flash, 0.1f);
    }
    
    private void SpawnBulletHole(RaycastHit hit)
    {
        if (bulletHolePrefab == null) return;
        
        GameObject hole = Instantiate(
            bulletHolePrefab, 
            hit.point + hit.normal * 0.001f, 
            Quaternion.LookRotation(hit.normal)
        );
        hole.transform.SetParent(hit.transform);
        Destroy(hole, 10f);
    }
    
    private void ApplyRecoil()
    {
        if (rb == null || !grabInteractable.isSelected) return;
        
        Vector3 recoilDirection = -muzzlePoint.forward;
        Vector3 upwardKick = muzzlePoint.up * gunData.recoilTorque * 0.5f;
        
        rb.AddForce((recoilDirection + upwardKick) * gunData.recoilForce, ForceMode.Impulse);
        rb.AddTorque(muzzlePoint.right * gunData.recoilTorque, ForceMode.Impulse);
    }
    
    public void RackSlide()
    {
        PlaySlideRackSound();
        AnimateSlideKickback();
        
        if (currentMagazine != null && currentMagazine.HasAmmo())
        {
            if (!chamberedRound)
            {
                chamberedRound = true;
            }
            else
            {
                EjectShell();
                chamberedRound = true;
            }
        }
        else
        {
            chamberedRound = false;
        }
    }
    
    public void AttachMagazine(GunMagazine magazine)
    {
        if (currentMagazine != null)
        {
            Debug.LogWarning($"[{name}] Magazine already inserted!", this);
            return;
        }
        
        currentMagazine = magazine;
    }
    
    public void DetachMagazine()
    {
        currentMagazine = null;
    }
    
    public Transform GetMagazineWell()
    {
        return magazineWell;
    }
    
    private void PlayFireSound()
    {
        if (fireSound != null)
        {
            audioSource.PlayOneShot(fireSound, 1.0f);
        }
    }
    
    private void PlayDryFire()
    {
        if (emptySound != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(emptySound, 0.5f);
        }
    }
    
    private void PlaySlideRackSound()
    {
        if (slideRackSound != null)
        {
            audioSource.PlayOneShot(slideRackSound, 0.8f);
        }
    }
    
    private void OnValidate()
    {
        if (gunData == null)
        {
            Debug.LogWarning($"[{name}] GunData not assigned. Assign a GunData asset in the inspector.", this);
        }
    }
}
