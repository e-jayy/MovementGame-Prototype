using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;
    
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private int maxJumps = 2;
    
    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;
    
    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;
    
    [Header("Coyote Time & Jump Buffer")]
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;
    
    private Rigidbody2D rb;
    private float horizontalInput;
    private bool isGrounded;
    private bool wasGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int jumpsRemaining;
    
    // Dash variables
    private bool isDashing;
    private bool canDash = true;
    private float dashTimeLeft;
    private float dashCooldownTimer;
    private Vector2 dashDirection;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    private void Update()
    {
        GetInput();
        CheckGround();
        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleDash();
        
        if (!isDashing)
        {
            HandleJump();
        }
        
        // Update dash cooldown timer
        if (!canDash)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (dashCooldownTimer <= 0f)
            {
                canDash = true;
            }
        }
    }
    
    private void FixedUpdate()
    {
        if (isDashing)
        {
            PerformDash();
        }
        else
        {
            HandleMovement();
            ApplyBetterJump();
        }
    }
    
    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        
        if (Input.GetButtonDown("Jump"))
        {
            jumpBufferCounter = jumpBufferTime;
        }
        else
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }
    
    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        
        // Reset jumps when grounded
        if (isGrounded)
        {
            jumpsRemaining = maxJumps;
        }
        
        // Reset dash when landing (transitioning from air to ground)
        if (isGrounded && !wasGrounded)
        {
            canDash = true;
            dashCooldownTimer = 0f;
        }
    }
    
    private void HandleCoyoteTime()
    {
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }
    
    private void HandleJumpBuffer()
    {
        // Jump buffer allows player to press jump slightly before landing
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
        }
    }
    
    private void HandleJump()
    {
        // Jump input
        if (Input.GetButtonDown("Jump"))
        {
            // Ground jump or coyote time jump
            if (coyoteTimeCounter > 0f && jumpsRemaining > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsRemaining--;
                coyoteTimeCounter = 0f;
            }
            // Double jump (or additional jumps)
            else if (enableDoubleJump && jumpsRemaining > 0 && !isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsRemaining--;
            }
        }
        
        // Variable jump height (release jump button early for shorter jump)
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }
    
    private void HandleMovement()
    {
        // Instant maximum speed movement
        float targetSpeed = horizontalInput * moveSpeed;
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }
    
    private void HandleDash()
    {
        // Start dash
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            // Determine dash direction based on input
            float dashDir = horizontalInput;
            
            // If no horizontal input, dash in the direction player is facing (default right)
            if (Mathf.Abs(dashDir) < 0.1f)
            {
                dashDir = 1f; // Default to right if no input
            }
            
            dashDirection = new Vector2(dashDir, 0f).normalized;
            isDashing = true;
            dashTimeLeft = dashDuration;
            canDash = false;
            dashCooldownTimer = dashCooldown;
        }
        
        // Update dash duration
        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
            {
                isDashing = false;
            }
        }
    }
    
    private void PerformDash()
    {
        // Set linearVelocity to dash speed in dash direction
        rb.linearVelocity = dashDirection * dashSpeed;
    }
    
    private void ApplyBetterJump()
    {
        // Apply extra gravity when falling
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        // Apply less gravity when holding jump and going up
        else if (rb.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }
    
    // Visualize ground check in editor
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }
    }
}