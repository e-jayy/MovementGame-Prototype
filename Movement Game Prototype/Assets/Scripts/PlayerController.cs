using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("UnlockedAbilities")]
    //Add these values into a player manager singleton later
    [SerializeField] private bool unlockedDash;
    [SerializeField] private bool unlockedHook;
    [SerializeField] private bool unlockedWallJump;
    [SerializeField] private bool unlockedDoubleJump;

    
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;
    [SerializeField] private float ascentMultiplier = 1.5f;
    [SerializeField] private int maxJumps = 2;

    [Header("Fall Speed Cap")]
    [SerializeField] private float maxFallSpeed = -20f;

    [Header("Wall Cling & Wall Jump")]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float wallCheckHeight = 0.5f;
    [SerializeField] private float wallSlideSpeed = -2f;
    [SerializeField] private float wallJumpHorizontalForce = 9f;
    [SerializeField] private float wallJumpVerticalForce = 11f;
    [SerializeField] private float wallDetachCooldown = 0.12f;
    [SerializeField] private float wallJumpInputLock = 0.35f;

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

    [Header("Bounce Settings")]
    [SerializeField] private LayerMask bounceLayer;
    [SerializeField] private float bounceCooldown = 1f;
    private float nextBounceTime = 0f;
    [SerializeField] private float bounceEndBoost = 12f;

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
    [SerializeField] private LineRenderer grappleLineRenderer;

    private RaycastHit2D directionalRayHit;
    private bool isRayActive;
    private float rayTimer;
    private float originalGravityScale;
    private bool isGrapplingToTarget;
    private bool isGrappling;
    private Vector2 grappleStartPos;
    private Vector2 grappleTargetPos;
    private float grappleLerpTimer;
    private bool canGrapple = true;
    private float grappleCooldownTimer;

    // Sprite flash
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isRed = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;

        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;

        if (grappleLineRenderer != null)
            grappleLineRenderer.enabled = false;
    }

    private void Update()
    {
        if (InputManager.instance.BounceInput && Time.time >= nextBounceTime)
            StartCoroutine(BounceRoutine());

        if (wallCooldownTimer > 0f) wallCooldownTimer -= Time.deltaTime;
        if (wallJumpInputTimer > 0f) wallJumpInputTimer -= Time.deltaTime;
        if (jumpBufferCounter > 0f) jumpBufferCounter -= Time.deltaTime;
        if (!canDash) dashCooldownTimer -= Time.deltaTime;

        if (justWallJumped && wallJumpInputTimer <= 0f)
            justWallJumped = false;

        GetHorizontalInput();
        CheckGround();
        CheckWall();

        HandleHook();
        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleDash();
        
        HandleAbilityUnlocks();

        horizontalInputEffective = (wallJumpInputTimer > 0f) ? 0f : horizontalInput; //If the wall-jump input lock timer is still running, ignore horizontal input. Otherwise, use the players real horizontal input

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

    private void GetHorizontalInput()
    {
        horizontalInput = InputManager.instance.MoveInput.x;

        if (!isRayActive)
        {
            if (horizontalInput > 0.1f) facingDirection = 1;
            else if (horizontalInput < -0.1f) facingDirection = -1;
        }

        if (InputManager.instance.JumpJustPressed)
            jumpBufferCounter = jumpBufferTime;
    }

    private void HandleMovement()
    {
        if (wallJumpInputTimer > 0f) // Locks input when wall jumping
            return;

        if (isRayActive || isGrapplingToTarget)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    #region Wall
    private void CheckWall()
    {
        RaycastHit2D hitRight = Physics2D.BoxCast(
            wallCheck.position,
            new Vector2(0.1f, wallCheckHeight),
            0f,
            Vector2.right,
            wallCheckDistance,
            groundLayer
        );

        RaycastHit2D hitLeft = Physics2D.BoxCast(
            wallCheck.position,
            new Vector2(0.1f, wallCheckHeight),
            0f,
            Vector2.left,
            wallCheckDistance,
            groundLayer
        );

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
        else { isTouchingWall = false; wallDirection = 0; }
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
    #endregion

    #region Jump
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

    private void HandleCoyoteTime()
    {
        if (isGrounded) coyoteTimeCounter = coyoteTime;
        else coyoteTimeCounter -= Time.deltaTime;
    }

    private void HandleJump()
    {
        if (InputManager.instance.JumpJustPressed && isWallClinging)
        {
            rb.linearVelocity = new Vector2(-wallDirection * wallJumpHorizontalForce, wallJumpVerticalForce);

            justWallJumped = true;
            wallJumpInputTimer = wallJumpInputLock;
            wallCooldownTimer = wallDetachCooldown;
            isWallClinging = false;
            return;
        }

        if (InputManager.instance.JumpJustPressed)
        {
            if (coyoteTimeCounter > 0f && jumpsRemaining > 0)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsRemaining--;
                coyoteTimeCounter = 0f;
            }
            else if (unlockedDoubleJump && jumpsRemaining > 0 && !isGrounded)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpsRemaining--;
            }
        }

        if (InputManager.instance.JumpReleased && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }
    }

    private void HandleJumpBuffer()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
        }
    }

    private void ApplyBetterJump()
    {
        if (isRayActive || isGrapplingToTarget) return;

        if (rb.linearVelocity.y < 0f)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime);
        }
        else if (rb.linearVelocity.y > 0f && !InputManager.instance.JumpBeingHeld)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime);
        }
        else if (rb.linearVelocity.y > 0f && InputManager.instance.JumpBeingHeld)
        {
            rb.linearVelocity += Vector2.up * (Physics2D.gravity.y * (ascentMultiplier - 1f) * Time.fixedDeltaTime);
        }

        // 👇 FALL SPEED CAP
        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
    }
    #endregion

    #region Dash
    private void HandleDash()
    {
        if (InputManager.instance.DashInput && canDash && !isDashing && !isGrapplingToTarget && !isGrappling)
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
    
    #endregion

    #region Hook Functions
    private void HandleHook()
    {
        if (InputManager.instance.HookInput && !isRayActive && !isGrapplingToTarget && canGrapple)
        {
            float verticalInput = InputManager.instance.MoveInput.y;
            if (verticalInput > 0.1f) rayDirection = Vector2.up;
            else if (verticalInput < -0.1f) rayDirection = Vector2.down;
            else rayDirection = Vector2.right * facingDirection;

            isRayActive = true;
            isGrappling = true;
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
                isGrappling = false;

                if (grappleLineRenderer != null)
                    grappleLineRenderer.enabled = false;
            }
        }

        if (!canGrapple && !isGrapplingToTarget && !isRayActive)
        {
            grappleCooldownTimer -= Time.deltaTime;
            if (grappleCooldownTimer <= 0f)
                canGrapple = true;
        }
    }

    public RaycastHit2D ShootDirectionalRay()
    {
        Vector2 origin = transform.position;
        directionalRayHit = Physics2D.Raycast(origin, rayDirection, directionalRayDistance, grappleLayer);

        // Update line renderer
        if (grappleLineRenderer != null)
        {
            grappleLineRenderer.enabled = true;
            grappleLineRenderer.positionCount = 2;
            grappleLineRenderer.SetPosition(0, origin);

            if (directionalRayHit.collider != null &&
                ((1 << directionalRayHit.collider.gameObject.layer) & grappleLayer) != 0)
            {
                grappleLineRenderer.SetPosition(1, directionalRayHit.point);
            }
            else
            {
                grappleLineRenderer.SetPosition(1, origin + rayDirection * directionalRayDistance);
            }
        }

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

        isGrapplingToTarget = true;
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

            // Update line renderer during grapple
            if (grappleLineRenderer != null)
            {
                grappleLineRenderer.SetPosition(0, transform.position);
            }

            yield return null;
        }

        transform.position = grappleTargetPos;
        isGrapplingToTarget = false;

        // Disable line renderer
        if (grappleLineRenderer != null)
            grappleLineRenderer.enabled = false;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, grappleEndBoost);
        jumpsRemaining = maxJumps;
        canDash = true;
        dashCooldownTimer = 0f;
    }
    
    #endregion

    #region Bounce Function
    private IEnumerator BounceRoutine()
    {
        isRed = true;
        sr.color = Color.red;
        nextBounceTime = Time.time + bounceCooldown;
        
        Debug.Log("isRed = " + isRed);

        yield return new WaitForSeconds(0.5f);

        sr.color = originalColor;
        isRed = false;
    }
    #endregion

    #region Unlock Ability Functions

    public void HandleAbilityUnlocks()
    {
        if (unlockedWallJump)
        {
            wallJumpInputLock = 0.14f;
            wallJumpHorizontalForce = 11f;
            wallJumpVerticalForce = 13f;
        }
    }

    #endregion

    private void OnTriggerStay2D(Collider2D other)
    {
        //Debug.Log("Trigger with: " + other.name);
        //Debug.Log($"Trigger stay with {other.name}, layer = {other.gameObject.layer}");
        if (!isRed) return;

        if (other.gameObject.layer == 8)
        {
            Debug.Log("Bounce detected");
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceEndBoost);
            jumpsRemaining = maxJumps;
            canDash = true;
            dashCooldownTimer = 0f;
        }
    }

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
            Vector3 checkPos = wallCheck.position;
            
            // Draw right box
            Gizmos.DrawWireCube(
                checkPos + Vector3.right * (wallCheckDistance / 2f),
                new Vector3(wallCheckDistance, wallCheckHeight, 0f)
            );
            
            // Draw left box
            Gizmos.DrawWireCube(
                checkPos + Vector3.left * (wallCheckDistance / 2f),
                new Vector3(wallCheckDistance, wallCheckHeight, 0f)
            );
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Damage"))
        {
            TakeDamage();
        }
    }

    private void TakeDamage()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}