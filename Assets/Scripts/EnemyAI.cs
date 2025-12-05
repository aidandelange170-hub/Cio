using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 5f;
    public float health = 100f;
    public float maxHealth = 100f;
    public float damage = 20f;
    
    [Header("Patrol Settings")]
    public Transform[] patrolPoints;
    public float waitTime = 2f;
    public bool isPatrolling = true;
    
    [Header("Animation Settings")]
    public float animationSmoothTime = 0.1f;
    
    private Transform player;
    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider enemyCollider;
    
    private int currentPatrolIndex = 0;
    private bool isChasing = false;
    private bool isAttacking = false;
    private bool isTakingDamage = false;
    private bool isDead = false;
    private float lastAttackTime = 0f;
    private Vector3 startingPosition;
    
    private enum EnemyState { Patrol, Chase, Attack, TakeDamage, Dead }
    private EnemyState currentState = EnemyState.Patrol;
    
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<CapsuleCollider>();
        startingPosition = transform.position;
        
        SetupAnimationParameters();
        
        if (patrolPoints.Length == 0)
        {
            isPatrolling = false;
        }
        
        StartCoroutine(AIStateMachine());
    }
    
    IEnumerator AIStateMachine()
    {
        while (!isDead)
        {
            switch (currentState)
            {
                case EnemyState.Patrol:
                    Patrol();
                    break;
                case EnemyState.Chase:
                    Chase();
                    break;
                case EnemyState.Attack:
                    Attack();
                    break;
                case EnemyState.TakeDamage:
                    TakeDamageState();
                    break;
                case EnemyState.Dead:
                    Die();
                    yield break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void Patrol()
    {
        if (!isPatrolling || patrolPoints.Length == 0) return;
        
        if (patrolPoints.Length > 0)
        {
            Transform targetPoint = patrolPoints[currentPatrolIndex];
            
            if (Vector3.Distance(transform.position, targetPoint.position) < 0.5f)
            {
                // Wait at patrol point
                StartCoroutine(WaitAtPatrolPoint());
            }
            else
            {
                // Move towards patrol point
                Vector3 direction = (targetPoint.position - transform.position).normalized;
                rb.MovePosition(transform.position + direction * moveSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, 
                    Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
                
                animator.SetFloat("Speed", moveSpeed / 10f, animationSmoothTime, Time.deltaTime);
            }
        }
    }
    
    IEnumerator WaitAtPatrolPoint()
    {
        animator.SetFloat("Speed", 0f, animationSmoothTime, Time.deltaTime);
        yield return new WaitForSeconds(waitTime);
        
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }
    
    void Chase()
    {
        if (player == null) return;
        
        Vector3 direction = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer > detectionRange)
        {
            // Return to patrol
            currentState = EnemyState.Patrol;
            return;
        }
        
        if (distanceToPlayer <= attackRange)
        {
            currentState = EnemyState.Attack;
            return;
        }
        
        // Move towards player
        rb.MovePosition(transform.position + direction * moveSpeed * 1.5f * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, 
            Quaternion.LookRotation(direction), rotationSpeed * 1.5f * Time.deltaTime);
        
        animator.SetFloat("Speed", (moveSpeed * 1.5f) / 10f, animationSmoothTime, Time.deltaTime);
    }
    
    void Attack()
    {
        if (player == null) return;
        
        Vector3 direction = (player.position - transform.position).normalized;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Look at player
        transform.rotation = Quaternion.Slerp(transform.rotation, 
            Quaternion.LookRotation(direction), rotationSpeed * Time.deltaTime);
        
        if (distanceToPlayer > attackRange)
        {
            currentState = EnemyState.Chase;
            return;
        }
        
        if (Time.time - lastAttackTime >= 2f) // Attack cooldown
        {
            StartCoroutine(AttackCoroutine());
        }
    }
    
    IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;
        animator.SetTrigger("Attack");
        
        yield return new WaitForSeconds(0.5f); // Attack delay
        
        // Damage player if in range
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        if (distanceToPlayer <= attackRange + 0.5f)
        {
            // In a real game, this would call player damage function
            Debug.Log("Player damaged by enemy!");
        }
        
        yield return new WaitForSeconds(1f); // Attack recovery time
        isAttacking = false;
    }
    
    void TakeDamageState()
    {
        // Currently handled by TakeDamage method
    }
    
    public void TakeDamage(float damageAmount)
    {
        if (isDead) return;
        
        health -= damageAmount;
        animator.SetTrigger("TakeDamage");
        
        if (health <= 0)
        {
            health = 0;
            currentState = EnemyState.Dead;
        }
        else
        {
            // Knockback effect
            Vector3 knockbackDirection = (transform.position - player.position).normalized;
            rb.AddForce(knockbackDirection * 5f, ForceMode.Impulse);
        }
    }
    
    void Die()
    {
        isDead = true;
        animator.SetBool("IsDead", true);
        
        // Disable collider and movement
        enemyCollider.enabled = false;
        rb.isKinematic = true;
        
        // Destroy after animation
        Destroy(gameObject, 2f);
    }
    
    void Update()
    {
        // Check for player in detection range
        if (player != null && !isDead)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            if (distanceToPlayer <= detectionRange && currentState != EnemyState.Attack)
            {
                currentState = EnemyState.Chase;
            }
            else if (currentState != EnemyState.Attack && currentState != EnemyState.TakeDamage && currentState != EnemyState.Dead)
            {
                currentState = EnemyState.Patrol;
            }
        }
        
        UpdateAnimations();
    }
    
    void UpdateAnimations()
    {
        // Update animation parameters based on state
        animator.SetFloat("Speed", animator.GetFloat("Speed"), animationSmoothTime, Time.deltaTime);
        animator.SetBool("IsChasing", currentState == EnemyState.Chase);
        animator.SetBool("IsAttacking", isAttacking);
        animator.SetBool("IsTakingDamage", isTakingDamage);
    }
    
    void SetupAnimationParameters()
    {
        // Initialize animation parameters
        animator.SetFloat("Speed", 0f);
        animator.SetBool("IsChasing", false);
        animator.SetBool("IsAttacking", false);
        animator.SetBool("IsTakingDamage", false);
        animator.SetBool("IsDead", false);
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("TakeDamage");
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection and attack ranges
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw patrol points
        if (patrolPoints != null)
        {
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(patrolPoints[i].position, 0.5f);
                    
                    if (i > 0)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(patrolPoints[i-1].position, patrolPoints[i].position);
                    }
                }
            }
            
            // Draw line from last to first point to complete patrol route
            if (patrolPoints.Length > 1 && patrolPoints[0] != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(patrolPoints[patrolPoints.Length-1].position, patrolPoints[0].position);
            }
        }
    }
}