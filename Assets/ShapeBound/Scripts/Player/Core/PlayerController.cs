using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(InputReader), typeof(Shapeshifter))]
public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform visualsChild;

    [Header("Form Physics Materials")]
    [SerializeField] private PhysicsMaterial2D circleMaterial;
    [SerializeField] private PhysicsMaterial2D boxMaterial;
    [SerializeField] private PhysicsMaterial2D spikeMaterial;

    [Header("Grounded Movement Settings")]
    [SerializeField] private float groundMoveSpeed = 6f;

    [Header("Launch Settings")]
    [SerializeField] private float minLaunchForce = 4f;
    [SerializeField] private float maxLaunchForce = 18f;
    [SerializeField] private float minAimDistance = 0.5f;
    [SerializeField] private float maxAimDistance = 4.0f;
    [SerializeField] private float stoppedVelocityThreshold = 0.2f;

    [Header("Spin Settings")]
    [SerializeField] private float maxSpin = 5f;
    [SerializeField] private float spinChangeSpeed = 6f;
    [SerializeField] private float spinDecayRate = 2.5f;
    [SerializeField] private float maxUpwardBounceVelocity = 10f;

    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);
    [SerializeField] private float groundCheckRadius = 0.2f;

    [Header("State & Debugging")]
    [field: SerializeField] public bool isGrounded { get; private set; } = true;
    [field: SerializeField] public bool isCharging { get; private set; } = false;
    public float queuedSpin = 0f;
    public float currentSpin = 0f;

    public InputReader InputReader { get; private set; }
    public Transform VisualsChild => visualsChild;
    public float MaxSpin => maxSpin;
    public float SpinChangeSpeed => spinChangeSpeed;
    public float SpinDecayRate => spinDecayRate;
    public float MaxUpwardBounceVelocity => maxUpwardBounceVelocity;

    private Rigidbody2D rb;
    private Shapeshifter shapeSystem;
    private Camera mainCam;
    private bool wasGroundedLastFrame;

    private PlayerBaseState currentState;
    private Dictionary<ShapeForm, PlayerBaseState> states;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        InputReader = GetComponent<InputReader>();
        shapeSystem = GetComponent<Shapeshifter>();
        mainCam = Camera.main;

        states = new Dictionary<ShapeForm, PlayerBaseState>()
        {
            { ShapeForm.Circle, new CircleFormState(this, rb) },
            { ShapeForm.Box, new BoxFormState(this, rb) },
            { ShapeForm.Spike, new SpikeFormState(this, rb) }
        };
    }

    private void Start()
    {
        SwitchState(shapeSystem.CurrentForm);
    }

    private void OnEnable()
    {
        InputReader.OnLaunchPressedEvent += StartCharging;
        InputReader.OnLaunchReleasedEvent += ExecuteLaunch;
        InputReader.OnResetSpinEvent += ResetSpin;
        InputReader.OnShapeShiftEvent += OnShapeShiftReceived;
    }

    private void OnDisable()
    {
        InputReader.OnLaunchPressedEvent -= StartCharging;
        InputReader.OnLaunchReleasedEvent -= ExecuteLaunch;
        InputReader.OnResetSpinEvent -= ResetSpin;
        InputReader.OnShapeShiftEvent -= OnShapeShiftReceived;
    }

    private void StartCharging()
    {
        if (isGrounded && rb.linearVelocity.magnitude <= stoppedVelocityThreshold)
        {
            isCharging = true;
            queuedSpin = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void OnShapeShiftReceived(int shapeIndex)
    {
        shapeSystem.SetShape(shapeIndex);
        SwitchState(shapeSystem.CurrentForm);
    }

    public void SwitchState(ShapeForm newForm)
    {
        currentState?.ExitState();
        currentState = states[newForm];
        currentState.EnterState();

        Collider2D activeCollider = shapeSystem.GetActiveCollider();
        if (activeCollider != null)
        {
            switch (newForm)
            {
                case ShapeForm.Circle:
                    activeCollider.sharedMaterial = circleMaterial;
                    rb.freezeRotation = true;
                    transform.rotation = Quaternion.identity;
                    break;

                case ShapeForm.Box:
                    activeCollider.sharedMaterial = boxMaterial;
                    break;

                case ShapeForm.Spike:
                    activeCollider.sharedMaterial = spikeMaterial;
                    break;
            }
        }
    }

    private void Update()
    {
        currentState?.UpdateState();
        HandleSpinDecay();
        HandleVisualRotation();
    }

    private void FixedUpdate()
    {
        CheckGroundedState();
        HandleGroundedMovement();
        currentState?.FixedUpdateState();
    }

    // Handles horizontal movement when the player is grounded and not charging
    private void HandleGroundedMovement()
    {
        if (isGrounded && !isCharging)
        {
            float moveInput = InputReader.MoveInput;

            if (Mathf.Abs(moveInput) > 0.01f)
            {
                rb.linearVelocity = new Vector2(moveInput * groundMoveSpeed, rb.linearVelocity.y);
            }
            else if (shapeSystem.CurrentForm != ShapeForm.Box)
            {
                // Only zero out horizontal speed if vertical movement has completely settled on floor
                if (Mathf.Abs(rb.linearVelocity.y) <= 0.05f)
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }
            }
        }
    }

    private void HandleSpinDecay()
    {
        if (Mathf.Abs(currentSpin) > 0.01f && isGrounded && !isCharging)
        {
            currentSpin = Mathf.MoveTowards(currentSpin, 0f, spinDecayRate * Time.deltaTime);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        currentState?.OnCollisionEnterState(collision);
    }

    // Checks if the player is grounded by performing a BoxCast downwards from the player's position
    private void CheckGroundedState()
    {
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        
        Vector2 checkSize = shapeSystem.CurrentForm == ShapeForm.Box 
            ? new Vector2(0.55f, 0.08f) 
            : new Vector2(0.4f, 0.08f);

        RaycastHit2D hit = Physics2D.BoxCast(checkPos, checkSize, transform.eulerAngles.z, Vector2.down, groundCheckRadius, groundLayer);
        
        // Don't mark grounded if carried through mid-air bounce
        bool isBouncingInAir = Mathf.Abs(rb.linearVelocity.y) > 0.1f;
        bool currentlyGrounded = hit.collider != null && !isBouncingInAir;

        if (currentlyGrounded)
        {
            if (!wasGroundedLastFrame)
            {
                isGrounded = true;
                isCharging = false;
                queuedSpin = 0f;
                shapeSystem.CanShift = true;
            }
        }
        else
        {
            isGrounded = false;
        }

        wasGroundedLastFrame = isGrounded;
    }

    // Executes the launch action when the player releases the launch input
    private void ExecuteLaunch()
    {
        if (!isGrounded || !isCharging) return;

        isCharging = false;
        isGrounded = false;
        shapeSystem.CanShift = false;

        currentSpin = queuedSpin;

        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(InputReader.AimScreenPosition);
        mouseWorldPos.z = 0f;
        Vector2 aimVector = (Vector2)mouseWorldPos - (Vector2)transform.position;

        Vector2 launchDirection = aimVector.normalized;

        float distance = aimVector.magnitude;
        float clampedDistance = Mathf.Clamp(distance, minAimDistance, maxAimDistance);
        float t = Mathf.InverseLerp(minAimDistance, maxAimDistance, clampedDistance);
        float calculatedForce = Mathf.Lerp(minLaunchForce, maxLaunchForce, t);

        float finalForce = calculatedForce * (currentState != null ? currentState.LaunchForceMultiplier : 1.0f);

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(launchDirection * finalForce, ForceMode2D.Impulse);
    }

    // Resets spin values when the player requests a reset (e.g., via input)
    private void ResetSpin()
    {
        if (isGrounded)
        {
            queuedSpin = 0f;
            currentSpin = 0f;
        }
    }

    // Visual rotation handling based on form, grounded state, and spin
    public void HandleVisualRotation()
    {
        if (visualsChild == null) return;

        if (shapeSystem.CurrentForm == ShapeForm.Box || shapeSystem.CurrentForm == ShapeForm.Spike)
        {
            visualsChild.localRotation = Quaternion.identity;

            if (!isGrounded || rb.linearVelocity.magnitude > stoppedVelocityThreshold)
            {
                rb.freezeRotation = false;
            }
            else
            {
                rb.freezeRotation = true;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, 12f * Time.deltaTime);
            }
            return;
        }

        rb.freezeRotation = true;
        transform.rotation = Quaternion.identity;

        if (!isGrounded)
        {
            float momentumTumble = -rb.linearVelocity.x * 15f;
            float activeSpin = currentSpin * 180f;
            visualsChild.Rotate(0f, 0f, (momentumTumble + activeSpin) * Time.deltaTime);
        }
        else if (isCharging)
        {
            visualsChild.Rotate(0f, 0f, queuedSpin * 180f * Time.deltaTime);
        }
        else
        {
            if (Mathf.Abs(currentSpin) > 0.01f)
            {
                visualsChild.Rotate(0f, 0f, currentSpin * 180f * Time.deltaTime);
            }
            else
            {
                visualsChild.localRotation = Quaternion.identity;
            }
        }
    }


    // Scene Gizmos for debugging aim and ground check
    private void OnDrawGizmos()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Vector3 checkPos = transform.position + (Vector3)groundCheckOffset;
        
        Vector3 checkSize = new Vector3(
            (shapeSystem != null && shapeSystem.CurrentForm == ShapeForm.Box) ? 0.55f : 0.4f, 
            0.08f, 
            0f
        );

        Gizmos.DrawWireCube(checkPos, checkSize);

        Vector3 endPos = checkPos + (Vector3)(Vector2.down * groundCheckRadius);
        Gizmos.DrawWireCube(endPos, checkSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, minAimDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, maxAimDistance);

        Camera cam = mainCam != null ? mainCam : Camera.main;

        if (isCharging && cam != null)
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(InputReader.AimScreenPosition);
            mouseWorldPos.z = 0f;

            Vector2 playerPos = transform.position;
            Vector2 aimVector = (Vector2)mouseWorldPos - playerPos;
            float currentDistance = aimVector.magnitude;

            Vector2 clampedTarget = playerPos + (aimVector.normalized * Mathf.Min(currentDistance, maxAimDistance));

            float chargePercent = Mathf.InverseLerp(minAimDistance, maxAimDistance, currentDistance);
            Gizmos.color = Color.Lerp(Color.yellow, Color.red, chargePercent);

            Gizmos.DrawLine(playerPos, clampedTarget);
            Gizmos.DrawWireSphere(clampedTarget, 0.15f);
        }
    }
}