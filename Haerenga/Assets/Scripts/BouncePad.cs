using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [Header("Bounce Settings")] public float bounceForce;

    [SerializeField] private float bouncePadTimer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

            if (player != null)
        {
            player.SetBouncePadDuration(bouncePadTimer);
        }

            // Reset horizontal velocity for consistent bounce
            rb.linearVelocity = new Vector2(rb.linearVelocity.y, 0f);

            // Apply upward impulse
            rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
        }
    }
}