using UnityEngine;

public class EndScreenMenu : MonoBehaviour
{
    public void OnStartPress()
    {
        SceneController.Instance.ResetSpawnData();
        SceneController.Instance.LoadScene("Start_Scene");
    }

    public void OnQuitPress()
    {
        SceneController.Instance.QuitGame();
    }
}
