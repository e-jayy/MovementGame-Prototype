using UnityEngine;

public class Bubble_Damage : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float activeTime = 2f;
    [SerializeField] private float inactiveTime = 1f;

    [Header("Visual Feedback")]
    [SerializeField] private byte activeAlpha = 200;
    [SerializeField] private byte inactiveAlpha = 60;

    private bool isActive = true;
    private float timer = 0f;
    private Collider2D col;
    private SpriteRenderer sr;

    void Start()
    {
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        timer = activeTime;
        UpdateVisuals();
    }

    void Update()
    {
        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            isActive = !isActive;
            timer = isActive ? activeTime : inactiveTime;
            col.enabled = isActive;
            UpdateVisuals();
        }
    }

    void UpdateVisuals()
    {
        if (sr == null) return;

        Color color = sr.color;
        color.a = isActive ? activeAlpha / 255f : inactiveAlpha / 255f;
        sr.color = color;
    }
}
