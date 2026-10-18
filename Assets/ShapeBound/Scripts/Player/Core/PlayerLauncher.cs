using System;
using UnityEngine;

/// <summary>
/// The launch loop: charge while aiming, launch on release, then stay "recovering"
/// (no walking) until the player has landed and come to rest.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(InputReader))]
public class PlayerLauncher : MonoBehaviour
{
    /// <summary>Everything about the current aim, computed in one place so launch, aim line and gizmos always agree.</summary>
    public readonly struct AimInfo
    {
        public readonly Vector2 Direction;        // normalized, player -> mouse
        public readonly Vector2 RawVector;        // player -> mouse, unclamped
        public readonly float ClampedDistance;    // clamped between min and max aim distance
        public readonly float Force;              // final launch force (includes the form multiplier)
        public readonly float ChargePercent;      // 0..1

        public AimInfo(Vector2 direction, Vector2 rawVector, float clampedDistance, float force, float chargePercent)
        {
            Direction = direction;
            RawVector = rawVector;
            ClampedDistance = clampedDistance;
            Force = force;
            ChargePercent = chargePercent;
        }
    }

    [Header("Launch Settings")]
    [SerializeField] private float minLaunchForce = 4f;
    [SerializeField] private float maxLaunchForce = 18f;
    [SerializeField] private float minAimDistance = 0.5f;
    [SerializeField] private float maxAimDistance = 4.0f;
    [SerializeField] private float stoppedVelocityThreshold = 0.2f;
    [Tooltip("After launching, ground detection is ignored for this long so a flat launch can't re-ground itself.")]
    [SerializeField] private float launchGroundLockTime = 0.15f;
    [Tooltip("After a launch, the player must stay at rest this long before walking is allowed again.")]
    [SerializeField] private float stableTimeRequired = 0.25f;

    [Header("Trajectory Prediction")]
    [Tooltip("Scene-view gizmo only. The in-game arc is controlled on PlayerAimLine.")]
    [SerializeField] private bool showTrajectory = true;
    [Tooltip("How many physics steps ahead to predict. More steps = a longer arc (60 steps is about 1.2 seconds of flight).")]
    [SerializeField] private int trajectorySteps = 60;

    [Header("State & Debugging")]
    [field: SerializeField] public bool IsCharging { get; private set; } = false;
    [field: SerializeField] public bool IsRecovering { get; private set; } = false;

    /// <summary>Fires when the player starts aiming.</summary>
    public event Action ChargeStarted;

    /// <summary>Fires the moment the player launches.</summary>
    public event Action Launched;

    private const float StableAngularSpeed = 15f; // degrees/sec

    private Rigidbody2D rb;
    private InputReader input;
    private PlayerGroundCheck ground;
    private PlayerFormHandler form;
    private PlayerWallStick wallStick; // optional: only present if the player has the wall-stick module
    private Camera mainCam;

    private float stableTimer;
    private Vector3[] gizmoPoints;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<InputReader>();
        ground = GetComponent<PlayerGroundCheck>();
        form = GetComponent<PlayerFormHandler>();
        wallStick = GetComponent<PlayerWallStick>();
        mainCam = Camera.main;
    }

    // You can launch from the ground, or while stuck to a wall/ceiling
    private bool CanLaunchFromHere => ground.IsGrounded || (wallStick != null && wallStick.IsStuck);

    public void StartCharging()
    {
        if (CanLaunchFromHere && rb.linearVelocity.magnitude <= stoppedVelocityThreshold)
        {
            IsCharging = true;
            rb.linearVelocity = Vector2.zero;
            ChargeStarted?.Invoke();
        }
    }

    public void CancelCharging()
    {
        IsCharging = false;
    }

    public void ExecuteLaunch()
    {
        if (!CanLaunchFromHere || !IsCharging) return;
        if (!TryGetAim(out AimInfo aim)) return;

        IsCharging = false;
        ground.NotifyLaunched(launchGroundLockTime);

        IsRecovering = true;
        stableTimer = 0f;

        Launched?.Invoke();

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(aim.Direction * aim.Force, ForceMode2D.Impulse);
    }

    /// <summary>Call from FixedUpdate: clears "recovering" once the player has stayed at rest long enough.</summary>
    public void FixedTick()
    {
        if (!IsRecovering) return;

        bool atRest = ground.IsGrounded
                      && rb.linearVelocity.magnitude <= stoppedVelocityThreshold
                      && Mathf.Abs(rb.angularVelocity) <= StableAngularSpeed;

        // Must STAY at rest, so a momentary slow-down on a slope doesn't unlock walking
        stableTimer = atRest ? stableTimer + Time.fixedDeltaTime : 0f;

        if (stableTimer >= stableTimeRequired)
        {
            IsRecovering = false;
            stableTimer = 0f;
        }
    }

    /// <summary>Computes the current aim from the mouse. False if there is no camera to aim with.</summary>
    public bool TryGetAim(out AimInfo aim)
    {
        Camera cam = mainCam != null ? mainCam : Camera.main;
        if (cam == null || input == null)
        {
            aim = default;
            return false;
        }

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint(input.AimScreenPosition);
        Vector2 rawVector = (Vector2)mouseWorldPos - (Vector2)transform.position;

        float clampedDistance = Mathf.Clamp(rawVector.magnitude, minAimDistance, maxAimDistance);
        float chargePercent = Mathf.InverseLerp(minAimDistance, maxAimDistance, clampedDistance);
        float baseForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, chargePercent);

        FormData data = form != null ? form.CurrentFormData : null;
        float multiplier = data != null ? data.launchForceMultiplier : 1f;

        // Spike: each wall stick used reduces launch power (100% -> 50% -> 25%)
        float stickMultiplier = wallStick != null ? wallStick.LaunchMultiplier : 1f;

        aim = new AimInfo(rawVector.normalized, rawVector, clampedDistance, baseForce * multiplier * stickMultiplier, chargePercent);
        return true;
    }

    /// <summary>How big the points array passed to PredictTrajectory should be.</summary>
    public int MaxTrajectoryPoints => Mathf.Max(2, trajectorySteps + 1);

    /// <summary>
    /// Predicts the flight path of a launch with the given aim, using the same order Box2D uses:
    /// gravity, damping, then move. Fills 'points' and returns how many were written (at least 2).
    /// Stops at the first ground hit. Does not simulate bounces or spin impulses.
    /// </summary>
    public int PredictTrajectory(AimInfo aim, Vector3[] points, out bool hitGround)
    {
        hitGround = false;

        float dt = Time.fixedDeltaTime;
        Vector2 gravity = Physics2D.gravity * rb.gravityScale;
        FormData data = form != null ? form.CurrentFormData : null;
        float airDamping = data != null ? data.airDamping : 0f;

        // Impulse -> velocity: v = impulse / mass (ExecuteLaunch zeroes velocity first)
        Vector2 pos = transform.position;
        Vector2 vel = aim.Direction * aim.Force / rb.mass;

        int maxPoints = Mathf.Min(points.Length, MaxTrajectoryPoints);
        int count = 0;
        points[count++] = pos;

        while (count < maxPoints)
        {
            vel += gravity * dt;
            vel *= 1f / (1f + dt * airDamping);
            Vector2 next = pos + vel * dt;

            RaycastHit2D hit = Physics2D.Linecast(pos, next, ground.GroundLayer);
            if (hit.collider != null)
            {
                points[count++] = hit.point;
                hitGround = true;
                break;
            }

            points[count++] = next;
            pos = next;
        }

        return count;
    }

    // ---------------- Gizmos ----------------

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minAimDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxAimDistance);

        if (Application.isPlaying && IsCharging)
        {
            DrawLaunchGizmo();
        }
    }

    // Uses the exact same math as ExecuteLaunch (TryGetAim), so what you see is what you get
    private void DrawLaunchGizmo()
    {
        if (rb == null || !TryGetAim(out AimInfo aim)) return;

        Vector2 playerPos = transform.position;

        PlayerAimLine aimLine = GetComponent<PlayerAimLine>();
        Color chargeColor = aimLine != null
            ? aimLine.EvaluateColor(aim.ChargePercent)
            : Color.Lerp(Color.yellow, Color.red, aim.ChargePercent);

        // Clamped aim line (min and max distance both respected)
        Vector2 clampedTarget = playerPos + aim.Direction * aim.ClampedDistance;
        Gizmos.color = chargeColor;
        Gizmos.DrawLine(playerPos, clampedTarget);
        Gizmos.DrawWireSphere(clampedTarget, 0.15f);

        // Faint line out to where the mouse really is
        Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
        Gizmos.DrawLine(clampedTarget, playerPos + aim.RawVector);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(clampedTarget + Vector2.up * 0.3f, aim.Force.ToString("F1"));
#endif

        if (showTrajectory)
        {
            if (gizmoPoints == null || gizmoPoints.Length != MaxTrajectoryPoints)
            {
                gizmoPoints = new Vector3[MaxTrajectoryPoints];
            }

            int count = PredictTrajectory(aim, gizmoPoints, out bool hitGround);

            Gizmos.color = chargeColor;
            for (int i = 0; i < count - 1; i++)
            {
                Gizmos.DrawLine(gizmoPoints[i], gizmoPoints[i + 1]);
            }

            if (hitGround)
            {
                Gizmos.DrawWireSphere(gizmoPoints[count - 1], 0.1f);
            }
        }
    }
}