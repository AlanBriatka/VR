using UnityEngine;
using System.Collections;

public class MagazineReleaseButton : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GunMagazine magazine;
    [SerializeField] private VRGun gun;
    [SerializeField] private Transform buttonTransform;
    
    [Header("Button Settings")]
    [SerializeField] private float pressDepth = 0.01f;
    [SerializeField] private float pressSpeed = 10f;
    [SerializeField] private float cooldownTime = 0.3f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip clickSound;
    
    [Header("Detection")]
    [SerializeField] private LayerMask triggerLayers = -1;
    
    private Vector3 buttonOriginalPosition;
    private float currentPressAmount;
    private bool isOnCooldown;
    private AudioSource audioSource;
    private WaitForSeconds cooldownWait;
    
    private void Awake()
    {
        InitializeButton();
        SetupAudioSource();
    }
    
    private void InitializeButton()
    {
        if (buttonTransform != null)
        {
            buttonOriginalPosition = buttonTransform.localPosition;
        }
        else
        {
            buttonOriginalPosition = transform.localPosition;
            buttonTransform = transform;
        }
    }
    
    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.spatialBlend = 1.0f;
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Linear;
    }
    
    private void Start()
    {
        if (gun != null && magazine == null)
        {
            magazine = gun.GetComponent<GunMagazine>();
        }

        cooldownWait = new WaitForSeconds(cooldownTime);
    }
    
    private void Update()
    {
        AnimateButton();
    }
    
    private void AnimateButton()
    {
        currentPressAmount = Mathf.Lerp(currentPressAmount, 0f, Time.deltaTime * pressSpeed);
        
        if (buttonTransform != null)
        {
            Vector3 pressOffset = Vector3.forward * (currentPressAmount * pressDepth);
            buttonTransform.localPosition = buttonOriginalPosition - pressOffset;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (isOnCooldown) return;
        
        if (IsValidTrigger(other))
        {
            Press();
        }
    }
    
    private bool IsValidTrigger(Collider other)
    {
        int layer = other.gameObject.layer;
        return ((1 << layer) & triggerLayers) != 0;
    }
    
    public void Press()
    {
        if (isOnCooldown) return;
        
        StartCoroutine(PerformPressSequence());
    }
    
    private IEnumerator PerformPressSequence()
    {
        isOnCooldown = true;
        currentPressAmount = 1f;
        
        PlayClickSound();
        EjectMagazine();
        
        yield return cooldownWait;
        
        isOnCooldown = false;
    }
    
    private void EjectMagazine()
    {
        if (magazine != null && magazine.IsInserted)
        {
            magazine.Eject();
        }
        else if (gun != null)
        {
            Debug.Log($"[{name}] No magazine to eject from gun.", this);
        }
    }
    
    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }
    
    private void OnValidate()
    {
        if (magazine == null && gun == null)
        {
            Debug.LogWarning($"[{name}] Neither Magazine nor Gun reference is assigned.", this);
        }
    }
}
