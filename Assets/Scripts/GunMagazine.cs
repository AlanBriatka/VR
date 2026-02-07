using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using System.Collections;

[RequireComponent(typeof(XRGrabInteractable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class GunMagazine : MonoBehaviour
{
    [Header("Magazine Configuration")]
    [SerializeField] private int maxAmmo = 15;
    [SerializeField] private int startingAmmo = 15;
    
    [Header("Insertion Settings")]
    [SerializeField] private VRGun targetGun;
    [SerializeField] private float insertionDistance = 0.1f;
    [SerializeField] private float insertionSnapDistance = 0.05f;
    [SerializeField] private float insertionAngleThreshold = 45f;
    
    [Header("Ejection Settings")]
    [SerializeField] private float ejectForce = 2f;
    [SerializeField] private Vector3 ejectDirection = Vector3.down;
    
    [Header("Audio")]
    [SerializeField] private AudioClip insertSound;
    [SerializeField] private AudioClip ejectSound;
    
    private XRGrabInteractable grabInteractable;
    private Rigidbody rb;
    private AudioSource audioSource;
    
    private int currentAmmo;
    private bool isInserted;
    private Transform originalParent;
    private Transform magazineWell;
    
    public bool IsInserted => isInserted;
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => maxAmmo;
    public bool HasAmmo() => currentAmmo > 0;
    
    private void Awake()
    {
        CacheComponents();
        Initialize();
    }
    
    private void CacheComponents()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        
        audioSource.spatialBlend = 1.0f;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }
    
    private void Initialize()
    {
        currentAmmo = startingAmmo;
        originalParent = transform.parent;
        
        if (targetGun != null)
        {
            magazineWell = targetGun.GetMagazineWell();
        }
    }
    
    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }
    
    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
    
    private void Update()
    {
        if (isInserted || grabInteractable.isSelected) return;
        
        CheckForInsertion();
    }
    
    private void CheckForInsertion()
    {
        if (targetGun == null || magazineWell == null) return;
        
        float distance = Vector3.Distance(transform.position, magazineWell.position);
        
        if (distance < insertionDistance && IsCorrectOrientation())
        {
            StartCoroutine(InsertWithDelay());
        }
    }
    
    private bool IsCorrectOrientation()
    {
        if (magazineWell == null) return true;
        
        float angle = Vector3.Angle(transform.up, magazineWell.up);
        return angle < insertionAngleThreshold;
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        if (isInserted) return;
        
        if (targetGun != null && magazineWell != null)
        {
            float distance = Vector3.Distance(transform.position, magazineWell.position);
            
            if (distance < insertionSnapDistance && IsCorrectOrientation())
            {
                StartCoroutine(InsertWithDelay());
            }
        }
    }
    
    private IEnumerator InsertWithDelay()
    {
        yield return new WaitForFixedUpdate();
        
        if (!isInserted)
        {
            Insert();
        }
    }
    
    private void Insert()
    {
        if (isInserted || targetGun == null || magazineWell == null) return;
        
        isInserted = true;
        
        transform.SetParent(magazineWell);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.None;
        grabInteractable.enabled = false;
        
        targetGun.AttachMagazine(this);
        
        PlaySound(insertSound);
    }
    
    public void Eject()
    {
        if (!isInserted) return;
        
        isInserted = false;
        
        transform.SetParent(originalParent);
        
        rb.isKinematic = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        grabInteractable.enabled = true;
        
        if (targetGun != null)
        {
            targetGun.DetachMagazine();
        }
        
        ApplyEjectionForce();
        PlaySound(ejectSound);
    }
    
    private void ApplyEjectionForce()
    {
        if (magazineWell != null)
        {
            Vector3 worldEjectDir = magazineWell.TransformDirection(ejectDirection.normalized);
            rb.AddForce(worldEjectDir * ejectForce, ForceMode.Impulse);
        }
        else
        {
            rb.AddForce(ejectDirection * ejectForce, ForceMode.Impulse);
        }
    }
    
    public void ConsumeRound()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
        }
    }
    
    public void Refill()
    {
        currentAmmo = maxAmmo;
    }
    
    public void SetTargetGun(VRGun gun)
    {
        targetGun = gun;
        if (gun != null)
        {
            magazineWell = gun.GetMagazineWell();
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    private void OnValidate()
    {
        currentAmmo = Mathf.Clamp(currentAmmo, 0, maxAmmo);
        startingAmmo = Mathf.Clamp(startingAmmo, 0, maxAmmo);
    }
}
