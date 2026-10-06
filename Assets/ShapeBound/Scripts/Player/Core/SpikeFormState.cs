using UnityEngine;

public class SpikeFormState : PlayerBaseState
{
    // Spike form scales at 80% force for controlled mid-height positioning
    public override float LaunchForceMultiplier => 0.8f;

    public SpikeFormState(PlayerController controller, Rigidbody2D rb) : base(controller, rb) { }

    public override void EnterState()
    {
        controller.currentSpin = 0f;
    }

    public override void FixedUpdateState()
    {
        rb.linearDamping = controller.isGrounded ? 3.0f : 0.05f;
    }
}