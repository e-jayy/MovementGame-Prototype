using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;
    [SerializeField] private Animator transitionAnim;

    [Header("Respawn Settings")]
    public Vector2 respawnPosition;
    public bool hasCustomRespawn = false;
    private float TransitionDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ResetSpawnData()
    {
        hasCustomRespawn = false;
        respawnPosition = Vector2.zero;
    }
    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(TransitionDuration);
        SceneManager.LoadScene(sceneName);
        transitionAnim.SetTrigger("Start");
    }
    public void PlayGame()
    {
        StartCoroutine(LoadTutorial());
        Debug.Log("PlayGame called, loading tutorial...");

    }

    IEnumerator LoadTutorial()
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(TransitionDuration);
        SceneManager.LoadScene(1);
        transitionAnim.SetTrigger("Start");
    }

    public void QuitGame()
    {
        StartCoroutine(QuitGameCoroutine());
    }

    private IEnumerator QuitGameCoroutine()
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(TransitionDuration);
        Application.Quit();
    }

    private IEnumerator LoadNextLevel()
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(TransitionDuration);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        transitionAnim.SetTrigger("Start");
    }

    #region Respawn Methods
    public void SetRespawnPoint(Vector2 position)
    {
        respawnPosition = position;
        hasCustomRespawn = true;
    }

    public void ReloadScene()
    {
        StartCoroutine(ReloadSceneCoroutine());
    }
    public IEnumerator ReloadSceneCoroutine()
    {
        transitionAnim.SetTrigger("End");
        yield return new WaitForSeconds(TransitionDuration);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        transitionAnim.SetTrigger("Start");
    }

    #endregion
}
