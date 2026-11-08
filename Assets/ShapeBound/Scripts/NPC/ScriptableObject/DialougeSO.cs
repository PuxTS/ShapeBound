using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueSO : ScriptableObject
{
    [Header("NPC Dialogue Content")]
    [TextArea(2, 5)]
    [SerializeField] private string[] npcLines;

    [Header("Sign Dialogue Content (When NPC Hides)")]
    [TextArea(2, 5)]
    [SerializeField] private string[] signLines;

    [Header("Reward Options")]
    [SerializeField] private bool givesFormReward = false;
    [SerializeField] private ShapeForm formToUnlock = ShapeForm.Box;
    [SerializeField] private bool switchToFormOnUnlock = true;

    [Header("Progression Triggers")]
    [Tooltip("The camera zone name where this NPC starts showing up.")]
    [SerializeField] private string revealZoneName = "Zone 1.1";
    [Tooltip("The camera zone name where the NPC hides and leaves a sign instead.")]
    [SerializeField] private string hideZoneName = "Zone 1.2";

    // Public Getters
    public string[] NpcLines => npcLines;
    public string[] SignLines => signLines;
    public bool GivesFormReward => givesFormReward;
    public ShapeForm FormToUnlock => formToUnlock;
    public bool SwitchToFormOnUnlock => switchToFormOnUnlock;
    public string RevealZoneName => revealZoneName;
    public string HideZoneName => hideZoneName;
}