using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public void OnStartPress()
    {
        SceneController.Instance.LoadScene("Level_Tutorial_Scene");
    }

    public void OnQuitPress()
    {
        SceneController.Instance.QuitGame();
    }
}
