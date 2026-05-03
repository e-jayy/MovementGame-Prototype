using System.Collections.Generic;
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

    [Header("Particles")]
    [SerializeField] private ParticleSystem activeParticles;
    [SerializeField] private ParticleSystem inactiveParticles;

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

        // Start with inactive particles playing
        if (activeParticles != null)
            activeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (inactiveParticles != null)
            inactiveParticles.Play();
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

            if (!isActive)
            {
                Collider2D col = GetComponent<Collider2D>();
                List<Collider2D> hits = new List<Collider2D>();
                col.Overlap(hits);
                foreach (Collider2D hit in hits)
                {
                    if (hit != null && hit.CompareTag("Player"))
                        hit.GetComponent<PlayerController>()?.SetBubbleState(false, Vector2.zero);
                }
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isActive && collision.CompareTag("Player"))
            collision.GetComponent<PlayerController>()?.SetBubbleState(true, GetLaunchVelocity(collision.GetComponent<Rigidbody2D>()));
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (isActive && collision.CompareTag("Player"))
            collision.GetComponent<PlayerController>()?.SetBubbleState(true, GetLaunchVelocity(collision.GetComponent<Rigidbody2D>()));
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            collision.GetComponent<PlayerController>()?.SetBubbleState(false, Vector2.zero);
    }

    Vector2 GetLaunchVelocity(Rigidbody2D playerRb)
    {
        Vector2 forceDirection = GetLaunchDirection();

        if (launchDirection == Direction.Up || launchDirection == Direction.Down)
            return new Vector2(playerRb.linearVelocity.x, forceDirection.y * launchForce);
        else
            return new Vector2(forceDirection.x * launchForce, playerRb.linearVelocity.y);
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
        if (spriteRenderer != null)
            SetAlpha(isActive ? activeAlpha : inactiveAlpha);

        if (isActive)
        {
            if (activeParticles != null)   activeParticles.Play();
            if (inactiveParticles != null) inactiveParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
        else
        {
            if (activeParticles != null)   activeParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (inactiveParticles != null) inactiveParticles.Play();
        }
    }

    void SetAlpha(byte alpha)
    {
        if (spriteRenderer == null) return;
        Color color = spriteRenderer.color;
        color.a = alpha / 255f;
        spriteRenderer.color = color;
    }
}