using UnityEngine;

public abstract class PlayerBaseState
{
    protected PlayerController controller;
    protected Rigidbody2D rb;

    // Multiplier applied to launch force (Defaults to 1.0f for Circle)
    public virtual float LaunchForceMultiplier => 1.0f;

    public PlayerBaseState(PlayerController controller, Rigidbody2D rb)
    {
        this.controller = controller;
        this.rb = rb;
    }

    public virtual void EnterState() { }
    public virtual void ExitState() { }
    public virtual void UpdateState() { }
    public virtual void FixedUpdateState() { }
    public virtual void OnCollisionEnterState(Collision2D collision) { }
}