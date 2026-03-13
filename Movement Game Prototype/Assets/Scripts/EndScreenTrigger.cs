using UnityEngine;

public class EndScreenTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneController.Instance.LoadScene("End_Scene");
        }
    }
}
