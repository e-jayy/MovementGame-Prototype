using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [SerializeField] private float fallTime = 1f;

    [SerializeField] private Collider2D killTrigger;

    [Header("Particles")]
    [SerializeField] private ParticleSystem rubbleParticles;

    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    private bool hasFallen = false;
    public bool IsFalling { get; private set; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        killTrigger.enabled = false;

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasFallen && collision.collider.CompareTag("Player"))
        {
            StartCoroutine(fallRoutine());
        }
    }

    public void TriggerFall()
    {
        if (!hasFallen)
        {
            StartCoroutine(fallRoutine());
        }
    }

    private System.Collections.IEnumerator fallRoutine()
    {
        hasFallen = true;

        if (rubbleParticles != null)
            rubbleParticles.Play();

        yield return new WaitForSeconds(fallTime);

        if (rubbleParticles != null)
            rubbleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        // Platform fall
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        IsFalling = true;
        killTrigger.enabled = true;
    }

    public void ResetPlatform()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        IsFalling = false;
        hasFallen = false;
        killTrigger.enabled = false;

        if (rubbleParticles != null)
            rubbleParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}