using UnityEngine;

public class ParallaxBackground : MonoBehaviour
{
    [Header("Parallax Settings")]
    [SerializeField] [Range(0f, 1f)] private float parallaxX = 0.1f; // Lower = less movement
    [Range(0f, 1f)] [SerializeField] private float parallaxY = 0.05f; // Set to 0 to lock vertical
    [SerializeField] private bool lockVertical = false;

    private Transform cam;
    private Vector3 lastCamPos;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;
    }

    void LateUpdate()
    {
        Vector3 camDelta = cam.position - lastCamPos;

        float moveX = camDelta.x * parallaxX;
        float moveY = lockVertical ? 0f : camDelta.y * parallaxY;

        transform.position += new Vector3(moveX, moveY, 0f);

        lastCamPos = cam.position;
    }
}