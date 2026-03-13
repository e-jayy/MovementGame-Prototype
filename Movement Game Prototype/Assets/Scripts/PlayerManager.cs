using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }

    [Header("Scene Management Settings")]
    public Vector2 respawnPosition;
    public bool hasCustomRespawn = false;

    [Header("Unlocked Abilities")]
    [SerializeField] private bool dashUnlocked;
    [SerializeField] private bool wallJumpUnlocked;
    [SerializeField] private bool doubleJumpUnlocked;
    [SerializeField] private bool hookUnlocked;

    public bool DashUnlocked => dashUnlocked;
    public bool WallJumpUnlocked => wallJumpUnlocked;
    public bool DoubleJumpUnlocked => doubleJumpUnlocked;
    public bool HookUnlocked => hookUnlocked;

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

    #region Ability Unlock Methods
    
    public void UnlockDash()       => dashUnlocked = true;
    public void UnlockWallJump()   => wallJumpUnlocked = true;
    public void UnlockDoubleJump() => doubleJumpUnlocked = true;
    public void UnlockHook()       => hookUnlocked = true;
    

    public void ResetAbilities()
    {
        dashUnlocked = false;
        wallJumpUnlocked = false;
        doubleJumpUnlocked = false;
        hookUnlocked = false;
    }

    #endregion

    #region Respawn Methods
    public void SetRespawnPoint(Vector2 position)
    {
        respawnPosition = position;
        hasCustomRespawn = true;
    }

    public void ReloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    #endregion

}