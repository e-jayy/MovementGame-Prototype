using UnityEngine;

public class Bubble_Damage : MonoBehaviour
{
    [Header("Startup Delay")]
    [SerializeField] private float startDelay = 0f;

    [Header("Timing")]
    [SerializeField] private float activeTime = 2f;
    [SerializeField] private float inactiveTime = 1f;

    [Header("Visual Feedback")]
    [SerializeField] private byte activeAlpha = 200;
    [SerializeField] private byte inactiveAlpha = 60;

    [Header("Particles")]
    [SerializeField] private ParticleSystem activeParticles;
    [SerializeField] private ParticleSystem inactiveParticles;

    private bool isActive = true;
    private float timer = 0f;
    private float startDelayTimer = 0f;
    private bool hasStarted = false;
    private Collider2D col;
    //private SpriteRenderer sr;

    void Start()
    {
        col = GetComponent<Collider2D>();
        //sr = GetComponent<SpriteRenderer>();
        startDelayTimer = startDelay;

        col.enabled = false;
        //SetAlpha(inactiveAlpha);

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
                timer = activeTime;
                col.enabled = isActive;
                UpdateVisuals();
            }
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            isActive = !isActive;
            timer = isActive ? activeTime : inactiveTime;
            col.enabled = isActive;
            UpdateVisuals();
        }
    }

    void UpdateVisuals()
    {
        // if (sr != null)
        //     SetAlpha(isActive ? activeAlpha : inactiveAlpha);

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

    // void SetAlpha(byte alpha)
    // {
    //     if (sr == null) return;
    //     Color color = sr.color;
    //     color.a = alpha / 255f;
    //     sr.color = color;
    // }
}