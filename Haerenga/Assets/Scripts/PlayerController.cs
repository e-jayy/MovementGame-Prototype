using System.Collections;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    
    [Header("Unlocked Abilities")]
    //Add these values into a player manager singleton later
    [SerializeField] private bool unlockedDash;
    [SerializeField] private bool unlockedHook;
    [SerializeField] private bool unlockedWallJump;
    [SerializeField] private bool unlockedDoubleJump;

    
    
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 7f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15f;
    [SerializeField] private float fallMultiplier = 0.5f;
    [SerializeField] private int maxJumps = 2;
    private bool shouldJump;
    private bool shouldWallJump;
    private float bouncePadDuration;
    private float bouncePadDurationHorizontal;
    private bool bounceLock;

    [SerializeField] private float lowJumpMultiplier = 0.5f;  
    [SerializeField] private float maxFallSpeed = -12f;  

    [Header("Wall Cling")]
    [SerializeField] private PhysicsMaterial2D wallClingMaterial;
    [SerializeField] private PhysicsMaterial2D normalMaterial;
    private float wallClingStamina;
    [SerializeField] private float wallClingStaminaRegenRate = 1f;
    [SerializeField] private float maxWallClingStamina = 3f;
    
    [SerializeField] private Transform wallCheck;
    [SerializeField] private float wallCheckDistance = 0.6f;
    [SerializeField] private float wallCheckHeight = 0.5f;
    private float wallJumpHorizontalForce;
    private float wallJumpVerticalForce;
    private float wallJumpInputLock;
    [SerializeField] private float wallDetachCooldown = 0.12f;

    [Header("Wall Jump")]
    [SerializeField] private float OldwallJumpHorizontalForce = 9f;
    [SerializeField] private float OldwallJumpVerticalForce = 7f;
    [SerializeField] private float OldwallJumpInputLock = 0.35f;

    [Header("Wall Jump Ability Upgrade")]
    
    [SerializeField] private float NewwallJumpHorizontalForce = 9f;
    [SerializeField] private float NewwallJumpVerticalForce = 9f;
    [SerializeField] private float NewwallJumpInputLock = 0.14f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask onewayLayer;

    [Header("Dash Settings")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    [Header("Coyote Time & Jump Buffer")]
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    // Grapple
    [Header("Grapple Settings")]
    [SerializeField] private float directionalRayDistance = 10f;
    [SerializeField] private float directionalRayDuration = 1f;
    [SerializeField] private LayerMask grappleLayer;
    [SerializeField] private float grappleDelay = 0.2f;
    [SerializeField] private float grappleLerpDuration = 0.3f;
    [SerializeField] private float grappleEndBoost = 8f;
    [SerializeField] private float grappleCooldown = 0.3f;
    [SerializeField] private LineRenderer grappleLineRenderer;
    [SerializeField] private Transform hookProjectile;

    [Header("Bounce Settings")]
    [SerializeField] private LayerMask bounceLayer;
    [SerializeField] private float bounceCooldown = 1f;
    private float nextBounceTime = 0f;
    [SerializeField] private float bounceEndBoost = 12f;

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

    private Rigidbody2D rb;
    private Collider2D coll;
    private Animator animator;

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
    public float wallJumpInputTimer = 0f;

    // Facing direction
    private int facingDirection = 1;
    private Vector2 rayDirection;

    // Sprite flash
    private SpriteRenderer sr;
    private Color originalColor;
    private bool isRed = false;
    
    //Bubble Column
    private bool inBubble = false;
    private Vector2 bubbleLaunchVelocity;

    private void Start()
{
    if (SceneController.Instance != null && SceneController.Instance.hasCustomRespawn)
    {
        transform.position = SceneController.Instance.respawnPosition;
        SceneController.Instance.hasCustomRespawn = false;
    }
}

    private void Awake()
    {
        coll = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        originalGravityScale = rb.gravityScale;
        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
        animator = GetComponent<Animator>();

        wallClingStamina = maxWallClingStamina;

        DisableGrappleVisuals();
    }

    private void Update()
    {
        HandleTimers();

        GetHorizontalInput();
        CheckGround();
        CheckWall();

        HandleHook();
        HandleCoyoteTime();
        HandleJumpBuffer();
        HandleDash();
        HandleWallClingStamina();

        HandleAnimation();
        
        HandleAbilityUnlocks();

        SetBounceLock();

        horizontalInputEffective = (wallJumpInputTimer > 0f) ? 0f : horizontalInput;
        //If the wall-jump input lock timer is still running, ignore horizontal input. Otherwise, use the players real horizontal input

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
            HandleMovementLock();
        }
    }

    private void HandleTimers()
    {
        // if (InputManager.instance.BounceInput && Time.time >= nextBounceTime)
        //     StartCoroutine(BounceRoutine());

        if (wallCooldownTimer >= 0f) wallCooldownTimer -= Time.deltaTime;
        if (wallJumpInputTimer >= 0f) wallJumpInputTimer -= Time.deltaTime;
        if (bouncePadDuration >= 0f) bouncePadDuration -= Time.deltaTime;
        if (bouncePadDurationHorizontal >= 0f) bouncePadDurationHorizontal -= Time.deltaTime;
        if (jumpBufferCounter >= 0f) jumpBufferCounter -= Time.deltaTime;
        if (!canDash) dashCooldownTimer -= Time.deltaTime;

        if (justWallJumped && wallJumpInputTimer <= 0f)
            justWallJumped = false;
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

    private void HandleMovementLock()
    {
        if (wallJumpInputTimer > 0f) return;
        if (bounceLock) return;
        if (inBubble)
        {
            rb.linearVelocity = bubbleLaunchVelocity;
            return;
        }
        if (isRayActive || isGrapplingToTarget)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);
    }

    #region Wall Cling Methods
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
        }
        else
        {
            isWallClinging = false;
        }
    }

    private void HandleWallClingStamina()
    {
        if (isWallClinging)
        {
            // Drain stamina
            wallClingStamina -= Time.deltaTime;
            
            // Out of stamina - fall off wall
            if (wallClingStamina <= 0f)
            {
                wallClingStamina = 0f;
                coll.sharedMaterial = normalMaterial;
            }
        }
        else
        {
            // Regenerate stamina when not wall clinging
            wallClingStamina += wallClingStaminaRegenRate * Time.deltaTime;
            
            // Clamp to max
            if (wallClingStamina >= maxWallClingStamina)
            {
                wallClingStamina = maxWallClingStamina;
            }
        }

        if (wallClingStamina > 0f)
        {
            coll.sharedMaterial = wallClingMaterial;
        }
        else
        {
            coll.sharedMaterial = normalMaterial;
        }
    }
    #endregion

    #region Jump
    private void CheckGround()
    {
        wasGrounded = isGrounded;
        isGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, onewayLayer | groundLayer);

        if (isGrounded || isWallClinging) jumpsRemaining = maxJumps;

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
    private void HandleJumpBuffer()
    {
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
        }
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
            if(bouncePadDuration <= 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * lowJumpMultiplier);
                //Debug.Log("Jump released, cutting jump height");
            }
        }

        if (rb.linearVelocity.y < 0f && !isWallClinging)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * fallMultiplier);
        }

        if (rb.linearVelocity.y < maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
        }
    }
    #endregion

    #region oldJump
    // private void HandleJump()
    // {
    //     if (InputManager.instance.JumpJustPressed && isWallClinging)
    //     {
    //         shouldWallJump = true;
    //         justWallJumped = true;
    //         wallJumpInputTimer = wallJumpInputLock;
    //         wallCooldownTimer = wallDetachCooldown;
    //         isWallClinging = false;
    //         return;
    //     }

    //     if (InputManager.instance.JumpJustPressed)
    //     {
    //         if (coyoteTimeCounter > 0f && jumpsRemaining > 0)
    //         {
    //             shouldJump = true;
    //             jumpsRemaining--;
    //             coyoteTimeCounter = 0f;
    //         }
    //         else if (unlockedDoubleJump && jumpsRemaining > 0 && !isGrounded)
    //         {
    //             shouldJump = true;
    //             jumpsRemaining--;
    //         }
    //     }

    //     // Early jump release for variable height
    //     // if (InputManager.instance.JumpReleased && rb.linearVelocity.y > 0f)
    //     //     {
    //     //         rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * fallMultiplier);
    //     //     }
    // }

    // private void ApplyJump()
    // {
    //     if (isRayActive || isGrapplingToTarget) return;

    //     // Apply wall jump
    //     if (shouldWallJump)
    //     {
    //         rb.linearVelocity = new Vector2(-wallDirection * wallJumpHorizontalForce, wallJumpVerticalForce);
    //         shouldWallJump = false;
    //     }
        
    //     // Apply regular jump
    //     if (shouldJump)
    //     {
    //         rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    //         shouldJump = false;
    //     }


    //     // Falling
    //     if (rb.linearVelocity.y < 0f && InputManager.instance.JumpBeingHeld)
    //     {
    //         rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * fallMultiplier);
    //     }

    //     // Rising and released
    //     else if (rb.linearVelocity.y > 0f && !InputManager.instance.JumpBeingHeld)
    //     {
    //         rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * fallMultiplier);
    //     }

    //     // Clamp max fall speed
    //     if (rb.linearVelocity.y < maxFallSpeed)
    //     {
    //         rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxFallSpeed);
    //     }
    // }

    #endregion

    #region Dash
    private void HandleDash()
    {
        if (InputManager.instance.DashInput && canDash && !isDashing && !isGrapplingToTarget && !isGrappling && unlockedDash)
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
        if (InputManager.instance.HookInput && !isRayActive && !isGrapplingToTarget && canGrapple && unlockedHook)
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

                DisableGrappleVisuals();
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

        RaycastHit2D grappleHit = Physics2D.Raycast(origin, rayDirection, directionalRayDistance, grappleLayer);
        RaycastHit2D groundHit = Physics2D.Raycast(origin, rayDirection, directionalRayDistance, groundLayer);

        bool groundIsBlocking = groundHit.collider != null &&
                                (grappleHit.collider == null || groundHit.distance < grappleHit.distance);

        // Determine the end point of the line
        Vector2 lineEnd;
        if (groundIsBlocking)
            lineEnd = groundHit.point;
        else if (grappleHit.collider != null)
            lineEnd = grappleHit.point;
        else
            lineEnd = origin + rayDirection * directionalRayDistance;

        // Start the animated line
        StartCoroutine(AnimateGrappleLine(origin, lineEnd));

        // Only grapple if nothing is blocking
        if (!groundIsBlocking && grappleHit.collider != null &&
            ((1 << grappleHit.collider.gameObject.layer) & grappleLayer) != 0)
        {
            directionalRayHit = grappleHit;
            StartCoroutine(GrappleToTarget(grappleHit.point));
        }

        return directionalRayHit;
    }

    [SerializeField] private float grappleLineSpeed = 30f; // How fast the line extends

    private IEnumerator AnimateGrappleLine(Vector2 origin, Vector2 target)
    {
        if (grappleLineRenderer == null) yield break;

        grappleLineRenderer.enabled = true;
        grappleLineRenderer.positionCount = 2;

        // Enable and orient the hook projectile
        if (hookProjectile != null)
        {
            hookProjectile.gameObject.SetActive(true);

            // Rotate hook to face the direction it's traveling
            float angle = Mathf.Atan2(rayDirection.y, rayDirection.x) * Mathf.Rad2Deg;
            hookProjectile.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        float distance = Vector2.Distance(origin, target);
        float elapsed = 0f;
        float duration = distance / grappleLineSpeed;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            Vector2 currentEnd = Vector2.Lerp(origin, target, t);
            grappleLineRenderer.SetPosition(0, transform.position);
            grappleLineRenderer.SetPosition(1, currentEnd);

            // Hook sits at the tip of the line
            if (hookProjectile != null)
                hookProjectile.position = currentEnd;

            yield return null;
        }

        // Snap to final position
        grappleLineRenderer.SetPosition(0, transform.position);
        grappleLineRenderer.SetPosition(1, target);

        if (hookProjectile != null)
            hookProjectile.position = target;
    }

    private void DisableGrappleVisuals()
    {
        if (grappleLineRenderer != null)
            grappleLineRenderer.enabled = false;

        if (hookProjectile != null)
            hookProjectile.gameObject.SetActive(false);
    }
    

    private IEnumerator GrappleToTarget(Vector2 targetPos)
    {
        yield return new WaitForSeconds(grappleDelay);

        isGrapplingToTarget = true;
        grappleStartPos = transform.position;
        grappleTargetPos = targetPos; // Already the exact hit point, no adjustment needed

        grappleLerpTimer = 0f;

        while (grappleLerpTimer < grappleLerpDuration)
        {
            grappleLerpTimer += Time.deltaTime;
            float t = grappleLerpTimer / grappleLerpDuration;

            Vector2 newPos = Vector2.Lerp(grappleStartPos, grappleTargetPos, t);
            transform.position = newPos;
            rb.linearVelocity = Vector2.zero;

            if (grappleLineRenderer != null)
            {
                grappleLineRenderer.SetPosition(0, transform.position);
                grappleLineRenderer.SetPosition(1, grappleTargetPos);
            }

            if (hookProjectile != null)
                hookProjectile.position = grappleTargetPos;

            yield return null;
        }

        transform.position = grappleTargetPos;
        isGrapplingToTarget = false;

        DisableGrappleVisuals();

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, grappleEndBoost);
        jumpsRemaining = maxJumps;
        canDash = true;
        dashCooldownTimer = 0f;
    }
    
    #endregion

    #region Bounce Function
    // private IEnumerator BounceRoutine()
    // {
    //     isRed = true;
    //     sr.color = Color.red;
    //     nextBounceTime = Time.time + bounceCooldown;
        
    //     Debug.Log("isRed = " + isRed);

    //     yield return new WaitForSeconds(0.5f);

    //     sr.color = originalColor;
    //     isRed = false;
    // }
    #endregion

    #region Unlock Ability Functions

    public void HandleAbilityUnlocks()
    {
        if (PlayerManager.Instance.WallJumpUnlocked)
        {
            unlockedWallJump = true;
            coll.sharedMaterial = wallClingMaterial;

        }
        else if (!PlayerManager.Instance.WallJumpUnlocked)
        {
            unlockedWallJump = false;
        }
        UnlockWallJump();

        if (PlayerManager.Instance.DoubleJumpUnlocked)
        {
            unlockedDoubleJump = true;
        }

        if (PlayerManager.Instance.HookUnlocked)
        {
            unlockedHook = true;
        }

        if (PlayerManager.Instance.DashUnlocked)
        {
            unlockedDash = true;
        }
    }

    public void UnlockWallJump()
    {
        if (unlockedWallJump)
        {
            wallJumpHorizontalForce = NewwallJumpHorizontalForce;
            wallJumpVerticalForce = NewwallJumpVerticalForce;
            wallJumpInputLock = NewwallJumpInputLock;
        }
        else if (!unlockedWallJump)
        {
            wallJumpHorizontalForce = OldwallJumpHorizontalForce;
            wallJumpVerticalForce = OldwallJumpVerticalForce;
            wallJumpInputLock = OldwallJumpInputLock;
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


    #region Damake and Death
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Damage"))
        {
            TakeDamage();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Damage"))
        {
            TakeDamage();
        }
    }
    
    #endregion
    public void SetBouncePadDuration(float time)
    {
        bouncePadDuration = time;
    }

    public void SetBouncePadDurationHorizontal(float time)
    {
        bouncePadDurationHorizontal = time;
    }

    public void SetBounceLock()
    {
        if (bouncePadDurationHorizontal >= 0f)
        {
            bounceLock = true;
        } else
        {
            bounceLock = false;
        }
    }

    public void SetBubbleState(bool state, Vector2 velocity)
    {
        inBubble = state;
        bubbleLaunchVelocity = velocity;
    }

    public void TakeDamage()
    {
        SceneController.Instance.ReloadScene();
    }

    private void HandleAnimation()
    {
        if(facingDirection == 1)
        {
            sr.flipX = false;
        }
        else if(facingDirection == -1)
        {
            sr.flipX = true;
        }
        animator.SetFloat("Horizontal Speed", Mathf.Abs(InputManager.instance.MoveInput.x));
        animator.SetBool("Horizontal Input", InputManager.instance.MoveInput.x != 0);
        animator.SetFloat("Vertical Velocity", rb.linearVelocity.y);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsWallClinging", isWallClinging);
    }
}