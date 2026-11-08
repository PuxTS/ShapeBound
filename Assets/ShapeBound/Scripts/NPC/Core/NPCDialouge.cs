using UnityEngine;
using TMPro;

public class NPCDialogue : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject textPopupContainer;
    [SerializeField] private TMP_Text sharedTextComponent;

    [Header("Child Objects & Animator")]
    [SerializeField] private GameObject npcObject; 
    [SerializeField] private GameObject signObject; 
    [SerializeField] private NPCAnimator npcAnimator;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueSO npcDialogueData;

    [Header("Input")]
    [SerializeField] private InputReader inputReader;

    [Header("Settings")]
    [SerializeField] private bool grantRewardOnlyOnce = true;

    private int currentDialogueIndex = 0;
    private bool isPlayerInRange = false;
    private bool isDialogueActive = false;
    private bool hasGrantedReward = false;
    private PlayerController playerInTrigger;
    private bool isUsingSign = false;

    private bool hasRevealedNpc = false;
    private bool hasReachedHideZone = false;
    private bool isDisappearing = false;

    private void Awake()
    {
        if (npcObject == null)
        {
            Transform t = transform.Find("NPC");
            if (t != null) npcObject = t.gameObject;
        }
        if (signObject == null)
        {
            Transform t = transform.Find("Sign");
            if (t != null) signObject = t.gameObject;
        }
    }

    private void Start()
    {
        if (textPopupContainer != null)
        {
            textPopupContainer.SetActive(false);
        }
        EvaluateState();
    }

    private void OnEnable()
    {
        if (inputReader != null) inputReader.OnInteractEvent += HandleInteract;
        CameraZone.OnZoneEntered += HandleZoneEntered;
    }

    private void OnDisable()
    {
        if (inputReader != null) inputReader.OnInteractEvent -= HandleInteract;
        CameraZone.OnZoneEntered -= HandleZoneEntered;
    }

    private void HandleZoneEntered(string zoneName)
    {
        if (npcDialogueData == null) return;

        if (!string.IsNullOrEmpty(npcDialogueData.RevealZoneName) && zoneName.Contains(npcDialogueData.RevealZoneName))
        {
            if (!hasRevealedNpc && npcObject != null)
            {
                hasRevealedNpc = true;
                if (npcObject != null) npcObject.SetActive(true);
                if (signObject != null) signObject.SetActive(false);

                if (npcAnimator != null)
                {
                    npcAnimator.PlayAppearSequence();
                }
                return;
            }
            hasRevealedNpc = true;
        }

        if (!string.IsNullOrEmpty(npcDialogueData.HideZoneName) && zoneName.Contains(npcDialogueData.HideZoneName))
        {
            hasReachedHideZone = true;
        }

        EvaluateState();
    }

    private void EvaluateState()
    {
        if (npcDialogueData == null) return;

        bool isFormUnlocked = false;
        if (npcDialogueData.GivesFormReward && grantRewardOnlyOnce)
        {
            PlayerData_Manager manager = PlayerData_Manager.Instance;
            if (manager != null && manager.PlayerData.IsUnlocked(npcDialogueData.FormToUnlock))
            {
                isFormUnlocked = true;
            }
        }

        if (hasReachedHideZone || isFormUnlocked)
        {
            ActivateSignMode();
        }
        else if (hasRevealedNpc)
        {
            ActivateNormalNpcMode();
        }
        else
        {
            DeactivateAll();
        }
    }

    private void ActivateSignMode()
    {
        isUsingSign = true;
        if (npcObject != null) npcObject.SetActive(false);
        if (signObject != null) signObject.SetActive(true);
    }

    private void ActivateNormalNpcMode()
    {
        isUsingSign = false;
        if (npcObject != null) npcObject.SetActive(true);
        if (signObject != null) signObject.SetActive(false);
        if (npcAnimator != null && !isDisappearing) npcAnimator.SetState(NPCAnimState.Idle);
    }

    private void DeactivateAll()
    {
        isUsingSign = false;
        if (npcObject != null) npcObject.SetActive(false);
        if (signObject != null) signObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((npcObject != null && npcObject.activeSelf) || (signObject != null && signObject.activeSelf))
        {
            PlayerController player = collision.GetComponentInParent<PlayerController>();
            if (player != null)
            {
                playerInTrigger = player;
                isPlayerInRange = true;
                currentDialogueIndex = 0;
                ShowDialogueUI();
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        PlayerController player = collision.GetComponentInParent<PlayerController>();
        if (player != null && player == playerInTrigger)
        {
            playerInTrigger = null;
            isPlayerInRange = false;
            ResetDialogueState();
        }
    }

    private void HandleInteract()
    {
        if (!isPlayerInRange || !isDialogueActive || npcDialogueData == null || isDisappearing) return;

        string[] activeLines = isUsingSign ? npcDialogueData.SignLines : npcDialogueData.NpcLines;
        if (activeLines == null || activeLines.Length == 0) return;

        currentDialogueIndex++;

        if (currentDialogueIndex < activeLines.Length)
        {
            UpdateDialogueText();
        }
        else
        {
            CompleteDialogue();
        }
    }

    private void CompleteDialogue()
    {
        if (!isUsingSign && npcDialogueData != null && npcDialogueData.GivesFormReward && (!grantRewardOnlyOnce || !hasGrantedReward))
        {
            PlayerData_Manager manager = PlayerData_Manager.Instance;
            if (manager != null)
            {
                manager.UnlockForm(npcDialogueData.FormToUnlock);
                hasGrantedReward = true;

                if (npcDialogueData.SwitchToFormOnUnlock && playerInTrigger != null)
                {
                    playerInTrigger.SwitchForm(npcDialogueData.FormToUnlock);
                }

                hasReachedHideZone = true;
                isDisappearing = true;
                ResetDialogueState();

                if (npcAnimator != null)
                {
                    npcAnimator.PlayDisappearSequence(() => {
                        isDisappearing = false;
                        ActivateSignMode();
                    });
                }
                return;
            }
        }

        ResetDialogueState();
    }

    private void ShowDialogueUI()
    {
        string[] activeLines = isUsingSign ? npcDialogueData.SignLines : npcDialogueData.NpcLines;
        if (npcDialogueData == null || activeLines == null || activeLines.Length == 0) return;

        isDialogueActive = true;
        if (textPopupContainer != null) textPopupContainer.SetActive(true);

        if (!isUsingSign && npcAnimator != null)
        {
            npcAnimator.SetState(NPCAnimState.Talking);
        }
        
        UpdateDialogueText();
    }

    private void UpdateDialogueText()
    {
        string[] activeLines = isUsingSign ? npcDialogueData.SignLines : npcDialogueData.NpcLines;
        if (sharedTextComponent != null && activeLines != null && currentDialogueIndex < activeLines.Length)
        {
            sharedTextComponent.text = activeLines[currentDialogueIndex];
        }
    }

    private void ResetDialogueState()
    {
        isDialogueActive = false;
        currentDialogueIndex = 0;

        if (textPopupContainer != null)
        {
            textPopupContainer.SetActive(false);
        }

        if (!isUsingSign && npcAnimator != null && !isDisappearing)
        {
            npcAnimator.SetState(NPCAnimState.Idle);
        }
    }
}