using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    public void OnStartPress()
    {
        SceneController.Instance.LoadScene("Level1_Tutorial");
    }

    public void OnQuitPress()
    {
        SceneController.Instance.QuitGame();
    }
}
