using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [Header("Bounce Settings")] public float bounceForce;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

            // Reset horizontal velocity for consistent bounce
            rb.linearVelocity = new Vector2(rb.linearVelocity.y, 0f);

            // Apply upward impulse
            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
        }
    }
}