using UnityEngine;

public class CrumblingPlatform : MonoBehaviour
{
    [Header("Timings")]
    [SerializeField] private float crumbleTime = 1f;     // Time it takes to turn black
    [SerializeField] private float respawnTime = 2f;     // Time before platform returns

    private SpriteRenderer sr;
    private Collider2D col;

    private Color startColor = Color.white;
    private Color endColor = Color.black;

    private bool isCrumbing = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isCrumbing && collision.collider.CompareTag("Player"))
        {
            StartCoroutine(CrumbleRoutine());
        }
    }


    private System.Collections.IEnumerator CrumbleRoutine()
    {
        isCrumbing = true;

        float t = 0f;
        sr.color = startColor;

        // Fade from white → black
        while (t < crumbleTime)
        {
            t += Time.deltaTime;
            float lerp = t / crumbleTime;
            sr.color = Color.Lerp(startColor, endColor, lerp);
            yield return null;
        }

        // Disable platform
        sr.enabled = false;
        col.enabled = false;

        // Wait before respawn
        yield return new WaitForSeconds(respawnTime);

        // Reset
        sr.color = startColor;
        sr.enabled = true;
        col.enabled = true;

        isCrumbing = false;
    }
}
