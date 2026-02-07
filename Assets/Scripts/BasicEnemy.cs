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
    [SerializeField] private float healthRetreatThreshold = 30f;
    [SerializeField] private float coverSearchRadius = 10f;
    
    [Header("Ragdoll")]
    [SerializeField] private bool enableRagdollOnDeath = true;
    [SerializeField] private float ragdollForceMultiplier = 300f;
    
    [Header("Gun System")]
    [SerializeField] private bool canUseGuns = true;
    
    private enum EnemyState { Idle, LookingForGun, Chasing, Attacking, SeekingCover, Retreating, Staggered, Dead }
    
    private NavMeshAgent agent;
    private Animator animator;
    private Transform player;
    private Rigidbody[] ragdollRigidbodies;
    private Collider[] ragdollColliders;
    private EnemyGunPickup gunPickup;
    private EnemyShooting enemyShooting;
    private ProceduralEnemyAnimation proceduralAnim;
    
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
        proceduralAnim = GetComponent<ProceduralEnemyAnimation>();

        if (proceduralAnim != null && animator != null)
        {
            animator.enabled = false; // Disable standard animator to use procedural rigging
        }
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
        
        if (currentHealth < healthRetreatThreshold && currentState != EnemyState.Retreating)
        {
            currentState = EnemyState.Retreating;
        }
        else if (canUseGuns && gunPickup != null && !gunPickup.HasGun)
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
        else if (currentState == EnemyState.Chasing && Random.value < 0.001f) // Chance to seek cover while chasing
        {
            currentState = EnemyState.SeekingCover;
        }
        else if (currentState != EnemyState.SeekingCover && currentState != EnemyState.Retreating)
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

            case EnemyState.SeekingCover:
                UpdateAnimator(agent.velocity.magnitude);
                if (!agent.hasPath || agent.remainingDistance < 0.5f)
                {
                    FindCover();
                }
                break;

            case EnemyState.Retreating:
                UpdateAnimator(agent.velocity.magnitude);
                Vector3 retreatDir = (transform.position - player.position).normalized;
                agent.SetDestination(transform.position + retreatDir * 5f);
                if (Vector3.Distance(transform.position, player.position) > chaseRange * 1.2f)
                {
                    currentState = EnemyState.Idle;
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
        if (proceduralAnim != null)
        {
            proceduralAnim.UpdateAnimation(speed);
        }
        else if (animator != null && animator.enabled)
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
    
    private void FindCover()
    {
        Vector3 randomDirection = Random.insideUnitSphere * coverSearchRadius;
        randomDirection += transform.position;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, coverSearchRadius, 1))
        {
            Vector3 directionToPlayer = (player.position - hit.position).normalized;
            if (Physics.Raycast(hit.position + Vector3.up, directionToPlayer, out RaycastHit rayHit, coverSearchRadius))
            {
                if (rayHit.collider.transform != player)
                {
                    agent.SetDestination(hit.position);
                    return;
                }
            }
        }

        Vector3 sideStep = Vector3.Cross(player.position - transform.position, Vector3.up).normalized * 3f;
        agent.SetDestination(transform.position + sideStep * (Random.value > 0.5f ? 1 : -1));
    }

    private void PerformMeleeAttack()
    {
        if (proceduralAnim != null)
        {
            proceduralAnim.PlayAttack();
        }
        Debug.Log($"[{name}] Melee Attack! (Triggered Procedural Attack)", this);
    }
    
    public void SetDifficulty(float healthScale, float speedScale)
    {
        maxHealth *= healthScale;
        currentHealth = maxHealth;
        moveSpeed *= speedScale;
        if (agent != null) agent.speed = moveSpeed;
    }

    public void TakeDamage(float damage, Vector3 hitPoint, Vector3 hitDirection, DamageType damageType = DamageType.Slash)
    {
        if (currentState == EnemyState.Dead) return;
        
        float finalDamage = damage;
        Rigidbody hitRb = FindClosestRigidbody(hitPoint);
        bool isHeadshot = hitRb != null && hitRb.name.ToLower().Contains("head");
        bool isLegshot = hitRb != null && (hitRb.name.ToLower().Contains("leg") || hitRb.name.ToLower().Contains("foot"));

        if (isHeadshot) finalDamage *= 4f;
        if (isLegshot) finalDamage *= 0.8f;

        currentHealth -= finalDamage;
        
        if (currentHealth <= 0)
        {
            TransitionToDeath(hitPoint, hitDirection, damageType);
            if (isHeadshot) ExplodeHead(hitPoint, hitDirection);
        }
        else if (isLegshot && finalDamage > 10f)
        {
            TransitionToHobble();
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

    private void ExplodeHead(Vector3 hitPoint, Vector3 hitDirection)
    {
        // Visceral head explosion effect
        if (ImpactManager.Instance != null)
        {
            ImpactManager.Instance.PlayImpact(hitPoint, -hitDirection, "Flesh");
        }
        Debug.Log($"[{name}] HEAD EXPLODED!");
    }

    private void TransitionToHobble()
    {
        moveSpeed *= 0.3f;
        if (agent != null && agent.isOnNavMesh) agent.speed = moveSpeed;
        Debug.Log($"[{name}] HOBBLING!");
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
        
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.EnemyDied();
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

