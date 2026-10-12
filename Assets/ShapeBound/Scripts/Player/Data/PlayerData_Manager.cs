using System;
using UnityEngine;

public class PlayerData_Manager : MonoBehaviour
{
    public static PlayerData_Manager Instance { get; private set; }

    [SerializeField] private PlayerData playerData;

    [Tooltip("Testing only: unlock every form when the game starts.")]
    [SerializeField] private bool unlockAllForTesting = false;

    public PlayerData PlayerData => playerData;

    public event Action<ShapeForm> OnFormUnlocked;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DefaultPlayerData();

        if (unlockAllForTesting)
        {
            foreach (ShapeForm form in Enum.GetValues(typeof(ShapeForm)))
            {
                playerData.Unlock(form);
            }
        }
    }

    public void DefaultPlayerData()
    {
        playerData = PlayerData.CreateDefault();
    }

    public bool UnlockForm(ShapeForm form)
    {
        if (!playerData.Unlock(form)) return false;

        OnFormUnlocked?.Invoke(form);
        return true;
    }
}