using System;
using System.Collections;
using UnityEngine;

public class RadioPostPullAnimation : MonoBehaviour
{
    public event Action IntelligencePageUnlocked;

    [SerializeField] private RadioTutorialPuzzle puzzle;
    [SerializeField] private GameObject[] worldIdleViewsToHide;
    [SerializeField] private GameObject juicerIdleView;
    [SerializeField] private GameObject radioAnimationView;
    [SerializeField] private Animator radioAnimator;
    [SerializeField] private AnimationClip radioAnimationClip;
    [SerializeField] private AudioSource radioWhirringAudioSource;
    [SerializeField] private AudioClip radioWhirringClip;
    [SerializeField] [Range(0f, 1f)] private float radioWhirringVolume = 1f;
    [SerializeField] private GameObject juicerExecuteAnimationView;
    [SerializeField] private Animator juicerExecuteAnimator;
    [SerializeField] private AnimationClip juicerExecuteAnimationClip;
    [SerializeField] private JuicerAnimationSfx juicerAnimationSfx;
    [SerializeField] private GameObject typewriterAnimationView;
    [SerializeField] private Animator typewriterAnimator;
    [SerializeField] private AnimationClip typewriterAnimationClip;
    [SerializeField] private GameObject typewriterClickTarget;
    [SerializeField] private NewspaperPopup intelligencePopup;
    [SerializeField] private AudioSource typewriterTypingAudioSource;
    [SerializeField] private AudioClip typewriterTypingClip;
    [SerializeField] [Range(0f, 1f)] private float typewriterTypingVolume = 1f;
    [SerializeField] private AudioSource typewriterBellAudioSource;
    [SerializeField] private AudioClip typewriterBellClip;
    [SerializeField] [Range(0f, 1f)] private float typewriterBellVolume = 1f;
    [SerializeField] private float fallbackDurationSeconds = 1f;

    private Coroutine playRoutine;
    private bool hasUnlockedIntelligencePage;
    private bool hasFinishedPostPullPresentation;
    private bool hasCompletedInitialIntelligenceReview;
    private bool isAwaitingIntelligenceClose;

    private void OnEnable()
    {
        if (puzzle != null)
        {
            puzzle.LeverSequenceStarted += HandleLeverSequenceStarted;
        }

        if (intelligencePopup != null)
        {
            intelligencePopup.Closed += HandleIntelligencePopupClosed;
        }
    }

    private void OnDisable()
    {
        if (puzzle != null)
        {
            puzzle.LeverSequenceStarted -= HandleLeverSequenceStarted;
        }

        if (intelligencePopup != null)
        {
            intelligencePopup.Closed -= HandleIntelligencePopupClosed;
        }

        StopRadioWhirringSfx();
        StopTypewriterTypingSfx();
    }

    private void Start()
    {
        SetViewsActive(worldIdleViewsToHide, true);
        SetViewActive(juicerIdleView, true);

        if (radioAnimationView != null)
        {
            radioAnimationView.SetActive(false);
        }

        if (juicerExecuteAnimationView != null)
        {
            juicerExecuteAnimationView.SetActive(false);
        }

        if (typewriterClickTarget != null)
        {
            typewriterClickTarget.SetActive(false);
        }

        hasUnlockedIntelligencePage = false;
        hasFinishedPostPullPresentation = false;
        hasCompletedInitialIntelligenceReview = false;
        isAwaitingIntelligenceClose = false;

        if (typewriterAnimator != null)
        {
            typewriterAnimator.Rebind();
            typewriterAnimator.Update(0f);
            typewriterAnimator.enabled = false;
        }
    }

    private void HandleLeverSequenceStarted()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        hasUnlockedIntelligencePage = false;
        hasFinishedPostPullPresentation = false;
        hasCompletedInitialIntelligenceReview = false;
        isAwaitingIntelligenceClose = false;

        playRoutine = StartCoroutine(PlayRadioAnimationRoutine());
    }

    private IEnumerator PlayRadioAnimationRoutine()
    {
        if (radioAnimationView != null)
        {
            radioAnimationView.SetActive(true);
        }

        PlayRadioWhirringSfx();
        ResetAnimator(radioAnimator);

        // Let the animated view become visible before hiding the static view underneath it.
        yield return null;

        SetViewsActive(worldIdleViewsToHide, false);

        yield return new WaitForSeconds(GetDuration(radioAnimationClip));

        if (radioAnimationView != null)
        {
            radioAnimationView.SetActive(false);
        }

        StopRadioWhirringSfx();

        if (juicerExecuteAnimationView != null)
        {
            juicerExecuteAnimationView.SetActive(true);
        }

        if (typewriterAnimationView != null)
        {
            typewriterAnimationView.SetActive(true);
        }

        SetViewActive(juicerIdleView, false);
        ResetAnimator(juicerExecuteAnimator);
        if (typewriterAnimator != null)
        {
            typewriterAnimator.enabled = true;
            ResetAnimator(typewriterAnimator);
        }

        StartTypewriterTypingSfx();

        if (juicerAnimationSfx != null)
        {
            juicerAnimationSfx.PlaySequence();
        }

        var juicerDuration = GetDuration(juicerExecuteAnimationClip);
        var typewriterDuration = typewriterAnimationClip != null
            ? GetDuration(typewriterAnimationClip)
            : 0f;

        if (typewriterDuration > 0f)
        {
            yield return new WaitForSeconds(typewriterDuration);
            FreezeTypewriterAtEndFrame();
        }

        UnlockIntelligencePage();

        var remainingJuicerDuration = Mathf.Max(0f, juicerDuration - typewriterDuration);
        if (remainingJuicerDuration > 0f)
        {
            yield return new WaitForSeconds(remainingJuicerDuration);
        }

        if (juicerExecuteAnimationView != null)
        {
            juicerExecuteAnimationView.SetActive(false);
        }

        SetViewActive(juicerIdleView, true);
        SetViewsActive(worldIdleViewsToHide, true);
        hasFinishedPostPullPresentation = true;
        TryCompleteLeverSequence();

        playRoutine = null;
    }

    private float GetDuration(AnimationClip clip)
    {
        return clip != null
            ? clip.length
            : Mathf.Max(0.01f, fallbackDurationSeconds);
    }

    private static void ResetAnimator(Animator animator)
    {
        if (animator == null)
        {
            return;
        }

        animator.Rebind();
        animator.Update(0f);
    }

    private void FreezeTypewriterAtEndFrame()
    {
        if (typewriterAnimator == null || typewriterAnimationClip == null || typewriterAnimationView == null)
        {
            return;
        }

        typewriterAnimationClip.SampleAnimation(typewriterAnimationView, typewriterAnimationClip.length);
        typewriterAnimator.enabled = false;
        StopTypewriterTypingSfx();
        PlayTypewriterBellSfx();
    }

    private void PlayRadioWhirringSfx()
    {
        if (radioWhirringAudioSource == null || radioWhirringClip == null)
        {
            return;
        }

        radioWhirringAudioSource.Stop();
        radioWhirringAudioSource.clip = radioWhirringClip;
        radioWhirringAudioSource.loop = false;
        radioWhirringAudioSource.volume = ClipVolumeRegistry.ScaleVolume(radioWhirringClip, radioWhirringVolume);
        radioWhirringAudioSource.Play();
    }

    private void StopRadioWhirringSfx()
    {
        if (radioWhirringAudioSource == null)
        {
            return;
        }

        if (radioWhirringAudioSource.isPlaying)
        {
            radioWhirringAudioSource.Stop();
        }

        radioWhirringAudioSource.clip = null;
    }

    public void OpenIntelligencePage()
    {
        if (!hasUnlockedIntelligencePage)
        {
            return;
        }

        if (intelligencePopup == null)
        {
            Debug.LogWarning("RadioPostPullAnimation: intelligencePopup is not assigned.");
            if (puzzle != null)
            {
                puzzle.CompleteLeverSequence();
            }
            return;
        }

        if (puzzle != null && !hasCompletedInitialIntelligenceReview && !puzzle.HasTranslatedMessageText())
        {
            hasCompletedInitialIntelligenceReview = true;
            isAwaitingIntelligenceClose = false;
            puzzle.CompleteLeverSequence();
            return;
        }

        if (puzzle != null)
        {
            puzzle.ShowTranslatedMessage();
        }

        isAwaitingIntelligenceClose = !hasCompletedInitialIntelligenceReview;
        intelligencePopup.ShowExistingText();
    }

    private void HandleIntelligencePopupClosed()
    {
        if (!isAwaitingIntelligenceClose)
        {
            return;
        }

        isAwaitingIntelligenceClose = false;
        hasCompletedInitialIntelligenceReview = true;
        TryCompleteLeverSequence();
    }

    private void UnlockIntelligencePage()
    {
        hasUnlockedIntelligencePage = true;

        if (typewriterClickTarget != null)
        {
            typewriterClickTarget.SetActive(true);
        }

        IntelligencePageUnlocked?.Invoke();
    }

    private void TryCompleteLeverSequence()
    {
        if (!hasFinishedPostPullPresentation || !hasCompletedInitialIntelligenceReview || puzzle == null)
        {
            return;
        }

        puzzle.CompleteLeverSequence();
    }

    private void StartTypewriterTypingSfx()
    {
        if (typewriterTypingAudioSource == null || typewriterTypingClip == null)
        {
            return;
        }

        typewriterTypingAudioSource.clip = typewriterTypingClip;
        typewriterTypingAudioSource.loop = true;
        typewriterTypingAudioSource.volume = ClipVolumeRegistry.ScaleVolume(typewriterTypingClip, typewriterTypingVolume);

        if (!typewriterTypingAudioSource.isPlaying)
        {
            typewriterTypingAudioSource.Play();
        }
    }

    private void StopTypewriterTypingSfx()
    {
        if (typewriterTypingAudioSource == null)
        {
            return;
        }

        if (typewriterTypingAudioSource.isPlaying)
        {
            typewriterTypingAudioSource.Stop();
        }
    }

    private void PlayTypewriterBellSfx()
    {
        if (typewriterBellAudioSource == null || typewriterBellClip == null)
        {
            return;
        }

        typewriterBellAudioSource.PlayOneShot(
            typewriterBellClip,
            ClipVolumeRegistry.ScaleVolume(typewriterBellClip, typewriterBellVolume));
    }

    private static void SetViewsActive(GameObject[] views, bool isActive)
    {
        if (views == null)
        {
            return;
        }

        for (var i = 0; i < views.Length; i++)
        {
            if (views[i] != null)
            {
                views[i].SetActive(isActive);
            }
        }
    }

    private static void SetViewActive(GameObject view, bool isActive)
    {
        if (view != null)
        {
            view.SetActive(isActive);
        }
    }
}
