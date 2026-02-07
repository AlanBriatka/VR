using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;

[RequireComponent(typeof(XRGrabInteractable))]
public class GunSlide : MonoBehaviour
{
    [Header("Required References")]
    [SerializeField] private VRGun gun;
    [SerializeField] private Transform slideTransform;
    
    [Header("Slide Mechanics")]
    [SerializeField] private float maxPullDistance = 0.08f;
    [SerializeField] private float releaseThreshold = 0.06f;
    [SerializeField] private bool requireFullPullToRack = true;
    [SerializeField] private float returnSpeed = 15f;
    
    [Header("Haptic Feedback")]
    [SerializeField] private float rackHapticIntensity = 0.5f;
    [SerializeField] private float rackHapticDuration = 0.1f;
    
    private XRGrabInteractable grabInteractable;
    private Vector3 slideOriginalPosition;
    private Vector3 grabStartPosition;
    private bool isGrabbed;
    private bool hasRacked;
    private float currentPullDistance;
    private bool isLeftController;
    
    private void Awake()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        
        if (slideTransform != null)
        {
            slideOriginalPosition = slideTransform.localPosition;
        }
        else if (gun != null)
        {
            Debug.LogWarning($"[{name}] Slide Transform not assigned.", this);
        }
    }
    
    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }
    
    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        hasRacked = false;
        grabStartPosition = transform.position;
        
        isLeftController = DetermineControllerSide(args.interactorObject);
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        SendHapticImpulse(rackHapticIntensity * 0.3f, rackHapticDuration * 0.5f);
    }
    
    private void Update()
    {
        if (isGrabbed)
        {
            UpdateSlidePosition();
            CheckForRack();
        }
        else
        {
            ReturnSlideToRest();
        }
    }
    
    private void UpdateSlidePosition()
    {
        if (slideTransform == null || gun == null) return;
        
        Vector3 currentPosition = transform.position;
        Vector3 pullDirection = -gun.transform.forward;
        
        currentPullDistance = Vector3.Dot(currentPosition - grabStartPosition, pullDirection);
        currentPullDistance = Mathf.Clamp(currentPullDistance, 0f, maxPullDistance);
        
        slideTransform.localPosition = slideOriginalPosition + Vector3.back * currentPullDistance;
    }
    
    private void CheckForRack()
    {
        if (hasRacked) return;
        
        float threshold = requireFullPullToRack ? maxPullDistance * 0.95f : releaseThreshold;
        
        if (currentPullDistance >= threshold)
        {
            hasRacked = true;
            PerformRack();
        }
    }
    
    private void PerformRack()
    {
        if (gun != null)
        {
            gun.RackSlide();
        }
        
        SendHapticImpulse(rackHapticIntensity, rackHapticDuration);
    }
    
    private void ReturnSlideToRest()
    {
        if (slideTransform == null) return;
        
        slideTransform.localPosition = Vector3.Lerp(
            slideTransform.localPosition,
            slideOriginalPosition,
            Time.deltaTime * returnSpeed
        );
        
        currentPullDistance = Mathf.Lerp(currentPullDistance, 0f, Time.deltaTime * returnSpeed);
    }
    
    private bool DetermineControllerSide(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRInteractor interactor)
    {
        string interactorName = interactor.transform.name.ToLower();
        return interactorName.Contains("left");
    }
    
    private void SendHapticImpulse(float intensity, float duration)
    {
        if (!isGrabbed && grabInteractable.interactorsSelecting.Count == 0) return;
        
        HapticsUtility.Controller controller = isLeftController 
            ? HapticsUtility.Controller.Left 
            : HapticsUtility.Controller.Right;
        
        HapticsUtility.SendHapticImpulse(intensity, duration, controller);
    }
    
    private void OnValidate()
    {
        if (gun == null)
        {
            gun = GetComponentInParent<VRGun>();
        }
    }
}

