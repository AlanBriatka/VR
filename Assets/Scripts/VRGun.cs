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
    [SerializeField] private Transform stockPoint;
    [SerializeField] private XRGrabInteractable secondaryGrip;
    
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

    [Header("Virtual Stock")]
    [SerializeField] private bool useVirtualStock = true;
    [SerializeField] private float stockThreshold = 0.25f;
    [SerializeField] private float stockSmoothing = 10f;
    
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
    private bool isTwoHanded;
    private Camera mainCamera;
    
    public bool HasChamberedRound => chamberedRound;
    public bool HasMagazine => currentMagazine != null;
    public int AmmoCount => currentMagazine != null ? currentMagazine.CurrentAmmo : 0;
    
    private void Awake()
    {
        CacheComponents();
        InitializeSlide();
        ValidateSetup();
        mainCamera = Camera.main;
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

        if (secondaryGrip != null)
        {
            secondaryGrip.selectEntered.AddListener(OnSecondaryGrabbed);
            secondaryGrip.selectExited.AddListener(OnSecondaryReleased);
        }
    }
    
    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.activated.RemoveListener(OnTriggerPressed);
            grabInteractable.deactivated.RemoveListener(OnTriggerReleased);
        }

        if (secondaryGrip != null)
        {
            secondaryGrip.selectEntered.RemoveListener(OnSecondaryGrabbed);
            secondaryGrip.selectExited.RemoveListener(OnSecondaryReleased);
        }
    }
    
    private void Update()
    {
        if (!isInitialized) return;
        
        UpdateState();
        HandleTrigger();
        UpdateSlideAnimation();
        ApplyStabilization();
        UpdateVirtualStock();
    }

    private void UpdateVirtualStock()
    {
        if (!useVirtualStock || mainCamera == null || !grabInteractable.isSelected) return;
        if (stockPoint == null) return;

        Vector3 shoulderPos = mainCamera.transform.position + mainCamera.transform.right * 0.15f - mainCamera.transform.up * 0.2f;
        float distanceToShoulder = Vector3.Distance(stockPoint.position, shoulderPos);

        if (distanceToShoulder < stockThreshold)
        {
            Vector3 targetDir = (muzzlePoint.position - shoulderPos).normalized;
            Quaternion targetRot = Quaternion.LookRotation(targetDir, mainCamera.transform.up);

            // We apply a gentle correction to the rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * stockSmoothing);
        }
    }

    private void ApplyStabilization()
    {
        if (isTwoHanded && rb != null && !rb.isKinematic)
        {
            // Increase angular drag to stabilize aim when using two hands
            rb.angularDamping = 10f;
        }
        else if (rb != null && !rb.isKinematic)
        {
            rb.angularDamping = 0.05f;
        }
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

    private void OnSecondaryGrabbed(SelectEnterEventArgs args)
    {
        isTwoHanded = true;
    }

    private void OnSecondaryReleased(SelectExitEventArgs args)
    {
        isTwoHanded = false;
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
        SendFireHaptics();
    }

    private void SendFireHaptics()
    {
        if (grabInteractable != null && grabInteractable.isSelected)
        {
            foreach (var interactor in grabInteractable.interactorsSelecting)
            {
                bool isLeft = interactor.transform.name.ToLower().Contains("left");
                HapticsUtility.SendHapticImpulse(0.5f, 0.1f, isLeft ? HapticsUtility.Controller.Left : HapticsUtility.Controller.Right);
            }
        }
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
        
        if (ImpactManager.Instance != null)
        {
            ImpactManager.Instance.PlayImpact(hit.point, hit.normal, hit.collider.tag, hit.transform);
        }
        else
        {
            SpawnBulletHole(hit);
        }
        
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

        PhysicsLOD lod = FindFirstObjectByType<PhysicsLOD>();
        if (lod != null) lod.RegisterRigidbody(shellRb);
        
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
        
        float recoilMult = isTwoHanded ? (1f - gunData.twoHandedRecoilReduction) : 1f;

        Vector3 recoilDirection = -muzzlePoint.forward;
        Vector3 upwardKick = muzzlePoint.up * gunData.recoilTorque * 0.5f;
        
        rb.AddForce((recoilDirection + upwardKick) * gunData.recoilForce * recoilMult, ForceMode.Impulse);
        rb.AddTorque(muzzlePoint.right * gunData.recoilTorque * recoilMult, ForceMode.Impulse);
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
