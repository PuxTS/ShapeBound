using UnityEngine;

/// <summary>
/// Circle's spin: aim a spin while charging, carry it through the flight, decay it,
/// and turn it into a sideways kick when bouncing. Also caps upward bounce speed.
/// Only active for forms with usesSpin enabled in their FormData.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(InputReader))]
public class PlayerSpin : MonoBehaviour
{
    [Header("Spin Settings")]
    [SerializeField] private float maxSpin = 5f;
    [SerializeField] private float spinChangeSpeed = 6f;
    [SerializeField] private float spinDecayRate = 2.5f;

    [Header("Bounce Settings")]
    [SerializeField] private float maxUpwardBounceVelocity = 10f;
    [SerializeField] private float spinImpulseMultiplier = 1.8f;

    [Header("State & Debugging")]
    [field: SerializeField] public float QueuedSpin { get; private set; } = 0f;
    [field: SerializeField] public float CurrentSpin { get; private set; } = 0f;

    private Rigidbody2D rb;
    private InputReader input;
    private PlayerGroundCheck ground;
    private PlayerFormHandler form;
    private PlayerLauncher launcher;

    private bool hasImpacted = false;

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
        hasImpacted = false;

        // Forms without spin never carry spin over
        if (!form.CurrentFormData.usesSpin)
        {
            CurrentSpin = 0f;
        }
    }

    /// <summary>Call from Update.</summary>
    public void Tick()
    {
        if (form.CurrentFormData.usesSpin)
        {
            HandleSpinPhase();
        }

        HandleSpinDecay();
    }

    public void ClearQueuedSpin()
    {
        QueuedSpin = 0f;
    }

    /// <summary>On launch, the spin you aimed becomes the active spin.</summary>
    public void CommitQueuedSpin()
    {
        CurrentSpin = QueuedSpin;
    }

    public void ResetSpin()
    {
        if (ground.IsGrounded)
        {
            QueuedSpin = 0f;
            CurrentSpin = 0f;
        }
    }

    // Aim/charge and flight spin logic
    private void HandleSpinPhase()
    {
        if (ground.IsGrounded)
        {
            hasImpacted = false;

            if (launcher.IsCharging && input.SpinInput != 0)
            {
                QueuedSpin += input.SpinInput * spinChangeSpeed * Time.deltaTime;
                QueuedSpin = Mathf.Clamp(QueuedSpin, -maxSpin, maxSpin);
            }
            else if (!launcher.IsCharging)
            {
                QueuedSpin = 0f;
            }

            // Only zero out active spin once completely still on flat ground
            if (ground.IsSettledOnFlatGround())
            {
                CurrentSpin = 0f;
            }
        }
        else if (hasImpacted)
        {
            CurrentSpin = Mathf.MoveTowards(CurrentSpin, 0f, spinDecayRate * Time.deltaTime);
        }
    }

    private void HandleSpinDecay()
    {
        if (Mathf.Abs(CurrentSpin) > 0.01f && ground.IsGrounded && !launcher.IsCharging)
        {
            CurrentSpin = Mathf.MoveTowards(CurrentSpin, 0f, spinDecayRate * Time.deltaTime);
        }
    }

    // Bounce spin impulse and upward clamp
    private void OnCollisionEnter2D(Collision2D collision)
    {
        FormData data = form.CurrentFormData;
        if (data == null || !data.usesSpin) return;
        if (ground.IsGrounded) return;

        ContactPoint2D contact = collision.GetContact(0);

        // Only apply spin impulse if active spin is actually charged
        if (Mathf.Abs(CurrentSpin) > 0.05f)
        {
            Vector2 tangent = new Vector2(-contact.normal.y, contact.normal.x);
            rb.AddForce(tangent * CurrentSpin * spinImpulseMultiplier, ForceMode2D.Impulse);
        }

        // Only cap bounces off floor-like surfaces, so scraping a wall doesn't kill upward speed
        bool hitFloorLike = Mathf.Abs(contact.normal.y) > 0.5f;
        if (hitFloorLike && rb.linearVelocity.y > maxUpwardBounceVelocity)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxUpwardBounceVelocity);
        }

        hasImpacted = true;
    }
}