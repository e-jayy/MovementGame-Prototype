using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager instance;

    public bool MenuOpenCloseInput { get; private set; }

    public Vector2 MoveInput { get; private set; }
    public bool JumpJustPressed { get; private set; }
    public bool JumpBeingHeld { get; private set; }
    public bool JumpReleased { get; private set; }
    public bool DashInput { get; private set; }
    public bool HookInput { get; private set; }
    public bool BounceInput { get; private set; }


    private InputAction _menuOpenCloseAction;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _dashAction;
    private InputAction _hookAction;
    private InputAction _bounceAction;
    private PlayerInput _playerInput;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }

        _playerInput = GetComponent<PlayerInput>();

        SetupInputActions();
    }

    private void Update()
    {
        UpdateInputs();
    }

    private void SetupInputActions()
    {
        _moveAction = _playerInput.actions["Move"];
        _jumpAction = _playerInput.actions["Jump"];
        _dashAction = _playerInput.actions["Dash"];
        _hookAction = _playerInput.actions["Hook"];
        _bounceAction = _playerInput.actions["Bounce"];
        _menuOpenCloseAction = _playerInput.actions["MenuOpenClose"];
    }

    private void UpdateInputs()
    {
        MoveInput = _moveAction.ReadValue<Vector2>();

        JumpJustPressed = _jumpAction.WasPressedThisFrame();
        JumpBeingHeld = _jumpAction.IsPressed();
        JumpReleased = _jumpAction.WasReleasedThisFrame();

        DashInput = _dashAction.WasPressedThisFrame();
        HookInput = _hookAction.WasPressedThisFrame();
        BounceInput = _bounceAction.WasPressedThisFrame();

        MenuOpenCloseInput = _menuOpenCloseAction.WasPressedThisFrame();
    }
}
