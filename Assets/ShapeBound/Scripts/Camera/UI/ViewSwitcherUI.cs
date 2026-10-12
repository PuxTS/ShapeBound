using UnityEngine;
using UnityEngine.UI;

// Indicator only: swaps the icon to show the current view. Switching is done with the V key.
public class ViewSwitcherUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraManager cameraManager; // optional: falls back to CameraManager.Instance
    [SerializeField] private Image iconImage;

    [Header("Icons")]
    [SerializeField] private Sprite playerViewSprite;
    [SerializeField] private Sprite zoneViewSprite;

    private void Start()
    {
        if (cameraManager == null) cameraManager = CameraManager.Instance;

        if (cameraManager == null)
        {
            Debug.LogError("[ViewSwitcherUI] No CameraManager found.", this);
            enabled = false;
            return;
        }

        cameraManager.OnCameraModeChanged += UpdateIcon;

        // Sync with the current state
        UpdateIcon(cameraManager.IsPlayerCamActive);
    }

    private void OnDestroy()
    {
        if (cameraManager != null)
        {
            cameraManager.OnCameraModeChanged -= UpdateIcon;
        }
    }

    private void UpdateIcon(bool isPlayerCam)
    {
        if (iconImage == null) return;
        iconImage.sprite = isPlayerCam ? playerViewSprite : zoneViewSprite;
    }
}