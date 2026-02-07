using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HolsterSlot : MonoBehaviour
{
    [SerializeField] private string weaponTag = "Gun";
    [SerializeField] private float snapDistance = 0.15f;
    [SerializeField] private float snapSmoothing = 15f;
    [SerializeField] private AudioClip holsterSound;
    [SerializeField] private AudioClip unholsterSound;

    private IXRInteractable currentWeapon;
    private AudioSource audioSource;
    private Transform originalWeaponParent;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (currentWeapon != null) return;

        if (other.CompareTag(weaponTag))
        {
            var interactable = other.GetComponentInParent<IXRInteractable>();
            if (interactable != null && !interactable.isSelected)
            {
                Holster(interactable);
            }
        }
    }

    private void Update()
    {
        if (currentWeapon != null)
        {
            // Keep weapon snapped to holster
            Transform weaponTransform = ((MonoBehaviour)currentWeapon).transform;
            weaponTransform.position = Vector3.Lerp(weaponTransform.position, transform.position, Time.deltaTime * snapSmoothing);
            weaponTransform.rotation = Quaternion.Slerp(weaponTransform.rotation, transform.rotation, Time.deltaTime * snapSmoothing);

            // Check if player grabbed it
            if (currentWeapon.isSelected)
            {
                Unholster();
            }
        }
    }

    private void Holster(IXRInteractable interactable)
    {
        currentWeapon = interactable;
        MonoBehaviour weaponMB = (MonoBehaviour)interactable;

        originalWeaponParent = weaponMB.transform.parent;
        weaponMB.transform.SetParent(transform);

        Rigidbody rb = weaponMB.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (holsterSound != null) audioSource.PlayOneShot(holsterSound);

        // Haptic feedback
        HapticsUtility.SendHapticImpulse(0.3f, 0.1f, HapticsUtility.Controller.Left);
        HapticsUtility.SendHapticImpulse(0.3f, 0.1f, HapticsUtility.Controller.Right);
    }

    private void Unholster()
    {
        if (currentWeapon == null) return;

        MonoBehaviour weaponMB = (MonoBehaviour)currentWeapon;
        weaponMB.transform.SetParent(originalWeaponParent);

        Rigidbody rb = weaponMB.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        if (unholsterSound != null) audioSource.PlayOneShot(unholsterSound);
        currentWeapon = null;
    }
}
