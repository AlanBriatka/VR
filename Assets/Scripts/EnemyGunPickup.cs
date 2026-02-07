using UnityEngine;

public class EnemyGunPickup : MonoBehaviour
{
    [Header("Hand Attachment")]
    [SerializeField] private Transform rightHandBone;
    [SerializeField] private Transform gunAttachPoint;
    
    [Header("Pickup Settings")]
    [SerializeField] private float pickupRange = 3f;
    [SerializeField] private LayerMask gunLayer = -1;
    [SerializeField] private string gunTag = "Gun";
    
    [Header("Gun Offset (Adjust in Inspector)")]
    [SerializeField] private Vector3 gunPositionOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 gunRotationOffset = new Vector3(0f, 0f, 0f);
    
    private VRGun equippedGun;
    private Transform gunTransform;
    private Rigidbody gunRigidbody;
    
    public bool HasGun => equippedGun != null;
    public VRGun EquippedGun => equippedGun;
    
    private void Awake()
    {
        CreateGunAttachPoint();
    }
    
    private void CreateGunAttachPoint()
    {
        if (rightHandBone == null)
        {
            Debug.LogError($"[{name}] Right hand bone not assigned!", this);
            return;
        }
        
        if (gunAttachPoint == null)
        {
            GameObject attachObj = new GameObject("GunAttachPoint");
            gunAttachPoint = attachObj.transform;
            gunAttachPoint.SetParent(rightHandBone);
            gunAttachPoint.localPosition = gunPositionOffset;
            gunAttachPoint.localRotation = Quaternion.Euler(gunRotationOffset);
        }
    }
    
    public void TryPickupNearbyGun()
    {
        if (HasGun) return;
        
        VRGun nearestGun = FindNearestGun();
        if (nearestGun != null)
        {
            PickupGun(nearestGun);
        }
    }
    
    private VRGun FindNearestGun()
    {
        GameObject[] gunObjects = GameObject.FindGameObjectsWithTag(gunTag);
        VRGun nearestGun = null;
        float nearestDistance = pickupRange;
        
        foreach (GameObject gunObj in gunObjects)
        {
            float distance = Vector3.Distance(transform.position, gunObj.transform.position);
            if (distance < nearestDistance)
            {
                VRGun gun = gunObj.GetComponent<VRGun>();
                if (gun != null && !IsGunHeldByPlayer(gun))
                {
                    nearestGun = gun;
                    nearestDistance = distance;
                }
            }
        }
        
        return nearestGun;
    }
    
    private bool IsGunHeldByPlayer(VRGun gun)
    {
        var grabInteractable = gun.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        return grabInteractable != null && grabInteractable.isSelected;
    }
    
    private void PickupGun(VRGun gun)
    {
        equippedGun = gun;
        gunTransform = gun.transform;
        gunRigidbody = gun.GetComponent<Rigidbody>();
        
        if (gunRigidbody != null)
        {
            gunRigidbody.isKinematic = true;
            gunRigidbody.useGravity = false;
        }
        
        var grabInteractable = gun.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = false;
        }
        
        gunTransform.SetParent(gunAttachPoint);
        gunTransform.localPosition = Vector3.zero;
        gunTransform.localRotation = Quaternion.identity;
        
        Debug.Log($"[{name}] Picked up gun: {gun.name}");
    }
    
    public void DropGun()
    {
        if (!HasGun) return;
        
        gunTransform.SetParent(null);
        
        if (gunRigidbody != null)
        {
            gunRigidbody.isKinematic = false;
            gunRigidbody.useGravity = true;
        }
        
        var grabInteractable = equippedGun.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }
        
        Debug.Log($"[{name}] Dropped gun");
        
        equippedGun = null;
        gunTransform = null;
        gunRigidbody = null;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
        
        if (gunAttachPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(gunAttachPoint.position, 0.05f);
        }
    }
}
