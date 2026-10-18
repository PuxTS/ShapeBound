using UnityEngine;

public class CameraTargetFollow : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private Rigidbody2D playerRb;
    [SerializeField] private InputReader inputReader;
    [Tooltip("Optional. If empty, it is found on the player object.")]
    [SerializeField] private PlayerLauncher playerLauncher;

    [Header("Mouse Offset Settings")]
    [SerializeField] private float maxMouseOffset = 2.5f;     // Max distance camera shifts toward cursor
    [SerializeField] private float mouseSmoothSpeed = 5f;
    [Tooltip("While aiming, how fast the camera target eases back onto the player.")]
    [SerializeField] private float aimReturnSpeed = 8f;

    [Header("Velocity Lookahead Settings")]
    [SerializeField] private float velocityLeadMultiplier = 0.25f; // Extra lead during high-speed launches
    [SerializeField] private float maxVelocityOffset = 4.0f;

    private Camera mainCam;
    private Vector3 currentOffset;

    private void Start()
    {
        mainCam = Camera.main;

        if (playerLauncher == null && playerTransform != null)
        {
            playerLauncher = playerTransform.GetComponent<PlayerLauncher>();
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // While aiming, ignore the mouse and ease back onto the player, then stay there.
        // The aim is read in world space from the mouse, so a camera that chases the cursor
        // would shift the aim (and it would stop off to one side).
        if (playerLauncher != null && playerLauncher.IsCharging)
        {
            transform.position = Vector3.Lerp(transform.position, playerTransform.position, Time.deltaTime * aimReturnSpeed);
            return;
        }

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