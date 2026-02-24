using UnityEngine;
using UnityEngine.Serialization;

public class SideBouncePad : MonoBehaviour
{
    [FormerlySerializedAs("bounceForce")]
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForceUp;

    [SerializeField] private float bounceForceSideways;

    [SerializeField] private float bouncePadTimer;

    [Tooltip("Checked = bounce right, Unchecked = bounce left")]
    [SerializeField] private bool bounceRight = true;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        
        if (player != null)
        {
            player.SetBouncePadDuration(bouncePadTimer);
        }
        
        if (other.CompareTag("Player"))
        {   
            Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
            
            // Reset horizontal velocity for consistent bounce
            rb.linearVelocity = new Vector2(rb.linearVelocity.y, 0f);
            
            // Apply upward impulse
            rb.AddForce(Vector2.up * bounceForceUp, ForceMode2D.Impulse);

            float direction = bounceRight ? 1f : -1f;

            // Force exact sideways launch (most reliable method)
            rb.AddForce(Vector2.right * direction * bounceForceSideways, ForceMode2D.Impulse);
        }
    }
}