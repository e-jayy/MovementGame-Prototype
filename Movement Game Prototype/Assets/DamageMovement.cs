using UnityEngine;
using UnityEngine.SceneManagement;

public class DamageMovement : MonoBehaviour
{
    [Header("Points (Transforms in world or as children)")]
    public Transform pointA;
    public Transform pointB;

    [Header("Settings")]
    public float speed = 1f;
    public float pauseTime = 0f;

    private float t = 0f;
    private bool goingToB = true;
    private bool isPaused = false;

    private Vector2 worldA;
    private Vector2 worldB;
    private float originalZ;

    private void Start()
    {
        // Store world positions so child movement doesn’t break it
        worldA = pointA.position;
        worldB = pointB.position;

        // Keep original Z so the object stays in its correct 2D layer
        originalZ = transform.position.z;
    }

    private void Update()
    {
        if (isPaused)
            return;

        t += (goingToB ? 1 : -1) * speed * Time.deltaTime;
        t = Mathf.Clamp01(t);

        Vector2 newPos = Vector2.Lerp(worldA, worldB, t);
        transform.position = new Vector3(newPos.x, newPos.y, originalZ);

        if (t == 1f || t == 0f)
            StartCoroutine(PauseAndSwitch());
    }

    private System.Collections.IEnumerator PauseAndSwitch()
    {
        isPaused = true;

        if (pauseTime > 0f)
            yield return new WaitForSeconds(pauseTime);

        goingToB = !goingToB;
        isPaused = false;
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Player"))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
