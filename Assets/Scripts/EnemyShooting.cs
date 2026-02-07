using UnityEngine;

[RequireComponent(typeof(EnemyGunPickup))]
public class EnemyShooting : MonoBehaviour
{
    [Header("Shooting Configuration")]
    [SerializeField] private float shootingRange = 20f;
    [SerializeField] private float fireRate = 2f;
    [SerializeField] private float aimAccuracy = 0.85f;
    [SerializeField] private LayerMask damageableLayers = -1;
    
    [Header("References")]
    [SerializeField] private Transform shootPoint;
    
    private EnemyGunPickup gunPickup;
    private Transform targetPlayer;
    private float nextFireTime;
    
    public bool CanShoot => gunPickup.HasGun && HasAmmo() && Time.time >= nextFireTime;
    
    private void Awake()
    {
        gunPickup = GetComponent<EnemyGunPickup>();
        
        if (shootPoint == null)
        {
            GameObject shootObj = new GameObject("ShootPoint");
            shootPoint = shootObj.transform;
            shootPoint.SetParent(transform);
            shootPoint.localPosition = new Vector3(0f, 1.6f, 0.5f);
        }
    }
    
    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            targetPlayer = playerObj.transform;
        }
    }
    
    private bool HasAmmo()
    {
        if (gunPickup.EquippedGun == null) return false;
        return gunPickup.EquippedGun.HasChamberedRound || gunPickup.EquippedGun.AmmoCount > 0;
    }
    
    public void TryShootAtPlayer()
    {
        if (!CanShoot || targetPlayer == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, targetPlayer.position);
        if (distanceToPlayer > shootingRange) return;
        
        FireGun();
    }
    
    private void FireGun()
    {
        nextFireTime = Time.time + (1f / fireRate);
        
        VRGun gun = gunPickup.EquippedGun;
        if (gun == null) return;
        
        Vector3 targetPosition = targetPlayer.position + Vector3.up * 1.5f;
        Vector3 shootDirection = (targetPosition - shootPoint.position).normalized;
        
        shootDirection = ApplyAccuracy(shootDirection);
        
        PerformRaycast(shootDirection);
        
        Debug.Log($"[{name}] Enemy fired gun at player!");
    }
    
    private Vector3 ApplyAccuracy(Vector3 direction)
    {
        float inaccuracy = 1f - aimAccuracy;
        float spreadX = Random.Range(-inaccuracy, inaccuracy);
        float spreadY = Random.Range(-inaccuracy, inaccuracy);
        
        Vector3 spread = new Vector3(spreadX, spreadY, 0f);
        return (direction + spread).normalized;
    }
    
    private void PerformRaycast(Vector3 direction)
    {
        Ray ray = new Ray(shootPoint.position, direction);
        
        if (Physics.Raycast(ray, out RaycastHit hit, shootingRange, damageableLayers))
        {
            IDamageable damageable = hit.collider.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                float damage = 25f;
                Vector3 hitDirection = direction;
                damageable.TakeDamage(damage, hit.point, hitDirection, DamageType.Bullet);
                
                Debug.Log($"[{name}] Enemy hit {hit.collider.name} for {damage} damage!");
            }
            
            Debug.DrawLine(shootPoint.position, hit.point, Color.red, 0.5f);
        }
        else
        {
            Debug.DrawRay(shootPoint.position, direction * shootingRange, Color.yellow, 0.5f);
        }
    }
    
    public bool IsPlayerInShootingRange()
    {
        if (targetPlayer == null) return false;
        float distance = Vector3.Distance(transform.position, targetPlayer.position);
        return distance <= shootingRange;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
        
        if (shootPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(shootPoint.position, 0.1f);
            Gizmos.DrawRay(shootPoint.position, shootPoint.forward * 2f);
        }
    }
}
