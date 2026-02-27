using UnityEngine;

public class FallingPlatformTrigger : MonoBehaviour
{
    private FallingPlatform parentPlatform;

    private void Awake()
    {
        parentPlatform = GetComponentInParent<FallingPlatform>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            parentPlatform.TriggerFall();
        }
    }
}