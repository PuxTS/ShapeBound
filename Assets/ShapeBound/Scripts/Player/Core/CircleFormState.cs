using UnityEngine;

public class CircleFormState : PlayerBaseState
{
    private bool hasImpacted = false;

    public CircleFormState(PlayerController controller, Rigidbody2D rb) : base(controller, rb) { }

    public override void EnterState()
    {
        hasImpacted = false;
    }

    public override void UpdateState()
    {
        // 1. AIM/CHARGE PHASE
        if (controller.isGrounded)
        {
            hasImpacted = false;

            if (controller.isCharging && controller.InputReader.SpinInput != 0)
            {
                controller.queuedSpin += controller.InputReader.SpinInput * controller.SpinChangeSpeed * Time.deltaTime;
                controller.queuedSpin = Mathf.Clamp(controller.queuedSpin, -controller.MaxSpin, controller.MaxSpin);
            }
            else if (!controller.isCharging)
            {
                controller.queuedSpin = 0f;
            }

            // Only zero out active spin once completely still on floor
            if (Mathf.Abs(rb.linearVelocity.y) <= 0.05f)
            {
                controller.currentSpin = 0f;
            }
        }
        // 2. FLIGHT PHASE
        else
        {
            if (hasImpacted)
            {
                controller.currentSpin = Mathf.MoveTowards(controller.currentSpin, 0f, controller.SpinDecayRate * Time.deltaTime);
            }
        }

        controller.HandleVisualRotation();
    }

    public override void FixedUpdateState()
    {
        // Only apply heavy ground damping if sitting flat on floor, NOT during bounces
        bool isSittingFlat = controller.isGrounded && Mathf.Abs(rb.linearVelocity.y) <= 0.05f;
        rb.linearDamping = isSittingFlat ? 2.0f : 0.05f;
    }

    public override void OnCollisionEnterState(Collision2D collision)
    {
        if (!controller.isGrounded)
        {
            // Only apply spin impulse if active spin is actually charged
            if (Mathf.Abs(controller.currentSpin) > 0.05f)
            {
                ContactPoint2D contact = collision.contacts[0];
                Vector2 tangent = new Vector2(-contact.normal.y, contact.normal.x);
                rb.AddForce(tangent * controller.currentSpin * 1.8f, ForceMode2D.Impulse);
            }

            if (rb.linearVelocity.y > controller.MaxUpwardBounceVelocity)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, controller.MaxUpwardBounceVelocity);
            }

            hasImpacted = true;
        }
    }
}