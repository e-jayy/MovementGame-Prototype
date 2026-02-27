using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [SerializeField] private float fallTime = 1f;     // Time it takes to turn black

    [SerializeField] private Collider2D killTrigger;

    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    private Color startColor = Color.white;
    private Color endColor = Color.black;

    private bool hasFallen = false;
    public bool IsFalling { get; private set; }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        killTrigger.enabled = false;

        rb.bodyType = RigidbodyType2D.Kinematic; // Prevent physics movement
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

        float t = 0f;
        sr.color = startColor;

        // Fade from white → black
        while (t < fallTime)
        {
            t += Time.deltaTime;
            float lerp = t / fallTime;
            sr.color = Color.Lerp(startColor, endColor, lerp);
            yield return null;
        }

        // Platform fall
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        IsFalling = true;
        killTrigger.enabled = true;

        // yield return new WaitForSeconds(1f);

        // ResetPlatform;
    }

    public void ResetPlatform()
    {
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        sr.color = startColor;
        IsFalling = false;
        killTrigger.enabled = false; 
    }
}
