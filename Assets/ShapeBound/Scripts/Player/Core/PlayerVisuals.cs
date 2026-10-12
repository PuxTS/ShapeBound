using UnityEngine;

/// <summary>
/// Visual-only rotation of the sprite child: Circle rolls with the ground, tumbles in the air and
/// shows its spin; Box/Spike keep the sprite upright. Never touches physics.
/// Driven by PlayerController.Update (via Tick).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerVisuals : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualsChild;

    [Header("Settings")]
    [Tooltip("Box/Spike: how fast (degrees/sec) the sprite eases back to upright.")]
    [SerializeField] private float visualUprightSpeed = 360f;

    public Transform VisualsChild => visualsChild;

    private Rigidbody2D rb;
    private Shapeshifter shapeSystem;
    private PlayerGroundCheck ground;
    private PlayerFormHandler form;
    private PlayerSpin spin;
    private PlayerLauncher launcher;

    // Circle rolling (world-space radius, read from the CircleCollider2D)
    private float rollRadius = 0.5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        shapeSystem = GetComponent<Shapeshifter>();
        ground = GetComponent<PlayerGroundCheck>();
        form = GetComponent<PlayerFormHandler>();
        spin = GetComponent<PlayerSpin>();
        launcher = GetComponent<PlayerLauncher>();
    }

    private void OnEnable()
    {
        form.OnFormChanged += HandleFormChanged;
    }

    private void OnDisable()
    {
        form.OnFormChanged -= HandleFormChanged;
    }

    private void HandleFormChanged(ShapeForm newForm)
    {
        if (shapeSystem.GetActiveCollider() is CircleCollider2D circle)
        {
            float scale = Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
            rollRadius = Mathf.Max(0.01f, circle.radius * scale);
        }
    }

    /// <summary>Call from Update.</summary>
    public void Tick()
    {
        if (visualsChild == null) return;

        FormData data = form.CurrentFormData;
        if (data == null) return;

        // Forms with free physics rotation: the body does the rotating,
        // so the sprite just eases back to upright locally.
        if (data.freePhysicsRotation)
        {
            visualsChild.localRotation = Quaternion.RotateTowards(
                visualsChild.localRotation,
                Quaternion.identity,
                visualUprightSpeed * Time.deltaTime
            );
            return;
        }

        if (launcher.IsCharging && ground.IsGrounded)
        {
            visualsChild.Rotate(0f, 0f, spin.QueuedSpin * 180f * Time.deltaTime);
        }
        else if (ground.HasGroundContact)
        {
            // Touching a surface: roll to match the speed ALONG it
            Vector2 normal = ground.GroundNormal;
            Vector2 tangent = new Vector2(normal.y, -normal.x);
            float surfaceSpeed = Vector2.Dot(rb.linearVelocity, tangent);
            float rollRate = -surfaceSpeed / rollRadius * Mathf.Rad2Deg;
            float spinRate = spin.CurrentSpin * 180f;

            if (Mathf.Abs(surfaceSpeed) > 0.05f || Mathf.Abs(spin.CurrentSpin) > 0.01f)
            {
                visualsChild.Rotate(0f, 0f, (rollRate + spinRate) * Time.deltaTime);
            }
            // At rest: maintain current stopped angle
        }
        else
        {
            // Airborne
            float momentumTumble = -rb.linearVelocity.x * 15f;
            float activeSpin = spin.CurrentSpin * 180f;
            visualsChild.Rotate(0f, 0f, (momentumTumble + activeSpin) * Time.deltaTime);
        }
    }
}