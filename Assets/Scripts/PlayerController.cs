using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 8f;
    public float jumpForce = 12f;
    public float gravity = -20f;
    public float turnSmoothTime = 0.1f;
    
    [Header("Animation Settings")]
    public AnimationCurve jumpCurve;
    public float animationSmoothTime = 0.1f;
    
    [Header("Combat Settings")]
    public float attackRange = 2f;
    public float attackDamage = 25f;
    public float attackCooldown = 0.5f;
    
    private Transform cameraTransform;
    private Rigidbody rb;
    private Animator animator;
    private CapsuleCollider playerCollider;
    
    private float turnSmoothVelocity;
    private Vector3 velocity;
    private bool isGrounded;
    private bool canAttack = true;
    private bool isAttacking = false;
    private bool isJumping = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        playerCollider = GetComponent<CapsuleCollider>();
        
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
        
        // Set up complex animation parameters
        SetupAnimationParameters();
    }
    
    void Update()
    {
        HandleMovement();
        HandleJumping();
        HandleAttacking();
        ApplyGravity();
        UpdateAnimations();
    }
    
    void HandleMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        
        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            
            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            rb.MovePosition(transform.position + moveDir * moveSpeed * Time.deltaTime);
            
            // Set animation parameters based on movement
            animator.SetFloat("Speed", direction.magnitude, animationSmoothTime, Time.deltaTime);
        }
        else
        {
            animator.SetFloat("Speed", 0f, animationSmoothTime, Time.deltaTime);
        }
    }
    
    void HandleJumping()
    {
        if (Input.GetButtonDown("Jump") && isGrounded && !isAttacking)
        {
            velocity.y = jumpForce;
            isJumping = true;
            animator.SetBool("IsJumping", true);
        }
    }
    
    void HandleAttacking()
    {
        if (Input.GetButtonDown("Fire1") && canAttack && !isJumping)
        {
            StartCoroutine(AttackCoroutine());
        }
    }
    
    IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        canAttack = false;
        animator.SetTrigger("Attack");
        
        yield return new WaitForSeconds(0.3f); // Attack wind-up time
        
        // Check for enemies in range
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position, attackRange);
        foreach (Collider enemy in hitEnemies)
        {
            if (enemy.CompareTag("Enemy"))
            {
                // Apply damage to enemy
                enemy.GetComponent<EnemyAI>().TakeDamage(attackDamage);
            }
        }
        
        yield return new WaitForSeconds(attackCooldown);
        isAttacking = false;
        canAttack = true;
    }
    
    void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep player grounded
        }
        
        rb.velocity = new Vector3(rb.velocity.x, velocity.y, rb.velocity.z);
    }
    
    void UpdateAnimations()
    {
        // Complex animation blending based on state
        float verticalVelocity = Mathf.Clamp(velocity.y / jumpForce, -1f, 1f);
        animator.SetFloat("VerticalVelocity", verticalVelocity);
        
        // Ground detection for animations
        isGrounded = Physics.Raycast(transform.position, Vector3.down, playerCollider.bounds.extents.y + 0.1f);
        animator.SetBool("IsGrounded", isGrounded);
        
        if (isGrounded && isJumping)
        {
            isJumping = false;
            animator.SetBool("IsJumping", false);
        }
    }
    
    void SetupAnimationParameters()
    {
        // Initialize animation parameters for complex state machine
        animator.SetFloat("Speed", 0f);
        animator.SetFloat("VerticalVelocity", 0f);
        animator.SetBool("IsGrounded", true);
        animator.SetBool("IsJumping", false);
        animator.ResetTrigger("Attack");
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw attack range gizmo
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }
    
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}