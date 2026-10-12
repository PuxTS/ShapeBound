using UnityEngine;

/// <summary>
/// Draws the aim line (LineRenderer) while the player is charging a launch.
/// Pure visuals: all aim math comes from PlayerLauncher. Driven by PlayerController.Update (via Tick).
/// </summary>
public class PlayerAimLine : MonoBehaviour
{
    [Header("Aim Line Visual Settings")]
    [SerializeField] private LineRenderer aimLineRenderer;
    [SerializeField] private Gradient aimGradient;
    [Tooltip("If true, always renders the aim line for debugging even outside of charging/aiming state.")]
    [SerializeField] private bool showAimLineForDebug = false;

    private PlayerLauncher launcher;

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

    public void Tick()
    {
        if (aimLineRenderer == null) return;

        // Show line if player is charging OR if the debug toggle is active
        if (launcher.IsCharging || showAimLineForDebug)
        {
            if (!launcher.TryGetAim(out PlayerLauncher.AimInfo aim)) return;

            Vector2 playerPos = transform.position;
            Vector2 clampedTarget = playerPos + aim.Direction * aim.ClampedDistance;
            Color currentColor = EvaluateColor(aim.ChargePercent);

            aimLineRenderer.enabled = true;
            aimLineRenderer.SetPosition(0, playerPos);
            aimLineRenderer.SetPosition(1, clampedTarget);
            aimLineRenderer.startColor = currentColor;
            aimLineRenderer.endColor = currentColor;
        }
        else
        {
            aimLineRenderer.enabled = false;
        }
    }
}