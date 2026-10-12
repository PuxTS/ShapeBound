using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputReader : MonoBehaviour, PlayerControl.IPlayerActions
{
    private PlayerControl inputActions;

    // Events for other scripts to subscribe to
    public event Action<Vector2> OnAimEvent;
    public event Action OnLaunchPressedEvent;
    public event Action OnLaunchReleasedEvent;
    public event Action<float> OnSpinEvent;
    public event Action<float> OnMoveEvent;
    public event Action OnResetSpinEvent;
    public event Action<int> OnShapeShiftEvent; // 1 = Circle, 2 = Box, 3 = Spike
    public event Action OnToggleCameraEvent;   // Fired when SwitchCam is pressed

    public Vector2 AimScreenPosition { get; private set; }
    public float SpinInput { get; private set; }
    public float MoveInput { get; private set; }

    private void OnEnable()
    {
        if (inputActions == null)
        {
            inputActions = new PlayerControl();
            inputActions.Player.SetCallbacks(this);
        }
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            inputActions.Player.Disable();
        }
    }

    // --- Interface Callbacks ---
    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<float>();
        OnMoveEvent?.Invoke(MoveInput);
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        AimScreenPosition = context.ReadValue<Vector2>();
        OnAimEvent?.Invoke(AimScreenPosition);
    }

    public void OnLaunch(InputAction.CallbackContext context)
    {
        if (context.started) OnLaunchPressedEvent?.Invoke();
        if (context.canceled) OnLaunchReleasedEvent?.Invoke();
    }

    public void OnSpin(InputAction.CallbackContext context)
    {
        SpinInput = context.ReadValue<float>();
        OnSpinEvent?.Invoke(SpinInput);
    }

    public void OnResetSpin(InputAction.CallbackContext context)
    {
        if (context.started) OnResetSpinEvent?.Invoke();
    }

    public void OnShiftCircle(InputAction.CallbackContext context)
    {
        if (context.started) OnShapeShiftEvent?.Invoke(1);
    }

    public void OnShiftBox(InputAction.CallbackContext context)
    {
        if (context.started) OnShapeShiftEvent?.Invoke(2);
    }

    public void OnShiftSpike(InputAction.CallbackContext context)
    {
        if (context.started) OnShapeShiftEvent?.Invoke(3);
    }

    // Callback for the 'SwitchCam' action in your Input Action Asset
    public void OnSwitchCam(InputAction.CallbackContext context)
    {
        if (context.started) OnToggleCameraEvent?.Invoke();
    }
}