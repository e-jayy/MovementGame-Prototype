using UnityEngine;

public class BouncePad : MonoBehaviour
{
    public float bounceForce = 20f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

            if (rb != null && rb.linearVelocity.y <= 0f) // Only bounce if falling
            {
                // Reset vertical velocity for consistent bounce
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

                // Apply upward impulse
                rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
            }
        }
    }
}