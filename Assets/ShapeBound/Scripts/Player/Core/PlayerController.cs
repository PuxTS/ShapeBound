using System;
using UnityEngine;

/// <summary>
/// The player's coordinator. It owns no gameplay logic itself: it wires input to the player
/// modules, connects their events, and ticks them in a fixed order.
///
///   PlayerGroundCheck   - is the player grounded, on what surface
///   PlayerFormHandler   - which form is active (unlocked forms, FormData, material)
///   PlayerLauncher      - charge, aim and launch, plus "recovering" after a launch
///   PlayerSpin          - Circle spin and bounce behavior
///   PlayerMovement      - walking, damping, Box/Spike body orientation
///   PlayerVisuals       - sprite rotation (rolling, tumbling, upright)
///   PlayerAimLine       - the aim line renderer
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(InputReader), typeof(Shapeshifter))]
[RequireComponent(typeof(PlayerGroundCheck), typeof(PlayerFormHandler), typeof(PlayerLauncher))]
[RequireComponent(typeof(PlayerSpin), typeof(PlayerMovement), typeof(PlayerVisuals))]
[RequireComponent(typeof(PlayerAimLine))]
public class PlayerController : MonoBehaviour
{
    public InputReader InputReader { get; private set; }
    public FormData CurrentFormData => formHandler.CurrentFormData;
    public Transform VisualsChild => visuals.VisualsChild;
    public bool IsGrounded => ground.IsGrounded;
    public bool IsCharging => launcher.IsCharging;

    /// <summary>Fires after the player switches form (also once at start). Used by the form UI.</summary>
    public event Action<ShapeForm> OnFormChanged;

    public void SwitchForm(ShapeForm newForm) => formHandler.SwitchForm(newForm);

    // ---- Modules ----
    private Shapeshifter shapeSystem;
    private PlayerGroundCheck ground;
    private PlayerFormHandler formHandler;
    private PlayerLauncher launcher;
    private PlayerSpin spin;
    private PlayerMovement movement;
    private PlayerVisuals visuals;
    private PlayerAimLine aimLine;

    private void Awake()
    {
        InputReader = GetComponent<InputReader>();
        shapeSystem = GetComponent<Shapeshifter>();

        ground = GetComponent<PlayerGroundCheck>();
        formHandler = GetComponent<PlayerFormHandler>();
        launcher = GetComponent<PlayerLauncher>();
        spin = GetComponent<PlayerSpin>();
        movement = GetComponent<PlayerMovement>();
        visuals = GetComponent<PlayerVisuals>();
        aimLine = GetComponent<PlayerAimLine>();
    }

    private void Start()
    {
        formHandler.InitializeStartingForm();
    }

    private void OnEnable()
    {
        // Input -> actions
        InputReader.OnLaunchPressedEvent += launcher.StartCharging;
        InputReader.OnLaunchReleasedEvent += launcher.ExecuteLaunch;
        InputReader.OnResetSpinEvent += spin.ResetSpin;
        InputReader.OnShapeShiftEvent += OnShapeShiftReceived;

        // Module events
        ground.Landed += HandleLanded;
        launcher.ChargeStarted += spin.ClearQueuedSpin;
        launcher.Launched += HandleLaunched;
        formHandler.OnFormChanged += RelayFormChanged;
    }

    private void OnDisable()
    {
        InputReader.OnLaunchPressedEvent -= launcher.StartCharging;
        InputReader.OnLaunchReleasedEvent -= launcher.ExecuteLaunch;
        InputReader.OnResetSpinEvent -= spin.ResetSpin;
        InputReader.OnShapeShiftEvent -= OnShapeShiftReceived;

        ground.Landed -= HandleLanded;
        launcher.ChargeStarted -= spin.ClearQueuedSpin;
        launcher.Launched -= HandleLaunched;
        formHandler.OnFormChanged -= RelayFormChanged;
    }

    private void Update()
    {
        if (formHandler.CurrentFormData == null) return;

        spin.Tick();
        visuals.Tick();
        aimLine.Tick();
    }

    private void FixedUpdate()
    {
        FormData data = formHandler.CurrentFormData;
        if (data == null) return;

        ground.FixedTick(data.freePhysicsRotation);
        launcher.FixedTick();
        movement.FixedTick();
    }

    private void OnShapeShiftReceived(int shapeIndex)
    {
        formHandler.SwitchForm((ShapeForm)shapeIndex);
    }

    private void RelayFormChanged(ShapeForm form)
    {
        OnFormChanged?.Invoke(form);
    }

    // Landing ends aiming, clears the queued spin and allows shape-shifting again
    private void HandleLanded()
    {
        launcher.CancelCharging();
        spin.ClearQueuedSpin();
        shapeSystem.CanShift = true;
    }

    // Launching locks shape-shifting until the next landing and commits the aimed spin
    private void HandleLaunched()
    {
        shapeSystem.CanShift = false;
        spin.CommitQueuedSpin();
    }
}