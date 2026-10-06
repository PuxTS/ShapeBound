using UnityEngine;

public class CameraTargetFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private InputReader inputReader;

    [Header("Mouse Offset Settings")]
    [SerializeField] private float maxMouseOffset = 2.5f;     // Max distance camera shifts toward cursor
    [SerializeField] private float mouseSmoothSpeed = 5f;

    [Header("Velocity Lookahead Settings")]
    [SerializeField] private float velocityLeadMultiplier = 0.25f; // Extra lead during high-speed launches
    [SerializeField] private float maxVelocityOffset = 4.0f;

    private Camera mainCam;
    private Vector3 currentOffset;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // 1. Calculate cursor offset relative to player
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(inputReader.AimScreenPosition);
        mouseWorldPos.z = 0f;

        Vector2 aimDir = (mouseWorldPos - playerTransform.position);
        Vector2 mouseOffset = Vector2.ClampMagnitude(aimDir * 0.3f, maxMouseOffset);

        // 2. Calculate velocity lead offset
        Vector2 velocityOffset = Vector2.ClampMagnitude(playerRb.linearVelocity * velocityLeadMultiplier, maxVelocityOffset);

        // 3. Combine target position with smooth interpolation
        Vector3 targetPosition = playerTransform.position + (Vector3)(mouseOffset + velocityOffset);
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * mouseSmoothSpeed);
    }
}