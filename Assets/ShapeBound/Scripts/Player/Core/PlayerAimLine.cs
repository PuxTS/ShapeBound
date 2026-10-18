using UnityEngine;

/// <summary>
/// Draws the aim line (LineRenderer) while the player is charging a launch: either a straight line
/// or the predicted flight arc. Pure visuals: all aim math and prediction come from PlayerLauncher.
/// Driven by PlayerController.Update (via Tick).
/// </summary>
public class PlayerAimLine : MonoBehaviour
{
    [Header("Aim Line Visual Settings")]
    [SerializeField] private LineRenderer aimLineRenderer;
    [SerializeField] private Gradient aimGradient;
    [Tooltip("If true, always renders the aim line for debugging even outside of charging/aiming state.")]
    [SerializeField] private bool showAimLineForDebug = false;

    [Header("Arc Settings")]
    [Tooltip("On: show the predicted flight arc. Off: show the straight aim line.")]
    [SerializeField] private bool showArc = true;
    [Tooltip("How much of the predicted arc to show (1 = all of it). Lower this to make aiming harder.")]
    [SerializeField, Range(0.1f, 1f)] private float arcLength = 1f;

    private PlayerLauncher launcher;
    private Vector3[] arcPoints;

    private void Awake()
    {
        launcher = GetComponent<PlayerLauncher>();

        if (aimLineRenderer == null)
            aimLineRenderer = GetComponent<LineRenderer>();

        if (aimLineRenderer != null)
        {
            aimLineRenderer.positionCount = 2;
            aimLineRenderer.enabled = false;
        }

        // Reused every frame so the arc doesn't create garbage
        arcPoints = new Vector3[launcher.MaxTrajectoryPoints];
    }

    private void OnEnable()
    {
        launcher.Launched += HandleLaunched;
    }

    private void OnDisable()
    {
        launcher.Launched -= HandleLaunched;
    }

    // Hide immediately on launch instead of waiting for the next Tick
    private void HandleLaunched()
    {
        if (aimLineRenderer != null && !showAimLineForDebug)
        {
            aimLineRenderer.enabled = false;
        }
    }

    /// <summary>Gradient color for a charge level (0..1). Also used by the launch gizmos.</summary>
    public Color EvaluateColor(float chargePercent)
    {
        return aimGradient != null ? aimGradient.Evaluate(chargePercent) : Color.white;
    }

    /// <summary>Call from Update.</summary>
    public void Tick()
    {
        if (aimLineRenderer == null) return;

        // Show line if player is charging OR if the debug toggle is active
        if (launcher.IsCharging || showAimLineForDebug)
        {
            if (!launcher.TryGetAim(out PlayerLauncher.AimInfo aim)) return;

            Color currentColor = EvaluateColor(aim.ChargePercent);

            aimLineRenderer.enabled = true;
            aimLineRenderer.startColor = currentColor;
            aimLineRenderer.endColor = currentColor;

            if (showArc)
            {
                DrawArc(aim);
            }
            else
            {
                DrawStraightLine(aim);
            }
        }
        else
        {
            aimLineRenderer.enabled = false;
        }
    }

    private void DrawStraightLine(PlayerLauncher.AimInfo aim)
    {
        Vector2 playerPos = transform.position;
        Vector2 clampedTarget = playerPos + aim.Direction * aim.ClampedDistance;

        aimLineRenderer.positionCount = 2;
        aimLineRenderer.SetPosition(0, playerPos);
        aimLineRenderer.SetPosition(1, clampedTarget);
    }

    private void DrawArc(PlayerLauncher.AimInfo aim)
    {
        // Keep the buffer in sync if the step count changes at runtime
        if (arcPoints.Length != launcher.MaxTrajectoryPoints)
        {
            arcPoints = new Vector3[launcher.MaxTrajectoryPoints];
        }

        int predicted = launcher.PredictTrajectory(aim, arcPoints, out _);

        // Only show the first part of the arc if arcLength < 1
        int count = Mathf.Clamp(Mathf.CeilToInt(predicted * arcLength), 2, predicted);

        aimLineRenderer.positionCount = count;
        for (int i = 0; i < count; i++)
        {
            aimLineRenderer.SetPosition(i, arcPoints[i]);
        }
    }
}