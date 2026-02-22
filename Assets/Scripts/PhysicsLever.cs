using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
[RequireComponent(typeof(XRGrabInteractable))]
public class PhysicsLever : MonoBehaviour
{
    [SerializeField] private float threshold = 0.8f;
    public UnityEvent onActivated;
    public UnityEvent onDeactivated;

    [Header("Audio")]
    [SerializeField] private AudioClip activateSound;
    [SerializeField] private AudioClip deactivateSound;

    private HingeJoint hinge;
    private bool isActivated;
    private AudioSource audioSource;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        float normalizedPos = GetNormalizedPosition();

        if (!isActivated && normalizedPos > threshold)
        {
            isActivated = true;
            onActivated?.Invoke();
            HandleActivation(true);
        }
        else if (isActivated && normalizedPos < (1f - threshold))
        {
            isActivated = false;
            onDeactivated?.Invoke();
            HandleActivation(false);
        }
    }

    private float GetNormalizedPosition()
    {
        float limitsRange = hinge.limits.max - hinge.limits.min;
        if (limitsRange <= 0) return 0;
        return (hinge.angle - hinge.limits.min) / limitsRange;
    }

    private void HandleActivation(bool active)
    {
        if (active && activateSound != null) audioSource.PlayOneShot(activateSound);
        if (!active && deactivateSound != null) audioSource.PlayOneShot(deactivateSound);

        // Add haptics if grabbed
        var grab = GetComponent<XRGrabInteractable>();
        if (grab != null && grab.isSelected)
        {
            foreach (var interactor in grab.interactorsSelecting)
            {
                bool isLeft = interactor.transform.name.ToLower().Contains("left");
                HapticsUtility.SendHapticImpulse(0.4f, 0.05f, isLeft ? HapticsUtility.Controller.Left : HapticsUtility.Controller.Right);
            }
        }
    }
}
