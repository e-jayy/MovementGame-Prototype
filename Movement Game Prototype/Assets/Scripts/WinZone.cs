using UnityEngine;

public class WinZone : MonoBehaviour
{
    [SerializeField] private GameObject winTextUI; // Drag your UI text here

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            winTextUI.SetActive(true);
        }
    }
}