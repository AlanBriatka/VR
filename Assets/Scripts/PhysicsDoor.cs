using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
[RequireComponent(typeof(XRGrabInteractable))]
public class PhysicsDoor : MonoBehaviour
{
    [SerializeField] private float autoCloseForce = 1f;
    [SerializeField] private bool autoClose = false;

    private HingeJoint hinge;
    private Rigidbody rb;
    private XRGrabInteractable grab;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint>();
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        // Ensure physics settings are correct for VR interaction
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void FixedUpdate()
    {
        if (autoClose && !grab.isSelected)
        {
            float angle = hinge.angle;
            if (Mathf.Abs(angle) > 1f)
            {
                rb.AddRelativeTorque(Vector3.up * (-angle * autoCloseForce));
            }
        }
    }
}
