using System.Collections;
using UnityEngine;

public class RadioPostPullAnimation : MonoBehaviour
{
    [SerializeField] private RadioTutorialPuzzle puzzle;
    [SerializeField] private GameObject[] worldIdleViewsToHide;
    [SerializeField] private GameObject juicerIdleView;
    [SerializeField] private GameObject radioAnimationView;
    [SerializeField] private Animator radioAnimator;
    [SerializeField] private AnimationClip radioAnimationClip;
    [SerializeField] private GameObject juicerExecuteAnimationView;
    [SerializeField] private Animator juicerExecuteAnimator;
    [SerializeField] private AnimationClip juicerExecuteAnimationClip;
    [SerializeField] private JuicerAnimationSfx juicerAnimationSfx;
    [SerializeField] private float fallbackDurationSeconds = 1f;

    private Coroutine playRoutine;

    private void OnEnable()
    {
        if (puzzle != null)
        {
            puzzle.LeverSequenceStarted += HandleLeverSequenceStarted;
        }
    }

    private void OnDisable()
    {
        if (puzzle != null)
        {
            puzzle.LeverSequenceStarted -= HandleLeverSequenceStarted;
        }
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
    }

    private void HandleLeverSequenceStarted()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRadioAnimationRoutine());
    }

    private IEnumerator PlayRadioAnimationRoutine()
    {
        if (radioAnimationView != null)
        {
            radioAnimationView.SetActive(true);
        }

        ResetAnimator(radioAnimator);

        // Let the animated view become visible before hiding the static view underneath it.
        yield return null;

        SetViewsActive(worldIdleViewsToHide, false);

        yield return new WaitForSeconds(GetDuration(radioAnimationClip));

        if (radioAnimationView != null)
        {
            radioAnimationView.SetActive(false);
        }

        if (juicerExecuteAnimationView != null)
        {
            juicerExecuteAnimationView.SetActive(true);
        }

        SetViewActive(juicerIdleView, false);
        ResetAnimator(juicerExecuteAnimator);
        if (juicerAnimationSfx != null)
        {
            juicerAnimationSfx.PlaySequence();
        }

        yield return new WaitForSeconds(GetDuration(juicerExecuteAnimationClip));

        if (juicerExecuteAnimationView != null)
        {
            juicerExecuteAnimationView.SetActive(false);
        }

        SetViewActive(juicerIdleView, true);
        SetViewsActive(worldIdleViewsToHide, true);

        if (puzzle != null)
        {
            puzzle.CompleteLeverSequence();
        }

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
