using UnityEngine;
using UnityEngine.UI;

public class ShapeFormUI : MonoBehaviour
{
    [System.Serializable]
    public struct ShapeUIItem
    {
        public string name;
        public ShapeForm shapeType;
        public Image backgroundImage;
        public Image spriteImage;
    }

    [Header("UI References")]
    [SerializeField] private ShapeUIItem[] shapeItems;

    [Header("Appearance Settings")]
    [SerializeField] private Color activeColor = Color.white;
    [SerializeField] private Color inactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.6f);
    [SerializeField] private Color lockedColor = new Color(0f, 0f, 0f, 0.25f);

    private PlayerController playerController;
    private PlayerData_Manager dataManager;
    private ShapeForm activeForm = ShapeForm.Circle;

    private void Start()
    {
        playerController = FindAnyObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerController.OnFormChanged += UpdateActiveShape;
        }

        dataManager = PlayerData_Manager.Instance;
        if (dataManager != null)
        {
            dataManager.OnFormUnlocked += HandleFormUnlocked;
        }

        // Sync now in case the player already picked its starting form before this ran
        if (playerController != null && playerController.CurrentFormData != null)
        {
            activeForm = playerController.CurrentFormData.form;
        }

        UpdateActiveShape(activeForm);
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.OnFormChanged -= UpdateActiveShape;
        }

        if (dataManager != null)
        {
            dataManager.OnFormUnlocked -= HandleFormUnlocked;
        }
    }

    private void HandleFormUnlocked(ShapeForm form)
    {
        UpdateActiveShape(activeForm);
    }

    public void UpdateActiveShape(ShapeForm currentForm)
    {
        activeForm = currentForm;

        if (shapeItems == null || shapeItems.Length == 0) return;

        PlayerData_Manager manager = PlayerData_Manager.Instance;

        foreach (var item in shapeItems)
        {
            // No manager in the scene means no restrictions
            bool unlocked = manager == null || manager.PlayerData.IsUnlocked(item.shapeType);
            bool isActive = item.shapeType == currentForm;

            Color targetColor = !unlocked ? lockedColor : (isActive ? activeColor : inactiveColor);

            if (item.backgroundImage != null)
                item.backgroundImage.color = targetColor;

            if (item.spriteImage != null)
                item.spriteImage.color = targetColor;
        }
    }
}