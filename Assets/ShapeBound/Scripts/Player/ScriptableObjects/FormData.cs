using UnityEngine;

[CreateAssetMenu(menuName = "Player/Form Data", fileName = "NewFormData")]
public class FormData : ScriptableObject
{
    [Header("Identity")]
    public ShapeForm form;

    [Header("Launch")]
    public float launchForceMultiplier = 1f;

    [Header("Damping")]
    public float groundDamping = 2f;
    public float airDamping = 0.05f;
    [Tooltip("If true, ground damping only applies when settled on the surface (not during bounces). Circle uses this.")]
    public bool dampOnlyWhenSettled = false;

    [Header("Physics")]
    public PhysicsMaterial2D material;

    [Header("Behavior Flags")]
    [Tooltip("Circle: charge/aim spin, spin impulse on bounce, bounce clamp.")]
    public bool usesSpin = false;
    [Tooltip("Box/Spike: body tumbles in the air, then locks and aligns flush with the ground once grounded.")]
    public bool freePhysicsRotation = false;
    [Tooltip("Box: don't stop dead when there is no move input.")]
    public bool keepSlideWhenIdle = false;
    [Tooltip("Spike: sticks to walls and ceilings when it hits them in the air (needs PlayerWallStick on the player).")]
    public bool canWallStick = false;
}