using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public List<ShapeForm> unlockedForms = new List<ShapeForm>();

    public bool IsUnlocked(ShapeForm form)
    {
        return unlockedForms.Contains(form);
    }

    public bool Unlock(ShapeForm form)
    {
        if (unlockedForms.Contains(form)) return false;

        unlockedForms.Add(form);
        return true;
    }

    public static PlayerData CreateDefault()
    {
        PlayerData data = new PlayerData();
        data.unlockedForms.Add(ShapeForm.Circle);
        return data;
    }
}