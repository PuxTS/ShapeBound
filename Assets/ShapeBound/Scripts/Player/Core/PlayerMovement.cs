using UnityEngine;

/// <summary>
/// How the body moves under physics: walking along the ground surface, per-form damping,
/// and Box/Spike body orientation (tumble in the air, lock and stand upright at rest).
/// Driven by PlayerController.FixedUpdate (via FixedTick).
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(InputReader))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Grounded Movement Settings")]
    [SerializeField] private float groundMoveSpeed = 6f;

    [Header("Body Rotation Settings")]
    [Tooltip("Box/Spike: how fast (degrees/sec) the body rights itself to upright once at rest.")]
    [SerializeField] private float uprightSpeed = 360f;

    private Rigidbody2D rb;
    private InputReader input;
    private PlayerGroundCheck ground;
    private PlayerFormHandler form;
    private PlayerLauncher launcher;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<InputReader>();
        ground = GetComponent<PlayerGroundCheck>();
        form = GetComponent<PlayerFormHandler>();
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
        // Circle never rotates physically (only its visual spins)
        if (!form.CurrentFormData.freePhysicsRotation)
        {
            rb.freezeRotation = true;
            rb.angularVelocity = 0f;
            rb.rotation = 0f;
        }
    }

    /// <summary>Call from FixedUpdate, after the ground check and launcher have ticked.</summary>
    public void FixedTick()
    {
        HandleGroundedMovement();
        HandleDamping();
        HandleBodyRotation();
    }

    // Walks ALONG the ground surface so slopes don't fight the physics solver
    private void HandleGroundedMovement()
    {
        if (!ground.IsGrounded) return;

        if (launcher.IsCharging)
        {
            // Stay put while aiming, even on a slope
            rb.linearVelocity = Vector2.zero;
            return;
        }

        FormData data = form.CurrentFormData;

        // No walking after a launch until stable (still falls into the idle branch below)
        float moveInput = launcher.IsRecovering ? 0f : input.MoveInput;

        if (Mathf.Abs(moveInput) > 0.01f)
        {
            // Tangent points "right" along the surface (flat ground = (1, 0))
            Vector2 normal = ground.GroundNormal;
            Vector2 tangent = new Vector2(normal.y, -normal.x);
            rb.linearVelocity = tangent * (moveInput * groundMoveSpeed);
        }
        else if (!data.keepSlideWhenIdle
                 && (data.dampOnlyWhenSettled ? ground.IsSettledOnFlatGround() : ground.IsSettledOnGround()))
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void HandleDamping()
    {
        FormData data = form.CurrentFormData;

        // dampOnlyWhenSettled (Circle): heavy ground damping only when resting on flat ground,
        // so it slides off slopes with normal physics
        bool applyGroundDamping = data.dampOnlyWhenSettled ? ground.IsSettledOnFlatGround() : ground.IsGrounded;
        rb.linearDamping = applyGroundDamping ? data.groundDamping : data.airDamping;
    }

    // Box/Spike: full physics rotation in the air AND after a launch until they come to rest.
    // Only once stable do they lock and right themselves (upright).
    private void HandleBodyRotation()
    {
        if (!form.CurrentFormData.freePhysicsRotation) return;

        if (!ground.IsGrounded || launcher.IsRecovering)
        {
            rb.freezeRotation = false;
            return;
        }

        rb.freezeRotation = true;
        rb.angularVelocity = 0f;

        if (Mathf.Abs(Mathf.DeltaAngle(rb.rotation, 0f)) > 0.05f)
        {
            rb.rotation = Mathf.MoveTowardsAngle(rb.rotation, 0f, uprightSpeed * Time.fixedDeltaTime);
        }
    }

    // ---------------- Gizmos ----------------

    private void OnDrawGizmos()
    {
        if (Application.isPlaying && rb != null)
        {
            // Current velocity
            Gizmos.color = Color.white;
            Gizmos.DrawRay(transform.position, rb.linearVelocity * 0.25f);
        }
    }
}