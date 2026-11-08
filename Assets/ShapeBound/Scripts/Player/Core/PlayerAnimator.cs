using System;
using UnityEngine;

public enum PlayerAnimState { Idle, Walk, Turning, Throwing, Falling, Stuck }

/// <summary>
/// Turns what the player is doing into animation by CALLING animator states by name
/// (no parameters and no transitions needed in the Animator Controller).
/// It only READS the other player modules, so nothing else needs to change.
/// Drives the Animator of whichever form visual is currently active.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(InputReader))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Animators (one per form visual)")]
    [Tooltip("Assign the Animator on each form's visual (Circle, Box, Spike). The one that is currently active gets driven.")]
    [SerializeField] private Animator[] animators;

    [Header("Animator State Names (must match your Animator Controller exactly)")]
    [Tooltip("Leave a name empty to skip that state. A state a form's controller doesn't have falls back to the Idle state.")]
    [SerializeField] private string idleState = "Idle";
    [Tooltip("Plays once when you start walking from standing still, then switches to the Walking state. Leave empty to skip.")]
    [SerializeField] private string startWalkState = "Start_Walking";
    [SerializeField] private string walkState = "Walking";
    [SerializeField] private string turnState = "Turning";
    [SerializeField] private string throwState = "Throw";
    [SerializeField] private string fallState = "Falling";
    [Tooltip("Spike stuck on a wall. Empty = uses Idle until you have a clip for it.")]
    [SerializeField] private string stuckState = "";

    [Header("Timing")]
    [Tooltip("Speed along the ground above this counts as walking.")]
    [SerializeField] private float walkSpeedThreshold = 0.1f;
    [Tooltip("How long the Turning state lasts. Set it to the length of your turn clip.")]
    [SerializeField] private float turnDuration = 0.2f;
    [Tooltip("How long the Start_Walking state plays before Walking. Set it to the length of that clip.")]
    [SerializeField] private float startWalkDuration = 0.15f;
    [Tooltip("Blend time between animations. 0 = cut instantly.")]
    [SerializeField] private float crossFadeTime = 0.1f;

    [Header("Mirroring (optional)")]
    [Tooltip("Mirrors this object left/right to match the facing direction. Use a parent that does NOT rotate. " +
             "Assumes your art faces right at scale +1. Leave empty if you handle facing another way.")]
    [SerializeField] private Transform flipTarget;
    [Tooltip("On: keep the old direction while the Turning clip plays, then flip when it ends. Off: flip the moment you turn.")]
    [SerializeField] private bool flipAtTurnEnd = true;

    [Header("State & Debugging")]
    [field: SerializeField] public PlayerAnimState State { get; private set; } = PlayerAnimState.Idle;
    [field: SerializeField] public int Facing { get; private set; } = 1; // 1 = right, -1 = left

    /// <summary>Fires when the state changes. Handy for sounds or particles.</summary>
    public event Action<PlayerAnimState> StateChanged;

    private Rigidbody2D rb;
    private InputReader input;
    private PlayerGroundCheck ground;
    private PlayerFormHandler form;
    private PlayerLauncher launcher;
    private PlayerWallStick wallStick; // optional

    private Animator active;
    private string playedStateName;
    private float turnTimer;
    private float startWalkTimer;
    private int visualFacing = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<InputReader>();
        ground = GetComponent<PlayerGroundCheck>();
        form = GetComponent<PlayerFormHandler>();
        launcher = GetComponent<PlayerLauncher>();
        wallStick = GetComponent<PlayerWallStick>();
    }

    private void OnEnable()
    {
        form.OnFormChanged += HandleFormChanged;
    }

    private void OnDisable()
    {
        form.OnFormChanged -= HandleFormChanged;
    }

    private void Update()
    {
        if (form.CurrentFormData == null) return;

        if (active == null || !active.gameObject.activeInHierarchy)
        {
            SelectActiveAnimator();
        }

        UpdateFacing();
        turnTimer = Mathf.Max(0f, turnTimer - Time.deltaTime);
        startWalkTimer = Mathf.Max(0f, startWalkTimer - Time.deltaTime);

        // Speed ALONG the ground surface (so slopes and recovering/locked states read correctly)
        Vector2 normal = ground.GroundNormal;
        Vector2 tangent = new Vector2(normal.y, -normal.x);
        float groundSpeed = Mathf.Abs(Vector2.Dot(rb.linearVelocity, tangent));

        SetState(ComputeState(groundSpeed));
        UpdateMirroring();
        PlayCurrentState();
    }

    // Highest priority first
    private PlayerAnimState ComputeState(float groundSpeed)
    {
        if (wallStick != null && wallStick.IsStuck) return PlayerAnimState.Stuck;
        if (launcher.IsCharging) return PlayerAnimState.Throwing;
        if (!ground.IsGrounded) return PlayerAnimState.Falling;
        if (turnTimer > 0f) return PlayerAnimState.Turning;
        if (groundSpeed > walkSpeedThreshold) return PlayerAnimState.Walk;

        return PlayerAnimState.Idle;
    }

    // Walking: face the way you move. Aiming: face the side of the mouse.
    private void UpdateFacing()
    {
        if (wallStick != null && wallStick.IsStuck) return;

        int desired = Facing;

        if (launcher.IsCharging)
        {
            if (launcher.TryGetAim(out PlayerLauncher.AimInfo aim) && Mathf.Abs(aim.RawVector.x) > 0.05f)
            {
                desired = aim.RawVector.x > 0f ? 1 : -1;
            }
        }
        else if (ground.IsGrounded && !launcher.IsRecovering)
        {
            float move = input.MoveInput;
            if (Mathf.Abs(move) > 0.01f)
            {
                desired = move > 0f ? 1 : -1;
            }
        }

        if (desired == Facing) return;

        Facing = desired;

        // The turn animation only plays when you turn on your feet, not while aiming
        if (!launcher.IsCharging)
        {
            turnTimer = turnDuration;
        }
    }

    private void SetState(PlayerAnimState newState)
    {
        if (newState == State) return;

        // Starting to walk from standing still plays the Start_Walking clip first
        if (newState == PlayerAnimState.Walk && State == PlayerAnimState.Idle)
        {
            startWalkTimer = startWalkDuration;
        }

        State = newState;
        StateChanged?.Invoke(newState);
    }

    // Mirrors the sprite to the facing direction (optional)
    private void UpdateMirroring()
    {
        if (flipTarget == null) return;

        // While turning, optionally keep the old direction until the turn clip has finished
        bool holdOldDirection = flipAtTurnEnd && State == PlayerAnimState.Turning;
        if (!holdOldDirection) visualFacing = Facing;

        Vector3 scale = flipTarget.localScale;
        float wantedX = Mathf.Abs(scale.x) * visualFacing;

        if (!Mathf.Approximately(scale.x, wantedX))
        {
            scale.x = wantedX;
            flipTarget.localScale = scale;
        }
    }

    // ---------------- Calling animator states ----------------

    private string GetStateName(PlayerAnimState state)
    {
        switch (state)
        {
            case PlayerAnimState.Walk:
                return startWalkTimer > 0f && !string.IsNullOrEmpty(startWalkState) ? startWalkState : walkState;
            case PlayerAnimState.Turning: return turnState;
            case PlayerAnimState.Throwing: return throwState;
            case PlayerAnimState.Falling: return fallState;
            case PlayerAnimState.Stuck: return stuckState;
            default: return idleState;
        }
    }

    // Plays the animator state for the current player state, but only when it actually changes
    // (this also covers Start_Walking -> Walking, and a freshly switched form visual).
    private void PlayCurrentState()
    {
        if (active == null || active.runtimeAnimatorController == null) return;

        string stateName = GetStateName(State);

        // This form's controller doesn't have that state (e.g. Circle has no Walking): use Idle
        if (string.IsNullOrEmpty(stateName) || !active.HasState(0, Animator.StringToHash(stateName)))
        {
            stateName = idleState;
        }

        if (string.IsNullOrEmpty(stateName) || stateName == playedStateName) return;

        int hash = Animator.StringToHash(stateName);
        if (!active.HasState(0, hash)) return;

        if (crossFadeTime > 0f && playedStateName != null)
        {
            active.CrossFadeInFixedTime(hash, crossFadeTime, 0);
        }
        else
        {
            active.Play(hash, 0, 0f);
        }

        playedStateName = stateName;
    }

    private void SelectActiveAnimator()
    {
        Animator found = null;

        if (animators != null)
        {
            foreach (Animator candidate in animators)
            {
                if (candidate != null && candidate.gameObject.activeInHierarchy)
                {
                    found = candidate;
                    break;
                }
            }
        }

        if (found == active) return;

        active = found;

        // A form's visual is switched off and on when shape-shifting, which resets its Animator
        // to its default state. Forget what we played so the right state is called again.
        playedStateName = null;
    }

    private void HandleFormChanged(ShapeForm newForm)
    {
        SelectActiveAnimator();
    }
}