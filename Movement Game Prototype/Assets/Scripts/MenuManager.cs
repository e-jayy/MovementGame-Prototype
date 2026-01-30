using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Objects")]
    [SerializeField] private GameObject _mainMenuCanvasGO;
    [SerializeField] private GameObject _settingsMenuCanvasGO;
    [SerializeField] private GameObject _keyboardMenuCanvasGO;
    [SerializeField] private GameObject _controllerMenuCanvasGO;

    [Header("Player Scripts")]
    [SerializeField] private PlayerController _playerController;

    [Header("First Selected Options")]
    [SerializeField] private GameObject _mainMenuFirst;
    [SerializeField] private GameObject _settingsMenuFirst;
    [SerializeField] private GameObject _keyboardMenuFirst;
    [SerializeField] private GameObject _controllerMenuFirst;

    private bool isPaused;

    private void Start()
    {
        _mainMenuCanvasGO.SetActive(false);
        _settingsMenuCanvasGO.SetActive(false);
        _keyboardMenuCanvasGO.SetActive(false);
        _controllerMenuCanvasGO.SetActive(false);
    }

    private void Update()
    {
        if(InputManager.instance.MenuOpenCloseInput)
        {
            if(!isPaused)
            {
                Pause();
            }
            else
            {
                Unpause();
            }
        }
    }

    #region Pause/Unpause Functions

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;

        _playerController.enabled = false;
        
        OpenMainMenu();
    }

    public void Unpause()
    {
        isPaused = false;
        Time.timeScale = 1f;

        _playerController.enabled = true;

        CloseAllMenus();
    }
    
    #endregion

    #region Canvas Activation Functions

    private void OpenMainMenu()
    {
        _mainMenuCanvasGO.SetActive(true);
        _settingsMenuCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(_mainMenuFirst);
    }

    private void OpenSettingsMenuHandle()
    {
        _mainMenuCanvasGO.SetActive(false);
        _settingsMenuCanvasGO.SetActive(true);

        EventSystem.current.SetSelectedGameObject(_settingsMenuFirst);
    }

    private void OpenKeyboardConfigPressHandle()
    {
        _keyboardMenuCanvasGO.SetActive(true);
        _settingsMenuCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(_keyboardMenuFirst);
    }

    private void OpenControllerConfigPressHandle()
    {
        _controllerMenuCanvasGO.SetActive(true);
        _settingsMenuCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(_controllerMenuFirst);
    }

    private void CloseAllMenus()
    {
        _mainMenuCanvasGO.SetActive(false);
        _settingsMenuCanvasGO.SetActive(false);
        _keyboardMenuCanvasGO.SetActive(false);
        _controllerMenuCanvasGO.SetActive(false);

        EventSystem.current.SetSelectedGameObject(null);
    }

    #endregion

    #region Main Menu Button Functions

    public void OnSettingsPress()
    {
        OpenSettingsMenuHandle();
    }

    public void OnKeyboardConfigPress()
    {
        OpenKeyboardConfigPressHandle();
    }

    public void OnControllerConfigPress()
    {
        OpenControllerConfigPressHandle();
    }

    public void OnResumePress()
    {
        Unpause();
    }

    #endregion

    #region Settings Menu Button Functions

    public void OnSettingsBackPress()
    {
        OpenMainMenu();
    }

    public void OnKeyboardConfigBackPress()
    {
        _keyboardMenuCanvasGO.SetActive(false);
        _settingsMenuCanvasGO.SetActive(true);

        EventSystem.current.SetSelectedGameObject(_settingsMenuFirst);
    }

    public void OnControllerConfigBackPress()
    {
        _controllerMenuCanvasGO.SetActive(false);
        _settingsMenuCanvasGO.SetActive(true);

        EventSystem.current.SetSelectedGameObject(_settingsMenuFirst);
    }

    #endregion
}
