using System;
using UnityEngine;

/// <summary>
/// Decides whether the player counts as "grounded", using real physics contacts so surface
/// normals are accurate on slopes. Driven by PlayerController.FixedUpdate (via FixedTick).
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerGroundCheck : MonoBehaviour
{
    [Header("Ground Settings")]
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Minimum surface normal Y to count as ground. 0.5 = slopes up to 60 degrees, 0.7 = about 45 degrees.")]
    [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.5f;
    [Tooltip("Box/Spike: stay grounded this long after losing contact, so rocking or slope/floor junctions can't flip the state every frame.")]
    [SerializeField] private float groundGraceTime = 0.1f;

    [Header("State & Debugging")]
    [field: SerializeField] public bool IsGrounded { get; private set; } = true;

    public bool HasGroundContact { get; private set; }
    public Vector2 GroundNormal { get; private set; } = Vector2.up;
    public Vector2 GroundPoint { get; private set; }
    public LayerMask GroundLayer => groundLayer;

    /// <summary>Fires on the frame the player becomes grounded.</summary>
    public event Action Landed;

    // Speed toward/away from the surface above which we consider the player bouncing, not resting
    private const float GroundNormalSpeedThreshold = 0.1f;
    private const float SettledNormalSpeed = 0.05f;
    private const float FlatGroundNormalY = 0.98f; // about 11 degrees or flatter counts as flat

    private Rigidbody2D rb;
    private ContactFilter2D groundFilter;
    private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[16];

    private bool wasGroundedLastFrame;
    private float groundLockUntil;
    private float lastGroundContactTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        groundFilter = new ContactFilter2D();
        groundFilter.useTriggers = false;
        groundFilter.SetLayerMask(groundLayer);
        groundFilter.useLayerMask = true;
    }

    /// <summary>True when grounded and not moving toward/away from the surface. Works on slopes.</summary>
    public bool IsSettledOnGround()
    {
        return IsGrounded && Mathf.Abs(Vector2.Dot(rb.linearVelocity, GroundNormal)) <= SettledNormalSpeed;
    }

    /// <summary>Like IsSettledOnGround, but only on (near) flat ground. Used by forms that slide freely on slopes.</summary>
    public bool IsSettledOnFlatGround()
    {
        return IsSettledOnGround() && GroundNormal.y >= FlatGroundNormalY;
    }

    /// <summary>
    /// Called when the player launches: forces "not grounded" and ignores ground detection for a moment,
    /// so a flat launch can't re-ground itself on the next frame.
    /// </summary>
    public void NotifyLaunched(float lockDuration)
    {
        IsGrounded = false;
        groundLockUntil = Time.time + lockDuration;
    }

    // Box/Spike: once grounded, stay grounded through brief contact loss (rocking, slope/floor junctions).
    public void FixedTick(bool holdGroundedThroughJitter)
    {
        int count = rb.GetContacts(groundFilter, contactBuffer);
        float centerY = rb.worldCenterOfMass.y;

        HasGroundContact = false;
        Vector2 bestNormal = Vector2.up;
        Vector2 bestPoint = Vector2.zero;
        float bestNormalY = minGroundNormalY;

        for (int i = 0; i < count; i++)
        {
            Vector2 n = contactBuffer[i].normal;

            // Normals can face either way depending on perspective; make them point up
            if (n.y < 0f) n = -n;

            // Must be touching below the body's center (ignores ceilings)
            if (contactBuffer[i].point.y > centerY) continue;

            // Must be walkable (not too steep)
            if (n.y < bestNormalY) continue;

            bestNormalY = n.y;
            bestNormal = n;
            bestPoint = contactBuffer[i].point;
            HasGroundContact = true;
        }

        if (HasGroundContact)
        {
            GroundPoint = bestPoint;
            lastGroundContactTime = Time.time;
        }

        bool launchLocked = Time.time < groundLockUntil;

        // Along a slope this is ~0. A bounce (toward or away from the surface) is large.
        float normalSpeed = HasGroundContact ? Mathf.Abs(Vector2.Dot(rb.linearVelocity, bestNormal)) : 0f;
        bool bouncing = normalSpeed > GroundNormalSpeedThreshold;

        bool landed = HasGroundContact && !bouncing;

        bool holdGrounded = holdGroundedThroughJitter
                            && wasGroundedLastFrame
                            && Time.time - lastGroundContactTime <= groundGraceTime;

        bool currentlyGrounded = !launchLocked && (landed || holdGrounded);

        // Smooth the normal so slope/floor junctions don't make the walk direction flicker
        if (HasGroundContact)
        {
            GroundNormal = wasGroundedLastFrame
                ? Vector2.Lerp(GroundNormal, bestNormal, 0.3f).normalized
                : bestNormal;
        }
        else if (!currentlyGrounded)
        {
            GroundNormal = Vector2.up;
        }

        if (currentlyGrounded)
        {
            if (!wasGroundedLastFrame)
            {
                IsGrounded = true;
                Landed?.Invoke();
            }
        }
        else
        {
            IsGrounded = false;
        }

        wasGroundedLastFrame = IsGrounded;
    }

    // ---------------- Gizmos ----------------

    // Shows exactly what FixedTick decided (play mode only)
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || rb == null) return;

        Vector2 center = rb.worldCenterOfMass;

        // Green = grounded, yellow = touching ground but not counted (bouncing / launch lock), red = airborne
        if (IsGrounded) Gizmos.color = Color.green;
        else if (HasGroundContact) Gizmos.color = Color.yellow;
        else Gizmos.color = Color.red;

        Gizmos.DrawWireSphere(center, 0.1f);

        if (HasGroundContact)
        {
            Gizmos.DrawWireSphere(GroundPoint, 0.06f);
            Gizmos.DrawRay(GroundPoint, GroundNormal * 0.6f); // surface normal
        }

        if (IsGrounded)
        {
            // Walk direction (right) along the surface
            Vector2 tangent = new Vector2(GroundNormal.y, -GroundNormal.x);
            Gizmos.color = Color.magenta;
            Gizmos.DrawRay(center, tangent * 0.8f);
        }
    }
}