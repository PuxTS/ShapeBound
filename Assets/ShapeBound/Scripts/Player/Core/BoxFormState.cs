using UnityEngine;

public class BoxFormState : PlayerBaseState
{
    // Heavy weight caps jump force at 65% for grounded, low-trajectory launches
    public override float LaunchForceMultiplier => 0.65f;

    public BoxFormState(PlayerController controller, Rigidbody2D rb) : base(controller, rb) { }

    public override void EnterState()
    {
        controller.currentSpin = 0f;
    }

    public override void FixedUpdateState()
    {
        // High ground damping for flat stopping when needed
        rb.linearDamping = controller.isGrounded ? 4.0f : 0.05f;
    }
}