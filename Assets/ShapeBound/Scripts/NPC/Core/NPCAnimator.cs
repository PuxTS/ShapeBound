using UnityEngine;
using System.Collections;

public enum NPCAnimState { Appear, Idle, Talking, Disappear }

public class NPCAnimator : MonoBehaviour
{
    [Header("Animator Reference")]
    [SerializeField] private Animator animator;

    [Header("Animator State Names")]
    [SerializeField] private string appearState = "Appear";
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string talkingState = "Talking";
    [SerializeField] private string disappearState = "Disappear";

    [Header("Timing")]
    [SerializeField] private float crossFadeTime = 0.1f;

    [field: SerializeField] public NPCAnimState State { get; private set; } = NPCAnimState.Idle;

    private string currentPlayedState = "";
    private bool isLockedInSequence = false;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (isLockedInSequence || animator == null) return;

        PlayCurrentState();
    }

    public void SetState(NPCAnimState newState)
    {
        if (isLockedInSequence) return;
        State = newState;
    }

    public void PlayAppearSequence(System.Action onComplete = null)
    {
        StartCoroutine(RunSequence(NPCAnimState.Appear, appearState, 0.5f, NPCAnimState.Idle, onComplete));
    }

    public void PlayDisappearSequence(System.Action onComplete = null)
    {
        StartCoroutine(RunSequence(NPCAnimState.Disappear, disappearState, 0.5f, NPCAnimState.Idle, onComplete));
    }

    private IEnumerator RunSequence(NPCAnimState targetEnumState, string stateName, float duration, NPCAnimState nextEnumState, System.Action onComplete)
    {
        isLockedInSequence = true;
        State = targetEnumState;
        PlayForcedState(stateName);

        yield return new WaitForSeconds(duration);

        State = nextEnumState;
        isLockedInSequence = false;
        onComplete?.Invoke();
    }

    private string GetStateName(NPCAnimState state)
    {
        switch (state)
        {
            case NPCAnimState.Appear: return appearState;
            case NPCAnimState.Talking: return talkingState;
            case NPCAnimState.Disappear: return disappearState;
            default: return idleState;
        }
    }

    private void PlayCurrentState()
    {
        string stateName = GetStateName(State);
        PlayForcedState(stateName);
    }

    private void PlayForcedState(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName)) return;

        int hash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, hash)) return;

        if (stateName == currentPlayedState) return;

        if (crossFadeTime > 0f && !string.IsNullOrEmpty(currentPlayedState))
        {
            animator.CrossFadeInFixedTime(hash, crossFadeTime, 0);
        }
        else
        {
            animator.Play(hash, 0, 0f);
        }

        currentPlayedState = stateName;
    }
}