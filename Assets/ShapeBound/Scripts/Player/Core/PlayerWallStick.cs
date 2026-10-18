using System;
using UnityEngine;

/// <summary>
/// Spike's wall-stick. While airborne, hitting a wall or ceiling embeds the spike into it: the body is
/// pinned in place (no falling) and you can aim and launch from it again. Each stick costs launch power
/// (see launchPowerByStickCount), and the number of sticks is limited. Landing on solid ground resets everything.
/// Only active for forms with canWallStick enabled in their FormData.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerWallStick : MonoBehaviour
{
    [Header("Stick Settings")]
    [Tooltip("Launch power by number of sticks used. Index 0 = launching from the ground (100%), 1 = after the first stick, 2 = after the second. " +
             "The length of this list also sets how many sticks are allowed (3 entries = 2 sticks). It multiplies the form's own launch multiplier.")]
    [SerializeField] private float[] launchPowerByStickCount = { 1f, 0.5f, 0.25f };

    [Tooltip("On: the percentages above mean how FAR you launch (50% = about half the distance). " +
             "Off: they scale launch force directly, which shrinks distance much more (50% force = about 25% distance).")]
    [SerializeField] private bool percentagesAreDistance = true;

    [Tooltip("After launching off a wall, ignore new sticks for this long so you can't instantly re-stick to the wall you just left.")]
    [SerializeField] private float stickCooldown = 0.15f;

    [Tooltip("How far the spike sinks into the surface when it sticks.")]
    [SerializeField] private float embedDepth = 0.05f;

    [Tooltip("Rotate the body so the spike tip points into the surface. Assumes the sprite's tip points up.")]
    [SerializeField] private bool alignTipToSurface = true;

    [Header("State & Debugging")]
    [field: SerializeField] public bool IsStuck { get; private set; } = false;
    [field: SerializeField] public int StickCount { get; private set; } = 0;

    public int MaxSticks => Mathf.Max(0, launchPowerByStickCount.Length - 1);

    /// <summary>Launch power multiplier for the current stick count. 1 for forms that can't wall-stick.</summary>
    public float LaunchMultiplier
    {
        get
        {
            FormData data = form != null ? form.CurrentFormData : null;
            if (data == null || !data.canWallStick || launchPowerByStickCount.Length == 0) return 1f;

            float value = launchPowerByStickCount[Mathf.Clamp(StickCount, 0, launchPowerByStickCount.Length - 1)];

            // Distance grows with the square of launch speed, so take the square root to make
            // "50%" actually feel like half the distance instead of a quarter
            return percentagesAreDistance ? Mathf.Sqrt(Mathf.Max(0f, value)) : value;
        }
    }

    /// <summary>Fires when the spike sticks. Parameter: how many sticks have been used so far.</summary>
    public event Action<int> Stuck;

    private Rigidbody2D rb;
    private PlayerGroundCheck ground;
    private PlayerLauncher launcher;
    private PlayerFormHandler form;

    private Collider2D stuckCollider;
    private float stickBlockedUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ground = GetComponent<PlayerGroundCheck>();
        launcher = GetComponent<PlayerLauncher>();
        form = GetComponent<PlayerFormHandler>();
    }

    private void OnEnable()
    {
        ground.Landed += ResetSticks;
        launcher.Launched += HandleLaunched;
        form.OnFormChanged += HandleFormChanged;
    }

    private void OnDisable()
    {
        ground.Landed -= ResetSticks;
        launcher.Launched -= HandleLaunched;
        form.OnFormChanged -= HandleFormChanged;
    }

    // No ordering dependency on other modules, so this ticks itself
    private void FixedUpdate()
    {
        // If what we stuck to disappears (destroyed or disabled), fall instead of hanging in mid-air
        if (IsStuck && (stuckCollider == null || !stuckCollider.enabled || !stuckCollider.gameObject.activeInHierarchy))
        {
            Release();
        }
    }

    // Landing on solid ground: stick count back to zero, full movement again
    private void ResetSticks()
    {
        StickCount = 0;
        Release();
    }

    // Called by the launcher right before it applies the launch force
    private void HandleLaunched()
    {
        bool wasStuck = IsStuck;
        Release();

        if (wasStuck)
        {
            stickBlockedUntil = Time.time + stickCooldown;
        }
    }

    private void HandleFormChanged(ShapeForm newForm)
    {
        // Changing out of the spike lets go of the wall
        if (!form.CurrentFormData.canWallStick)
        {
            Release();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        FormData data = form.CurrentFormData;
        if (data == null || !data.canWallStick) return;

        if (IsStuck || StickCount >= MaxSticks) return;
        if (ground.IsGrounded) return;                       // walking into a wall on the ground doesn't stick
        if (Time.time < stickBlockedUntil) return;

        // Only stick to solid level geometry
        int otherLayer = collision.collider.gameObject.layer;
        if ((ground.GroundLayer.value & (1 << otherLayer)) == 0) return;

        float centerY = rb.worldCenterOfMass.y;
        bool foundSurface = false;
        Vector2 outwardNormal = Vector2.zero;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            Vector2 n = contact.normal;

            // Touching a floor (below us, flat enough to stand on) is a landing, not a stick.
            // The ground check handles landings.
            bool floorLike = Mathf.Abs(n.y) >= ground.MinGroundNormalY && contact.point.y <= centerY;
            if (floorLike) return;

            if (!foundSurface)
            {
                // Make the normal point from the surface toward us, whichever way Unity reports it
                if (Vector2.Dot(n, contact.point - rb.worldCenterOfMass) > 0f) n = -n;

                outwardNormal = n;
                foundSurface = true;
            }
        }

        if (!foundSurface) return;

        Stick(collision.collider, outwardNormal);
    }

    private void Stick(Collider2D surface, Vector2 outwardNormal)
    {
        IsStuck = true;
        StickCount++;
        stuckCollider = surface;

        // Kinematic = no gravity, no forces, doesn't get pushed: pinned in place
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;

        if (alignTipToSurface)
        {
            rb.rotation = Vector2.SignedAngle(Vector2.up, -outwardNormal);
        }

        // Sink the tip slightly into the surface
        rb.position += -outwardNormal * embedDepth;

        Stuck?.Invoke(StickCount);
    }

    private void Release()
    {
        if (!IsStuck) return;

        IsStuck = false;
        stuckCollider = null;
        rb.bodyType = RigidbodyType2D.Dynamic;
    }
}