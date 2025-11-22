using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float ascentMultiplier = 1.5f; // slows down rise at peak
    [SerializeField] private bool enableDoubleJump = true;
    [SerializeField] private int maxJumps = 2;

    [Header("Wall Cling & Wall Jump")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.65f;
    [SerializeField] private float wallSlideSpeed = -2f;
    [SerializeField] private float wallJumpHorizontalForce = 18f;
    [SerializeField] private float wallJumpVerticalForce = 14f;
    [SerializeField] private float wallDetachCooldown = 0.12f;
    [SerializeField] private float wallJumpInputLock = 0.14f;

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

    [Header("Parry Settings")]
    [SerializeField] private LayerMask parryLayer;   // existing
    [SerializeField] private float parryCooldown = 1f; // NEW: cooldown exposed to inspector
    private float nextParryTime = 0f;                  // NEW: internal timer
    [SerializeField] private float parryEndBoost = 12f;

    private Rigidbody2D rb;

    private float horizontalInput;
    private float horizontalInputEffective;
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
    private bool wasWallClinging;
    private int wallDirection = 0;
    private float wallCooldownTimer;
    private bool justWallJumped = false;
    private float wallJumpInputTimer = 0f;

    // Facing direction
    private int facingDirection = 1;
    private Vector2 rayDirection;

    // Grapple
    [Header("Directional Raycast")]
    [SerializeField] private float directionalRayDistance = 10f;
    [SerializeField] private float directionalRayDuration = 1f;
    [SerializeField] private LayerMask grappleLayer;
    [SerializeField] private float grappleDelay = 0.2f;
    [SerializeField] private float grappleLerpDuration = 0.3f;
    [SerializeField] private float grappleEndBoost = 8f;
    [SerializeField] private float grappleCooldown = 0.3f;
    private RaycastHit2D directionalRayHit;
    private bool isRayActive;
    private float rayTimer;
    private float originalGravityScale;
    private bool isGrappling;
    private Vector2 grappleStartPos;
    private Vector2 grappleTargetPos;
    private float grappleLerpTimer;
    private bool canGrapple = true;
    private float grappleCooldownTimer;

    // Sprite flash
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isRed = false;   // existing

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;

        // Sprite Renderer setup
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    private void Update()
    {
        // Flash red on RIGHT-CLICK, but only if cooldown has passed (Option A behavior)
        if (Input.GetMouseButtonDown(1) && Time.time >= nextParryTime)
            StartCoroutine(FlashRedRoutine());

        if (wallCooldownTimer > 0f) wallCooldownTimer -= Time.deltaTime;
        if (wallJumpInputTimer > 0f) wallJumpInputTimer -= Time.deltaTime;
        if (jumpBufferCounter > 0f) jumpBufferCounter -= Time.deltaTime;
        if (!canDash) dashCooldownTimer -= Time.deltaTime;

        if (justWallJumped && wallJumpInputTimer <= 0f)
            justWallJumped = false;

        GetInput();
        CheckGround();
        CheckWall();

        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleDash();

        horizontalInputEffective = (wallJumpInputTimer > 0f) ? 0f : horizontalInput;

        if (!isDashing)
        {
            HandleWallCling();
            HandleJump();
        }

        if (!canDash && dashCooldownTimer <= 0f)
            canDash = true;
    }

    private void FixedUpdate()
    {
        if (isDashing)
            PerformDash();
        else
        {
            HandleMovement();
            ApplyBetterJump();
        }
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");

        if (!isRayActive)
        {
            if (horizontalInput > 0.1f) facingDirection = 1;
            else if (horizontalInput < -0.1f) facingDirection = -1;
        }

        if (Input.GetButtonDown("Jump"))
            jumpBufferCounter = jumpBufferTime;

        // Directional Ray Input
        if (Input.GetMouseButtonDown(0) && !isRayActive && !isGrappling && canGrapple)
        {
            float verticalInput = Input.GetAxisRaw("Vertical");
            if (verticalInput > 0.1f) rayDirection = Vector2.up;
            else if (verticalInput < -0.1f) rayDirection = Vector2.down;
            else rayDirection = Vector2.right * facingDirection;

            isRayActive = true;
            rayTimer = directionalRayDuration;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
            canGrapple = false;
            grappleCooldownTimer = grappleCooldown;

            ShootDirectionalRay();
        }

        if (isRayActive)
        {
            rayTimer -= Time.deltaTime;
            if (rayTimer <= 0f)
            {
                isRayActive = false;
                rb.gravityScale = originalGravityScale;
                directionalRayHit = default;
            }
        }

        if (!canGrapple && !isGrappling && !isRayActive)
        {
            grappleCooldownTimer -= Time.deltaTime;
            if (grappleCooldownTimer <= 0f)
                canGrapple = true;
        }
    }

    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (isGrounded) jumpsRemaining = maxJumps;

        if (isGrounded && !wasGrounded)
        {
            canDash = true;
            dashCooldownTimer = 0f;
        }
    }

    private void CheckWall()
    {
        RaycastHit2D hitRight =
            Physics2D.Raycast(wallCheck.position, Vector2.right, wallCheckDistance, groundLayer);
        RaycastHit2D hitLeft =
            Physics2D.Raycast(wallCheck.position, Vector2.left, wallCheckDistance, groundLayer);

        if (hitRight.collider != null) { isTouchingWall = true; wallDirection = 1; }
        else if (hitLeft.collider != null) { isTouchingWall = true; wallDirection = -1; }
        else { isTouchingWall = false; wallDirection = 0; }
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
    private void HandleWallCling()
    {
        wasWallClinging = isWallClinging;

        if (wallCooldownTimer > 0f || justWallJumped)
        {
            isWallClinging = false;
            return;
        }

        bool pressingIntoWall =
            (horizontalInputEffective > 0f && wallDirection == 1) ||
            (horizontalInputEffective < 0f && wallDirection == -1);

        if (isTouchingWall && !isGrounded && pressingIntoWall)
        {
            isWallClinging = true;
            facingDirection = -wallDirection;

            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                Mathf.Max(rb.linearVelocity.y, wallSlideSpeed)
            );

            if (!wasWallClinging)
                jumpsRemaining = maxJumps;
        }
        else
        {
            isWallClinging = false;
        }
    }

    private void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isWallClinging)
        {
            Vector2 vel = new Vector2(
                -wallDirection * wallJumpHorizontalForce,
                wallJumpVerticalForce
            );

            rb.linearVelocity = vel;
            justWallJumped = true;
            wallJumpInputTimer = wallJumpInputLock;
            wallCooldownTimer = wallDetachCooldown;
            isWallClinging = false;
            return;
        }

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

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x,
                rb.linearVelocity.y * 0.5f
            );
        }
    }

    private void HandleMovement()
    {
        if (wallJumpInputTimer > 0f)
            return;

        if (isRayActive || isGrappling)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float targetSpeed = horizontalInput * moveSpeed;
        rb.linearVelocity = new Vector2(targetSpeed, rb.linearVelocity.y);
    }

    private void HandleDash()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash && !isDashing)
        {
            dashDirection = new Vector2(facingDirection, 0f);
            isDashing = true;
            dashTimeLeft = dashDuration;
            canDash = false;
            dashCooldownTimer = dashCooldown;
        }

        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0f)
                isDashing = false;
        }
    }

    private void PerformDash()
    {
        rb.linearVelocity = dashDirection * dashSpeed;
    }

    private void ApplyBetterJump()
    {
        if (isRayActive || isGrappling) return;

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && !Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0f && Input.GetButton("Jump"))
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                                 (ascentMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // GRAPPLE LOGIC (UNCHANGED)
    // ─────────────────────────────────────────────────────────────

    public RaycastHit2D ShootDirectionalRay()
    {
        Vector2 origin = transform.position;
        directionalRayHit = Physics2D.Raycast(origin, rayDirection, directionalRayDistance, grappleLayer);

        if (directionalRayHit.collider != null &&
            ((1 << directionalRayHit.collider.gameObject.layer) & grappleLayer) != 0)
        {
            StartCoroutine(GrappleToTarget(directionalRayHit.collider.transform.position));
        }

        return directionalRayHit;
    }

    private IEnumerator GrappleToTarget(Vector2 targetPos)
    {
        yield return new WaitForSeconds(grappleDelay);

        isGrappling = true;
        grappleStartPos = transform.position;

        if (rayDirection == Vector2.up || rayDirection == Vector2.down)
            grappleTargetPos = new Vector2(transform.position.x, targetPos.y);
        else
            grappleTargetPos = new Vector2(targetPos.x, transform.position.y);

        grappleLerpTimer = 0f;

        while (grappleLerpTimer < grappleLerpDuration)
        {
            grappleLerpTimer += Time.deltaTime;
            float t = grappleLerpTimer / grappleLerpDuration;

            Vector2 newPos = Vector2.Lerp(grappleStartPos, grappleTargetPos, t);
            transform.position = newPos;
            rb.linearVelocity = Vector2.zero;

            yield return null;
        }

        transform.position = grappleTargetPos;
        isGrappling = false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, grappleEndBoost);
        jumpsRemaining = maxJumps;
        canDash = true;
        dashCooldownTimer = 0f;
    }

    // ─────────────────────────────────────────────────────────────
    //  PARRY (FLASH RED + COLLISION CHECK + COOLDOWN)
    // ─────────────────────────────────────────────────────────────

    private IEnumerator FlashRedRoutine()
    {
        isRed = true;
        sr.color = Color.red;
        nextParryTime = Time.time + parryCooldown; // cooldown starts NOW

        yield return new WaitForSeconds(0.5f);

        sr.color = originalColor;
        isRed = false;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isRed) return;

        if (((1 << other.gameObject.layer) & parryLayer) != 0)
        {
            Debug.Log("Parry detected");
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, parryEndBoost);
            jumpsRemaining = maxJumps;
            canDash = true;
            dashCooldownTimer = 0f;
        }
    }

    // ─────────────────────────────────────────────────────────────

    public int GetFacingDirection() => facingDirection;
    public bool DirectionalRayHit() => directionalRayHit.collider != null;
    public RaycastHit2D GetDirectionalRayHit() => directionalRayHit;

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
            Gizmos.DrawLine(wallCheck.position,
                wallCheck.position + Vector3.right * wallCheckDistance);
            Gizmos.DrawLine(wallCheck.position,
                wallCheck.position + Vector3.left * wallCheckDistance);
        }

        if (isRayActive)
        {
            Vector2 origin = transform.position;
            if (directionalRayHit.collider != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(origin, directionalRayHit.point);
                Gizmos.DrawWireSphere(directionalRayHit.point, 0.15f);
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(origin, origin + rayDirection * directionalRayDistance);
            }
        }
    }
}
