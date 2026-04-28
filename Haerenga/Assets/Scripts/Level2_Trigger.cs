using UnityEngine;

public class Level2_Trigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(other.gameObject);
            SceneController.Instance.ResetSpawnData();
            SceneController.Instance.LoadScene("Level2_Overgrown");
            PlayerManager.Instance.UnlockHook();
        }
    }
}
