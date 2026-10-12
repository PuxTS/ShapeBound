using System;
using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [Header("Input Reference")]
    [SerializeField] private InputReader inputReader;

    [Header("Player Camera")]
    [SerializeField] private CinemachineCamera playerFollowCamera;

    [Header("Priority Settings")]
    [SerializeField] private int playerActivePriority = 20;
    [SerializeField] private int playerInactivePriority = 0;

    [Header("Debug Status (Read-Only in Inspector)")]
    [SerializeField] private bool isPlayerCamActive = false;

    private CameraZone currentZone;

    public bool IsPlayerCamActive => isPlayerCamActive;
    public bool HasZone => currentZone != null;

    /// <summary>Fires whenever the view switches. Parameter: true = player cam, false = zone cam.</summary>
    public event Action<bool> OnCameraModeChanged;

    /// <summary>Fires when the player enters or leaves a zone. Parameter: true = standing in a zone.</summary>
    public event Action<bool> OnZoneAvailabilityChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.OnToggleCameraEvent += ToggleCamera;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.OnToggleCameraEvent -= ToggleCamera;
        }
    }

    public void SetCurrentZone(CameraZone zone)
    {
        if (currentZone == zone) return;

        if (currentZone != null && currentZone.ZoneCamera != null)
        {
            currentZone.ZoneCamera.Priority.Value = 0;
        }

        currentZone = zone;
        OnZoneAvailabilityChanged?.Invoke(currentZone != null);

        if (!isPlayerCamActive && currentZone != null && currentZone.ZoneCamera != null)
        {
            if (playerFollowCamera != null) playerFollowCamera.Priority.Value = playerInactivePriority;
            currentZone.ZoneCamera.Priority.Value = currentZone.ZonePriority;
        }
    }

    public void ClearCurrentZone(CameraZone zone)
    {
        if (currentZone == zone)
        {
            if (currentZone.ZoneCamera != null)
            {
                currentZone.ZoneCamera.Priority.Value = 0;
            }

            currentZone = null;
            OnZoneAvailabilityChanged?.Invoke(false);

            if (!isPlayerCamActive)
            {
                SwitchToPlayerCam();
            }
        }
    }

    public void ToggleCamera()
    {
        if (isPlayerCamActive)
        {
            if (currentZone != null && currentZone.ZoneCamera != null)
            {
                SwitchToZoneCam();
            }
            else
            {
                Debug.LogWarning("[CameraManager] Cannot switch: Not currently standing inside any CameraZone!");
            }
        }
        else
        {
            SwitchToPlayerCam();
        }
    }

    private void SwitchToZoneCam()
    {
        isPlayerCamActive = false;

        if (playerFollowCamera != null) playerFollowCamera.Priority.Value = playerInactivePriority;
        if (currentZone != null && currentZone.ZoneCamera != null)
        {
            currentZone.ZoneCamera.Priority.Value = currentZone.ZonePriority;
        }

        OnCameraModeChanged?.Invoke(isPlayerCamActive);
    }

    private void SwitchToPlayerCam()
    {
        isPlayerCamActive = true;

        if (currentZone != null && currentZone.ZoneCamera != null)
        {
            currentZone.ZoneCamera.Priority.Value = 0;
        }
        if (playerFollowCamera != null) playerFollowCamera.Priority.Value = playerActivePriority;

        OnCameraModeChanged?.Invoke(isPlayerCamActive);
    }
}