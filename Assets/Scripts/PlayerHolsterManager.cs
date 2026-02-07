using UnityEngine;

public class PlayerHolsterManager : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float heightOffset = -0.5f;
    [SerializeField] private float rotationSmoothing = 5f;

    [Header("Slots")]
    [SerializeField] private Transform rightHip;
    [SerializeField] private Transform leftHip;
    [SerializeField] private Transform back;

    private void Start()
    {
        if (cameraTransform == null) cameraTransform = Camera.main.transform;
    }

    private void Update()
    {
        if (cameraTransform == null) return;

        // Position at waist level
        Vector3 targetPos = cameraTransform.position;
        targetPos.y += heightOffset;
        transform.position = targetPos;

        // Only rotate on Y axis to match head direction but stay horizontal
        Vector3 forward = cameraTransform.forward;
        forward.y = 0;
        if (forward.sqrMagnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(forward.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSmoothing);
        }
    }
}
