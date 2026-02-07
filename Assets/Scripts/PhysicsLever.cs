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

    private HingeJoint hinge;
    private bool isActivated;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
    }

    private void Update()
    {
        float normalizedPos = GetNormalizedPosition();

        if (!isActivated && normalizedPos > threshold)
        {
            isActivated = true;
            onActivated?.Invoke();
            PlayClickSound(true);
        }
        else if (isActivated && normalizedPos < (1f - threshold))
        {
            isActivated = false;
            onDeactivated?.Invoke();
            PlayClickSound(false);
        }
    }

    private float GetNormalizedPosition()
    {
        float limitsRange = hinge.limits.max - hinge.limits.min;
        if (limitsRange <= 0) return 0;
        return (hinge.angle - hinge.limits.min) / limitsRange;
    }

    private void PlayClickSound(bool active)
    {
        // Add haptics if grabbed
        var grab = GetComponent<XRGrabInteractable>();
        if (grab.isSelected)
        {
            foreach (var interactor in grab.interactorsSelecting)
            {
                bool isLeft = interactor.transform.name.ToLower().Contains("left");
                HapticsUtility.SendHapticImpulse(0.4f, 0.05f, isLeft ? HapticsUtility.Controller.Left : HapticsUtility.Controller.Right);
            }
        }
    }
}
