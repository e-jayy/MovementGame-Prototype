using UnityEngine;

public class FallingPlatformKillTrigger : MonoBehaviour
{
    private FallingPlatform parentPlatform;

    private void Awake()
    {
        parentPlatform = GetComponentInParent<FallingPlatform>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!parentPlatform.IsFalling)
            return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage();
            }
        }
        if (other.gameObject.layer == LayerMask.NameToLayer("groundLayer"))
        {
            parentPlatform.ResetPlatform();
        }
    }
}