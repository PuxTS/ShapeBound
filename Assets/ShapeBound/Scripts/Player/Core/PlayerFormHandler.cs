using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns which form the player is in: looks up the FormData, respects unlocked forms,
/// asks the Shapeshifter to swap colliders/visuals, and announces the change.
/// </summary>
[RequireComponent(typeof(Shapeshifter))]
public class PlayerFormHandler : MonoBehaviour
{
    [Header("Form Data (one asset per form)")]
    [SerializeField] private FormData[] forms;

    public FormData CurrentFormData { get; private set; }

    /// <summary>Fires after the player switches form (also once at start).</summary>
    public event Action<ShapeForm> OnFormChanged;

    private Shapeshifter shapeSystem;
    private PlayerLauncher launcher;
    private Dictionary<ShapeForm, FormData> formLookup;

    private void Awake()
    {
        shapeSystem = GetComponent<Shapeshifter>();
        launcher = GetComponent<PlayerLauncher>();

        formLookup = new Dictionary<ShapeForm, FormData>();
        foreach (FormData data in forms)
        {
            if (data != null) formLookup[data.form] = data;
        }
    }

    /// <summary>Picks the starting form. Never starts in a locked form.</summary>
    public void InitializeStartingForm()
    {
        ShapeForm startForm = shapeSystem.CurrentForm;
        if (!IsFormUnlocked(startForm)) startForm = ShapeForm.Circle;

        SwitchForm(startForm);
    }

    // No PlayerData_Manager in the scene means no restrictions
    public bool IsFormUnlocked(ShapeForm form)
    {
        return PlayerData_Manager.Instance == null || PlayerData_Manager.Instance.PlayerData.IsUnlocked(form);
    }

    public void SwitchForm(ShapeForm newForm)
    {
        if (!formLookup.TryGetValue(newForm, out FormData data))
        {
            Debug.LogWarning($"No FormData assigned for {newForm}", this);
            return;
        }

        // Respect the player data: locked forms can't be switched to
        if (!IsFormUnlocked(newForm))
            return;

        // Can't change shape while aiming a launch
        if (launcher != null && launcher.IsCharging)
            return;

        shapeSystem.SetShape(newForm);
        if (shapeSystem.CurrentForm != newForm)
            return;

        CurrentFormData = data;

        Collider2D activeCollider = shapeSystem.GetActiveCollider();
        if (activeCollider != null)
        {
            activeCollider.sharedMaterial = data.material;
        }

        // Other player scripts (spin, movement, visuals) react to this
        OnFormChanged?.Invoke(newForm);
    }
}