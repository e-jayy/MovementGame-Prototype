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

    [Header("Wall Cling & Wall Jump")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.65f;
    [SerializeField] private float wallSlideSpeed = -2f;
    [SerializeField] private float wallJumpHorizontalForce = 18f; // stronger than vertical by default
    [SerializeField] private float wallJumpVerticalForce = 14f;
    [SerializeField] private float wallDetachCooldown = 0.12f;   // short cooldown so you don't re-stick immediately
    [SerializeField] private float wallJumpInputLock = 0.14f;     // ignore player's horizontal input for this time after wall jump

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Coyote Time & Jump Buffer")]
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    private Rigidbody2D rb;

    private float horizontalInput;
    private float horizontalInputEffective; // used for cling checks and movement when locked
    private bool isGrounded;
    private bool wasGrounded;

    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int jumpsRemaining;

    // Dash
    private bool isDashing;
    private bool canDash = true;
    private float dashTimeLeft;
    private float dashCooldownTimer;
    private Vector2 dashDirection;

    // Wall
    private bool isTouchingWall;
    private bool isWallClinging;
    private int wallDirection = 0;      // -1 left, +1 right, 0 none
    private float wallCooldownTimer;
    private bool justWallJumped = false;
    private float wallJumpInputTimer = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // decrement timers
        if (wallCooldownTimer > 0f) wallCooldownTimer -= Time.deltaTime;
        if (wallJumpInputTimer > 0f) wallJumpInputTimer -= Time.deltaTime;
        if (jumpBufferCounter > 0f) jumpBufferCounter -= Time.deltaTime;
        if (!canDash) dashCooldownTimer -= Time.deltaTime;

        // allow clearing justWallJumped once the player has moved away enough (or input lock expired)
        if (justWallJumped && wallJumpInputTimer <= 0f)
        {
            // only clear after the input lock expires (prevents instant re-cling)
            justWallJumped = false;
        }

        GetInput();      // updates horizontalInput and jumpBufferCounter
        CheckGround();
        CheckWall();

        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleDash();

        // choose effective horizontal for cling checks and movement:
        // when we've just wall-jumped, ignore player's held horizontal for a short time
        horizontalInputEffective = (wallJumpInputTimer > 0f) ? 0f : horizontalInput;

        if (!isDashing)
        {
            HandleWallCling();
            HandleJump();
        }

        // dash cooldown finishing
        if (!canDash && dashCooldownTimer <= 0f)
            canDash = true;
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
            jumpBufferCounter = jumpBufferTime;
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (isGrounded)
            jumpsRemaining = maxJumps;

        if (isGrounded && !wasGrounded)
        {
            canDash = true;
            dashCooldownTimer = 0f;
        }
    }

    private void CheckWall()
    {
        // Debug lines to visualize
        if (wallCheck != null)
        {
            Debug.DrawRay(wallCheck.position, Vector2.right * wallCheckDistance, Color.yellow);
            Debug.DrawRay(wallCheck.position, Vector2.left * wallCheckDistance, Color.yellow);
        }

        // Raycast both sides to determine actual wall side (this avoids relying on input)
        RaycastHit2D hitRight = Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, groundLayer);
        RaycastHit2D hitLeft  = Physics2D.Raycast(wallCheck.position, Vector2.left, wallCheckDistance, groundLayer);

        if (hitRight.collider != null)
        {
            isTouchingWall = true;
            wallDirection = 1;
        }
        else if (hitLeft.collider != null)
        {
            isTouchingWall = true;
            wallDirection = -1;
        }
        else
        {
            isTouchingWall = false;
            wallDirection = 0;
        }

        // If raycasts fail in your setup (tilemap/composite collider), consider BoxCast fallback:
        // (uncomment and tweak size/distance if needed)
        /*
        if (!isTouchingWall)
        {
            Vector2 boxSize = new Vector2(0.5f, 1.0f);
            float boxDistance = 0.1f;
            RaycastHit2D rightBox = Physics2D.BoxCast(transform.position, boxSize, 0f, Vector2.right, boxDistance, groundLayer);
            RaycastHit2D leftBox  = Physics2D.BoxCast(transform.position, boxSize, 0f, Vector2.left, boxDistance, groundLayer);
            if (rightBox.collider != null) { isTouchingWall = true; wallDirection = 1; }
            else if (leftBox.collider != null) { isTouchingWall = true; wallDirection = -1; }
        }
        */
    }

    private void HandleCoyoteTime()
    {
        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJumpBuffer()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
        }
    }

    // WALL CLING
    private void HandleWallCling()
    {
        // don't allow cling while cooldown active or right after wall jump (input lock prevents immediate re-cling)
        if (wallCooldownTimer > 0f || justWallJumped)
        {
            isWallClinging = false;
            return;
        }

        // pressing into wall uses effective horizontal (which will be zero during input lock)
        bool pressingIntoWall =
            (horizontalInputEffective > 0f && wallDirection == 1) ||
            (horizontalInputEffective < 0f && wallDirection == -1);

        if (isTouchingWall && !isGrounded && pressingIntoWall)
        {
            isWallClinging = true;
            // slow slide
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, wallSlideSpeed));
        }
        else
        {
            isWallClinging = false;
        }
    }

    // JUMPING
    private void HandleJump()
    {
        // WALL JUMP
        if (Input.GetButtonDown("Jump") && isWallClinging)
        {
            // apply a strong diagonal velocity AWAY from the wall (no normalization)
            Vector2 wallJumpVel = new Vector2(-wallDirection * wallJumpHorizontalForce, wallJumpVerticalForce);
            rb.linearVelocity = wallJumpVel;

            // lock input for a short time so held input can't force a re-cling
            justWallJumped = true;
            wallJumpInputTimer = wallJumpInputLock;
            wallCooldownTimer = wallDetachCooldown;
            isWallClinging = false;

            return;
        }

        // GROUND / DOUBLE JUMP
        if (Input.GetButtonDown("Jump"))
        {
            if (coyoteTimeCounter > 0f && jumpsRemaining > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsRemaining--;
                coyoteTimeCounter = 0f;
            }
            else if (enableDoubleJump && jumpsRemaining > 0 && !isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsRemaining--;
            }
        }

        // VARIABLE HEIGHT
        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void HandleMovement()
    {
        // If we're in the short input lock after a wall jump, don't apply player horizontal input
        if (wallJumpInputTimer > 0f)
        {
            // allow the wall-jump horizontal velocity to carry — do not overwrite it
            return;
        }

        float targetSpeed = horizontalInput * moveSpeed;
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }

    // DASHING
    private void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            float dashDir = horizontalInput;
            if (Mathf.Abs(dashDir) < 0.1f)
                dashDir = transform.localScale.x > 0 ? 1f : -1f;

            dashDirection = new Vector2(dashDir, 0f);
            isDashing = true;
            dashTimeLeft = dashDuration;
            canDash = false;
            dashCooldownTimer = dashCooldown;
        }

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f) isDashing = false;
        }
    }

    private void PerformDash()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    private void ApplyBetterJump()
    {
        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position, wallCheck.position + Vector3.left * wallCheckDistance);
        }
    }
}
