using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(Collider2D))]
public class CameraZone : MonoBehaviour
{
    [Header("Zone Priority")]
    [SerializeField] private int zonePriority = 10;

    [Header("Exit Debounce")]
    [Tooltip("Wait this long before treating 'all player colliders left' as a real exit. " +
             "Shape-shifting disables one collider and enables another, which fires Exit then Enter.")]
    [SerializeField] private float exitDelay = 0.1f;

    public CinemachineCamera ZoneCamera { get; private set; }
    public int ZonePriority => zonePriority;

    private int playerColliderCount = 0;
    private Coroutine pendingExit;

    private void Awake()
    {
        ZoneCamera = GetComponentInParent<CinemachineCamera>();

        if (ZoneCamera == null)
        {
            Debug.LogError($"[CameraZone] No CinemachineCamera found on parent of {gameObject.name}!");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerColliderCount++;
            CameraManager.Instance.SetCurrentZone(this);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player") && playerColliderCount > 0)
        {
            CameraManager.Instance.SetCurrentZone(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerColliderCount = Mathf.Max(0, playerColliderCount - 1);

            // Only consider leaving when ALL player colliders are gone, and confirm after a short delay
            if (playerColliderCount == 0 && pendingExit == null)
            {
                pendingExit = StartCoroutine(ConfirmExit());
            }
        }
    }

    private IEnumerator ConfirmExit()
    {
        yield return new WaitForSecondsRealtime(exitDelay);

        pendingExit = null;

        // If a new collider entered in the meantime (a form switch), this was not a real exit
        if (playerColliderCount == 0)
        {
            Debug.Log($"[CameraZone] Player EXITED zone: {transform.parent.name}");
            CameraManager.Instance.ClearCurrentZone(this);
        }
    }

    private void OnDisable()
    {
        playerColliderCount = 0;
        pendingExit = null;
    }
}