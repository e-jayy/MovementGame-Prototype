using UnityEngine;

public class Bubble_Move : MonoBehaviour
{
    [Header("Startup Delay")]
    [SerializeField] private float startDelay = 0f;

    [Header("Timing")]
    [SerializeField] private float activeTime = 2f;
    [SerializeField] private float inactiveTime = 1f;

    [Header("Launch Settings")]
    [SerializeField] private float launchForce = 15f;
    [SerializeField] private Direction launchDirection = Direction.Up;

    [Header("Visual Feedback")]
    private SpriteRenderer spriteRenderer;
    [SerializeField] private byte activeAlpha = 200;
    [SerializeField] private byte inactiveAlpha = 60;

    private bool isActive = true;
    private float timer = 0f;
    private float startDelayTimer = 0f;
    private bool hasStarted = false;

    public enum Direction { Up, Down, Left, Right }

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startDelayTimer = startDelay;
        isActive = false;
        SetAlpha(inactiveAlpha);
    }

    void Update()
    {
        if (!hasStarted)
        {
            startDelayTimer -= Time.deltaTime;
            if (startDelayTimer <= 0f)
            {
                hasStarted = true;
                isActive = true;
                timer = activeTime;
                UpdateVisuals();
            }
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            isActive = !isActive;
            timer = isActive ? activeTime : inactiveTime;
            UpdateVisuals();
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActive && collision.CompareTag("Player"))
        {
            LaunchPlayer(collision.GetComponent<Rigidbody2D>());
        }
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (isActive && collision.CompareTag("Player"))
        {
            Rigidbody2D playerRb = collision.GetComponent<Rigidbody2D>();
            if (playerRb == null) return;

            Vector2 forceDirection = GetLaunchDirection();
            float currentVelocityInDirection = Vector2.Dot(playerRb.linearVelocity, forceDirection);

            if (currentVelocityInDirection < launchForce * 0.5f)
            {
                LaunchPlayer(playerRb);
            }
        }
    }

    void LaunchPlayer(Rigidbody2D playerRb)
    {
        if (playerRb == null) return;

        Vector2 forceDirection = GetLaunchDirection();

        if (launchDirection == Direction.Up || launchDirection == Direction.Down)
        {
            playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 0);
            playerRb.linearVelocity += forceDirection * launchForce;
        }
        else
        {
            playerRb.linearVelocity = new Vector2(0, playerRb.linearVelocity.y);
            playerRb.AddForce(forceDirection * launchForce * 100f, ForceMode2D.Force);
        }
    }

    Vector2 GetLaunchDirection()
    {
        return launchDirection switch
        {
            Direction.Up    => Vector2.up,
            Direction.Down  => Vector2.down,
            Direction.Left  => Vector2.left,
            Direction.Right => Vector2.right,
            _               => Vector2.up
        };
    }

    void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        SetAlpha(isActive ? activeAlpha : inactiveAlpha);
    }

    void SetAlpha(byte alpha)
    {
        if (spriteRenderer == null) return;
        Color color = spriteRenderer.color;
        color.a = alpha / 255f;
        spriteRenderer.color = color;
    }
}