using UnityEngine;
using UnityEngine.Events;

// Place in the level. When the player touches it, the form is unlocked in PlayerData.
[RequireComponent(typeof(Collider2D))]
public class FormUnlockPickup : MonoBehaviour
{
    [SerializeField] private ShapeForm formToUnlock = ShapeForm.Box;

    [Tooltip("Also switch the player into the new form. Only works if the player can shift right now (e.g. grounded).")]
    [SerializeField] private bool switchToFormOnPickup = true;

    [Header("Float Animation")]
    [SerializeField] private bool floatEnabled = true;
    [Tooltip("Optional: a child (e.g. the sprite) to bob. Leave empty to bob this whole object, collider included.")]
    [SerializeField] private Transform visual;
    [Tooltip("How far it moves up and down from its start position.")]
    [SerializeField] private float floatHeight = 0.2f;
    [Tooltip("Seconds for one full up-and-down cycle.")]
    [SerializeField] private float floatCycleTime = 2f;

    private bool collected;
    private Transform bobTarget;
    private Vector3 startLocalPosition;
    private float phaseOffset;

    // Runs when the component is first added in the editor
    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void Start()
    {
        bobTarget = visual != null ? visual : transform;
        startLocalPosition = bobTarget.localPosition;

        // Random phase so several pickups don't bob in sync
        phaseOffset = Random.Range(0f, floatCycleTime);

        // Don't show a pickup for a form the player already has
        PlayerData_Manager manager = PlayerData_Manager.Instance;
        if (manager != null && manager.PlayerData.IsUnlocked(formToUnlock))
        {
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (!floatEnabled || floatCycleTime <= 0f) return;

        // PingPong goes 0 -> 1 -> 0; SmoothStep eases the ends so it slows down at the top and bottom
        float half = floatCycleTime * 0.5f;
        float t = Mathf.PingPong(Time.time + phaseOffset, half) / half;
        float eased = Mathf.SmoothStep(0f, 1f, t);

        float offsetY = Mathf.Lerp(-floatHeight, floatHeight, eased);
        bobTarget.localPosition = startLocalPosition + Vector3.up * offsetY;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;

        // The player's colliders sit on the same object as PlayerController
        PlayerController player = other.GetComponentInParent<PlayerController>();
        if (player == null) return;

        PlayerData_Manager manager = PlayerData_Manager.Instance;
        if (manager == null)
        {
            Debug.LogWarning("[FormUnlockPickup] No PlayerData_Manager in the scene.", this);
            return;
        }

        collected = true;

        manager.UnlockForm(formToUnlock);

        if (switchToFormOnPickup)
        {
            player.SwitchForm(formToUnlock);
        }

        Destroy(gameObject);
    }
}