using UnityEngine;

public class FallingPlatform : MonoBehaviour
{
    [Header("Timings")]
    [SerializeField] private float fallTime = 1f;     // Time it takes to turn black

    private SpriteRenderer sr;
    private Collider2D col;
    private Rigidbody2D rb;

    private Color startColor = Color.white;
    private Color endColor = Color.black;

    private bool hasFallen = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

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

        // Wait before respawn
        yield return new WaitForSeconds(1f);

        // Reset color and gravity scale
        rb.bodyType = RigidbodyType2D.Kinematic;
        sr.color = startColor;
    }
}
