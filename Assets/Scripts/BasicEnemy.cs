using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class BasicEnemy : MonoBehaviour, IDamageable
{
    [Header("Health Configuration")]
    [SerializeField] private float maxHealth = 100f;
    
    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    
    [Header("AI Behavior")]
    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float staggerThreshold = 20f;
    
    [Header("Ragdoll")]
    [SerializeField] private bool enableRagdollOnDeath = true;
    [SerializeField] private float ragdollForceMultiplier = 300f;
    
    [Header("Gun System")]
    [SerializeField] private bool canUseGuns = true;
    
    private enum EnemyState { Idle, LookingForGun, Chasing, Attacking, Staggered, Dead }
    
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private EnemyGunPickup gunPickup;
    private EnemyShooting enemyShooting;
    
    private EnemyState currentState = EnemyState.Idle;
    private float currentHealth;
    private float nextAttackTime;
    private float staggerRecoveryTime;
    
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead => currentState == EnemyState.Dead;
    
    private void Awake()
    {
        CacheComponents();
        SetupRagdoll();
    }
    
    private void Start()
    {
        Initialize();
        FindPlayer();
        
        if (agent != null && !agent.isOnNavMesh)
        {
            Debug.LogWarning($"[{name}] NavMeshAgent not on NavMesh! Create NavMesh via Window → AI → Navigation → Bake.", this);
        }
    }
    
    private void CacheComponents()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        gunPickup = GetComponent<EnemyGunPickup>();
        enemyShooting = GetComponent<EnemyShooting>();
    }
    
    private void Initialize()
    {
        currentHealth = maxHealth;
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange * 0.8f;
    }
    
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"[{name}] Player not found. Ensure Player has 'Player' tag.", this);
        }
    }
    
    private void Update()
    {
        if (currentState == EnemyState.Dead || player == null) return;
        
        UpdateState();
        ExecuteState();
    }
    
    private void UpdateState()
    {
        if (currentState == EnemyState.Staggered)
        {
            if (Time.time >= staggerRecoveryTime)
            {
                currentState = EnemyState.Idle;
            }
            return;
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (canUseGuns && gunPickup != null && !gunPickup.HasGun)
        {
            currentState = EnemyState.LookingForGun;
        }
        else if (distanceToPlayer > chaseRange)
        {
            currentState = EnemyState.Idle;
        }
        else if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attacking;
        }
        else
        {
            currentState = EnemyState.Chasing;
        }
    }
    
    private void ExecuteState()
    {
        if (agent == null || !agent.isOnNavMesh) return;
        
        switch (currentState)
        {
            case EnemyState.Idle:
                agent.ResetPath();
                UpdateAnimator(0f);
                break;
                
            case EnemyState.LookingForGun:
                if (gunPickup != null)
                {
                    gunPickup.TryPickupNearbyGun();
                }
                UpdateAnimator(0f);
                break;
                
            case EnemyState.Chasing:
                agent.SetDestination(player.position);
                UpdateAnimator(agent.velocity.magnitude);
                break;
                
            case EnemyState.Attacking:
                agent.ResetPath();
                transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
                UpdateAnimator(0f);
                
                if (Time.time >= nextAttackTime)
                {
                    PerformAttack();
                }
                break;
                
            case EnemyState.Staggered:
                agent.ResetPath();
                UpdateAnimator(0f);
                break;
        }
    }
    
    private void UpdateAnimator(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat(AnimSpeed, speed / moveSpeed);
        }
    }
    
    private void PerformAttack()
    {
        nextAttackTime = Time.time + attackCooldown;
        
        if (gunPickup != null && gunPickup.HasGun && enemyShooting != null)
        {
            enemyShooting.TryShootAtPlayer();
        }
        else
        {
            PerformMeleeAttack();
        }
    }
    
    private void PerformMeleeAttack()
    {
        Debug.Log($"[{name}] Melee Attack! (Implement physics-based melee attack here)", this);
    }
    
    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType = DamageType.Slash)
    {
        if (currentState == EnemyState.Dead) return;
        
        float finalDamage = damage;
        currentHealth -= finalDamage;
        
        if (currentHealth <= 0)
        {
            TransitionToDeath(hitPoint, hitDirection, damageType);
        }
        else if (finalDamage >= staggerThreshold)
        {
            TransitionToStagger(hitDirection, finalDamage);
        }
        else
        {
            PlayHitReaction();
        }
    }
    
    private void TransitionToStagger(Vector3 hitDirection, float damage)
    {
        currentState = EnemyState.Staggered;
        staggerRecoveryTime = Time.time + Mathf.Clamp(damage / 30f, 0.5f, 2f);
        
        ApplyPhysicalImpact(hitDirection, damage * 10f);
    }
    
    private void PlayHitReaction()
    {
    }
    
    private void ApplyPhysicalImpact(Vector3 direction, float force)
    {
        Rigidbody mainRb = GetComponent<Rigidbody>();
        if (mainRb != null && !mainRb.isKinematic)
        {
            mainRb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }
    }
    
    private void TransitionToDeath(Vector3 hitPoint, Vector3 hitDirection, DamageType damageType)
    {
        currentState = EnemyState.Dead;
        
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
        
        if (gunPickup != null && gunPickup.HasGun)
        {
            gunPickup.DropGun();
        }
        
        if (enableRagdollOnDeath)
        {
            ActivateRagdoll(hitPoint, hitDirection, damageType);
        }
        else
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
        }
        
        Destroy(gameObject, 10f);
    }
    
    private void SetupRagdoll()
    {
        ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
        ragdollColliders = GetComponentsInChildren<Collider>();
        
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb != GetComponent<Rigidbody>())
            {
                rb.isKinematic = true;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
        
        foreach (Collider col in ragdollColliders)
        {
            if (col != GetComponent<Collider>())
            {
                col.isTrigger = true;
            }
        }
    }
    
    private void ActivateRagdoll(Vector3 hitPoint, Vector3 hitDirection, DamageType damageType)
    {
        if (animator != null)
        {
            animator.enabled = false;
        }
        
        float forceMultiplier = damageType == DamageType.Bullet ? ragdollForceMultiplier * 1.5f : ragdollForceMultiplier;
        
        Rigidbody closestRb = FindClosestRigidbody(hitPoint);
        
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            rb.isKinematic = false;
            
            if (rb == closestRb)
            {
                rb.AddForceAtPosition(hitDirection * forceMultiplier, hitPoint, ForceMode.Impulse);
            }
        }
        
        foreach (Collider col in ragdollColliders)
        {
            col.isTrigger = false;
        }
        
        Rigidbody mainRb = GetComponent<Rigidbody>();
        if (mainRb != null)
        {
            mainRb.isKinematic = true;
        }
        
        Collider mainCol = GetComponent<Collider>();
        if (mainCol != null)
        {
            mainCol.enabled = false;
        }
    }
    
    private Rigidbody FindClosestRigidbody(Vector3 point)
    {
        Rigidbody closest = null;
        float closestDistance = float.MaxValue;
        
        foreach (Rigidbody rb in ragdollRigidbodies)
        {
            if (rb == GetComponent<Rigidbody>()) continue;
            
            float distance = Vector3.Distance(rb.position, point);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = rb;
            }
        }
        
        return closest;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, chaseRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

