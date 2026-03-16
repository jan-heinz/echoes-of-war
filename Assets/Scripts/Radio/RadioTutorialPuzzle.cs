using System;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// radio puzzle logic for tutorial
//  1. tracks puzzle state (scan -> tune -> decoded -> solved)
//  2. maps left knob positions to channel indices
//  3. evaluates right knob clarity around the tuning target
//  4. handles static/choir mix feedback
//  5. reveals translated text when lever is pulled
public class RadioTutorialPuzzle : MonoBehaviour
{
    // raised once when player first lands on the true left knob channel
    public event Action TrueChannelFound;
    // raised once when player first enters the valid right knob tune zone
    public event Action RightKnobTuned;
    // raised when player pulls lever and translated text is shown
    public event Action LeverPulled;
    // raised when the lever is pulled and the post-pull presentation should begin
    public event Action LeverSequenceStarted;

    private enum PuzzleState
    {
        Scan,
        Tune,
        DecodedAwaitLever,
        LeverSequencePlaying,
        Solved
    }

    [Header("References")]
    [SerializeField] private RadioController radioController;
    [SerializeField] private Button leverButton;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private TMP_Text subtitleText;

    [Header("Close-Up Cursor")]
    [SerializeField] private RectTransform closeUpCursor;
    [SerializeField] private RectTransform[] channelCursorTargets;

    [Header("Close-Up Clearance Pointer")]
    [SerializeField] private RectTransform closeUpClearancePointer;
    [SerializeField] private RectTransform clearancePointerStartTarget;
    [SerializeField] private RectTransform clearancePointerEndTarget;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource staticLoopSource;
    [SerializeField] private AudioSource choirLoopSource;
    [FormerlySerializedAs("sfxSource")]
    [SerializeField] private AudioSource successAudioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip staticClip;
    [SerializeField] private AudioClip choirClip;
    [SerializeField] private AudioClip successBingClip;

    [Header("Puzzle Config")]
    [Tooltip("Total number of channel positions on the left knob.")]
    [SerializeField] private int channelCount = 8;
    [Tooltip("Which channel index (0-based) is the real signal channel.")]
    [SerializeField] private int trueChannelIndex = 2;
    [Tooltip("Right knob target position in normalized range [0..1) where tuning is optimal.")]
    [SerializeField] private float tuningTargetNormalized = 0.38f;
    [Tooltip("How far from the tuning target still counts as 'in tune' (normalized range).")]
    [SerializeField] private float tuningZoneHalfWidth = 0.08f;
    [Tooltip("How long the right knob must stay in tune before decode succeeds.")]
    [SerializeField] private float requiredHoldSeconds = 1.0f;

    [Header("Audio Mix")]
    [Tooltip("Static volume used on incorrect channels.")]
    [SerializeField] private float decoyStaticVolume = 0.8f;
    [Tooltip("Static volume on the true channel before tuning improves clarity.")]
    [SerializeField] private float trueUntunedStaticVolume = 0.7f;
    [Tooltip("Choir cue volume on the true channel before tuning improves clarity.")]
    [SerializeField] private float trueUntunedChoirVolume = 0.35f;
    [Tooltip("Shape of tuning blend: 1 = linear, >1 = slower start, <1 = faster start.")]
    [SerializeField] private float blendCurvePower = 1f;

    [Header("Text")]
    [SerializeField] private string scanHintText = "Scan channels with left knob.";
    [SerializeField] private string tuneHintText = "Tune with right knob.";
    [SerializeField] private string decodedHintText = "Decoded. Pull lever to translate.";
    [SerializeField] private string solvedHintText = "Translation complete.";
    [FormerlySerializedAs("solvedSubtitleText")]
    [SerializeField] [TextArea] private string translatedMessageText = "[TRANSLATED MESSAGE]";

    private PuzzleState puzzleState;
    private int currentChannelIndex = -1;
    private float holdTimerSeconds;
    private bool isInTuneZone;
    private float cachedRightKnobNormalized;
    private float clearancePointerBaselineNormalized;
    private bool hasStartedAudioLoops;
    // prevents repeated event spam while player stays on true channel
    private bool hasBroadcastTrueChannelFound;
    // prevents repeated event spam while player stays in tune zone
    private bool hasBroadcastRightKnobTuned;

    // subscribes to knob change events
    private void OnEnable()
    {
        if (radioController != null)
        {
            radioController.KnobChanged += HandleKnobChanged;
            radioController.LeverThresholdReached += HandleLeverThresholdReached;
        }
    }

    // unhooks knob events and stops loops
    private void OnDisable()
    {
        if (radioController != null)
        {
            radioController.KnobChanged -= HandleKnobChanged;
            radioController.LeverThresholdReached -= HandleLeverThresholdReached;
        }

        StopAudioLoops();
    }

    // validates setup and starts puzzle state
    private void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        InitializeAudio();
        ResetPuzzleState();
    }

    // updates hold timer while right knob stays in the valid zone
    private void Update()
    {
        // stop puzzle loops when close-up is closed
        if (radioController != null && !radioController.IsCloseUpOpen && hasStartedAudioLoops)
        {
            StopAudioLoops();
        }

        if (puzzleState != PuzzleState.Tune || !IsOnTrueChannel())
        {
            return;
        }

        if (isInTuneZone)
        {
            holdTimerSeconds += Time.deltaTime;
        }
        else
        {
            holdTimerSeconds = 0f;
        }

        if (holdTimerSeconds >= requiredHoldSeconds)
        {
            CompleteDecode();
        }
    }

    // called by pull lever button
    // starts the post-pull sequence and exits close-up
    public void PullLever()
    {
        if (puzzleState != PuzzleState.DecodedAwaitLever)
        {
            return;
        }

        puzzleState = PuzzleState.LeverSequencePlaying;
        StopAudioLoops();

        if (radioController != null && radioController.IsCloseUpOpen)
        {
            radioController.CloseCloseUp();
        }

        LeverSequenceStarted?.Invoke();
    }

    // hides translated subtitle text object
    public void HideSubtitleText()
    {
        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(false);
        }
    }

    public bool CanPullLever()
    {
        return puzzleState == PuzzleState.DecodedAwaitLever;
    }

    public void CompleteLeverSequence()
    {
        if (puzzleState != PuzzleState.LeverSequencePlaying)
        {
            return;
        }

        puzzleState = PuzzleState.Solved;
        SetHint(solvedHintText);

        if (subtitleText != null)
        {
            subtitleText.text = translatedMessageText;
            subtitleText.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("RadioTutorialPuzzle: subtitleText is not assigned.");
        }

        LeverPulled?.Invoke();
    }

    // validates required references and clamps config
    private bool ValidateSetup()
    {
        var isValid = true;

        if (radioController == null)
        {
            Debug.LogError("RadioTutorialPuzzle: radioController is not assigned.");
            isValid = false;
        }

        if (leverButton == null)
        {
            Debug.LogWarning("RadioTutorialPuzzle: leverButton is not assigned. Visual lever interaction will still work.");
        }

        if (staticLoopSource == null)
        {
            Debug.LogError("RadioTutorialPuzzle: staticLoopSource is not assigned.");
            isValid = false;
        }

        if (choirLoopSource == null)
        {
            Debug.LogError("RadioTutorialPuzzle: choirLoopSource is not assigned.");
            isValid = false;
        }

        if (successAudioSource == null)
        {
            Debug.LogError("RadioTutorialPuzzle: successAudioSource is not assigned.");
            isValid = false;
        }

        if (closeUpCursor == null)
        {
            Debug.LogError("RadioTutorialPuzzle: closeUpCursor is not assigned.");
            isValid = false;
        }

        if (channelCount < 1)
        {
            Debug.LogWarning("RadioTutorialPuzzle: channelCount must be at least 1. Setting to 1.");
            channelCount = 1;
        }

        if (channelCursorTargets == null || channelCursorTargets.Length != channelCount)
        {
            Debug.LogError($"RadioTutorialPuzzle: channelCursorTargets must contain exactly {channelCount} entries.");
            isValid = false;
        }
        else
        {
            for (var i = 0; i < channelCursorTargets.Length; i++)
            {
                if (channelCursorTargets[i] == null)
                {
                    Debug.LogError($"RadioTutorialPuzzle: channelCursorTargets[{i}] is not assigned.");
                    isValid = false;
                }
            }
        }

        if (closeUpClearancePointer == null)
        {
            Debug.LogError("RadioTutorialPuzzle: closeUpClearancePointer is not assigned.");
            isValid = false;
        }

        if (clearancePointerStartTarget == null)
        {
            Debug.LogError("RadioTutorialPuzzle: clearancePointerStartTarget is not assigned.");
            isValid = false;
        }

        if (clearancePointerEndTarget == null)
        {
            Debug.LogError("RadioTutorialPuzzle: clearancePointerEndTarget is not assigned.");
            isValid = false;
        }

        CacheClearancePointerBaseline();

        tuningZoneHalfWidth = Mathf.Max(0.0001f, tuningZoneHalfWidth);
        requiredHoldSeconds = Mathf.Max(0f, requiredHoldSeconds);

        decoyStaticVolume = Mathf.Clamp01(decoyStaticVolume);
        trueUntunedStaticVolume = Mathf.Clamp01(trueUntunedStaticVolume);
        trueUntunedChoirVolume = Mathf.Clamp01(trueUntunedChoirVolume);
        blendCurvePower = Mathf.Max(0.01f, blendCurvePower);

        tuningTargetNormalized = Mathf.Repeat(tuningTargetNormalized, 1f);
        trueChannelIndex = Mathf.Clamp(trueChannelIndex, 0, channelCount - 1);

        return isValid;
    }

    // configures audio sources
    // loops start only after player interacts with left knob
    private void InitializeAudio()
    {
        if (staticLoopSource != null)
        {
            staticLoopSource.clip = staticClip;
            staticLoopSource.loop = true;
            staticLoopSource.playOnAwake = false;
        }

        if (choirLoopSource != null)
        {
            choirLoopSource.clip = choirClip;
            choirLoopSource.loop = true;
            choirLoopSource.playOnAwake = false;
        }

        if (successAudioSource != null)
        {
            successAudioSource.loop = false;
            successAudioSource.playOnAwake = false;
        }

        hasStartedAudioLoops = false;
    }

    // resets puzzle back to initial scan state
    private void ResetPuzzleState()
    {
        StopAudioLoops();
        puzzleState = PuzzleState.Scan;
        holdTimerSeconds = 0f;
        isInTuneZone = false;
        hasBroadcastTrueChannelFound = false;
        hasBroadcastRightKnobTuned = false;

        if (leverButton != null)
        {
            leverButton.interactable = false;
        }

        if (subtitleText != null)
        {
            subtitleText.gameObject.SetActive(false);
        }

        cachedRightKnobNormalized = radioController != null
            ? radioController.GetKnobNormalized(RadioKnobId.Right)
            : 0f;

        SetHint(scanHintText);
        ApplyDecoyAudioMix();
        UpdateCloseUpClearancePointerPosition();

        if (radioController != null)
        {
            HandleLeftKnobChanged(radioController.GetKnobNormalized(RadioKnobId.Left));
        }
    }

    // handles changes from both knobs
    private void HandleKnobChanged(RadioKnobId knobId, float angle, float normalized)
    {
        if (puzzleState == PuzzleState.Solved || puzzleState == PuzzleState.DecodedAwaitLever)
        {
            return;
        }

        if (knobId == RadioKnobId.Left)
        {
            EnsureAudioLoopsPlaying();
            HandleLeftKnobChanged(normalized);
            return;
        }

        HandleRightKnobChanged(normalized);
    }

    // left knob chooses channel index
    // true channel enters tune state, others stay in scan
    private void HandleLeftKnobChanged(float normalized)
    {
        var nextChannelIndex = ChannelIndexFromNormalized(normalized);

        if (nextChannelIndex == currentChannelIndex)
        {
            return;
        }

        currentChannelIndex = nextChannelIndex;
        holdTimerSeconds = 0f;
        isInTuneZone = false;
        UpdateCloseUpCursorPosition();

        if (IsOnTrueChannel())
        {
            puzzleState = PuzzleState.Tune;
            if (!hasBroadcastTrueChannelFound)
            {
                hasBroadcastTrueChannelFound = true;
                TrueChannelFound?.Invoke();
            }

            SetHint(tuneHintText);
            EvaluateTrueChannelTuning();
            return;
        }

        puzzleState = PuzzleState.Scan;
        SetHint(scanHintText);
        ApplyDecoyAudioMix();
    }

    // right knob updates tuning progress on the true channel
    private void HandleRightKnobChanged(float normalized)
    {
        cachedRightKnobNormalized = Mathf.Repeat(normalized, 1f);
        UpdateCloseUpClearancePointerPosition();

        if (puzzleState != PuzzleState.Tune || !IsOnTrueChannel())
        {
            return;
        }

        EvaluateTrueChannelTuning();
    }

    // computes clarity from circular distance and updates audio mix
    private void EvaluateTrueChannelTuning()
    {
        var distance = CircularDistance01(cachedRightKnobNormalized, tuningTargetNormalized);
        var clarity = Mathf.Clamp01(1f - (distance / tuningZoneHalfWidth));

        ApplyTrueChannelAudioMix(clarity);
        isInTuneZone = distance <= tuningZoneHalfWidth;

        if (isInTuneZone && !hasBroadcastRightKnobTuned)
        {
            hasBroadcastRightKnobTuned = true;
            RightKnobTuned?.Invoke();
        }
    }

    // marks decode complete when hold timer threshold is reached
    private void CompleteDecode()
    {
        if (puzzleState == PuzzleState.DecodedAwaitLever || puzzleState == PuzzleState.Solved)
        {
            return;
        }

        puzzleState = PuzzleState.DecodedAwaitLever;
        holdTimerSeconds = requiredHoldSeconds;
        isInTuneZone = true;

        ApplyTrueChannelAudioMix(1f);

        if (leverButton != null)
        {
            leverButton.interactable = true;
        }

        if (successAudioSource != null && successBingClip != null)
        {
            successAudioSource.PlayOneShot(successBingClip);
        }

        SetHint(decodedHintText);
    }

    private void HandleLeverThresholdReached()
    {
        PullLever();
    }

    // decoy channel mix: static only, no choir cue
    private void ApplyDecoyAudioMix()
    {
        if (staticLoopSource != null)
        {
            staticLoopSource.volume = decoyStaticVolume;
        }

        if (choirLoopSource != null)
        {
            choirLoopSource.volume = 0f;
        }
    }

    // true channel mix
    // untuned: static heavy + light choir
    // tuned: static 0 + choir 1
    private void ApplyTrueChannelAudioMix(float clarity)
    {
        var clampedClarity = Mathf.Clamp01(clarity);
        var blend = Mathf.Pow(clampedClarity, blendCurvePower);

        if (staticLoopSource != null)
        {
            staticLoopSource.volume = Mathf.Lerp(trueUntunedStaticVolume, 0f, blend);
        }

        if (choirLoopSource != null)
        {
            choirLoopSource.volume = Mathf.Lerp(trueUntunedChoirVolume, 1f, blend);
        }
    }

    // returns true when current channel matches configured target
    private bool IsOnTrueChannel()
    {
        return currentChannelIndex == trueChannelIndex;
    }

    // converts normalized knob position into channel index
    private int ChannelIndexFromNormalized(float normalized)
    {
        var wrapped = Mathf.Repeat(normalized, 1f);
        var index = Mathf.FloorToInt(wrapped * channelCount);
        return Mathf.Clamp(index, 0, channelCount - 1);
    }

    private void UpdateCloseUpCursorPosition()
    {
        if (closeUpCursor == null || channelCursorTargets == null)
        {
            return;
        }

        if (currentChannelIndex < 0 || currentChannelIndex >= channelCursorTargets.Length)
        {
            return;
        }

        var mirroredIndex = (channelCursorTargets.Length - currentChannelIndex) % channelCursorTargets.Length;
        var target = channelCursorTargets[mirroredIndex];
        if (target == null)
        {
            return;
        }

        closeUpCursor.anchoredPosition = target.anchoredPosition;
    }

    private void UpdateCloseUpClearancePointerPosition()
    {
        if (closeUpClearancePointer == null || clearancePointerStartTarget == null || clearancePointerEndTarget == null)
        {
            return;
        }

        var pointerNormalized = Mathf.Repeat(clearancePointerBaselineNormalized - cachedRightKnobNormalized, 1f);
        closeUpClearancePointer.anchoredPosition = Vector2.Lerp(
            clearancePointerStartTarget.anchoredPosition,
            clearancePointerEndTarget.anchoredPosition,
            pointerNormalized);
    }

    private void CacheClearancePointerBaseline()
    {
        if (closeUpClearancePointer == null || clearancePointerStartTarget == null || clearancePointerEndTarget == null)
        {
            clearancePointerBaselineNormalized = 0f;
            return;
        }

        var start = clearancePointerStartTarget.anchoredPosition;
        var end = clearancePointerEndTarget.anchoredPosition;
        var segment = end - start;
        var lengthSquared = segment.sqrMagnitude;

        if (lengthSquared <= Mathf.Epsilon)
        {
            clearancePointerBaselineNormalized = 0f;
            return;
        }

        var pointerOffset = closeUpClearancePointer.anchoredPosition - start;
        clearancePointerBaselineNormalized = Mathf.Clamp01(Vector2.Dot(pointerOffset, segment) / lengthSquared);
    }

    // shortest wraparound distance in 0..1 range
    private static float CircularDistance01(float a, float b)
    {
        var delta = Mathf.Abs(a - b);
        return Mathf.Min(delta, 1f - delta);
    }

    // writes hint text when label is assigned
    private void SetHint(string value)
    {
        if (hintText != null)
        {
            hintText.text = value;
        }
    }

    // starts loop audio only when left knob is used
    private void EnsureAudioLoopsPlaying()
    {
        if (hasStartedAudioLoops)
        {
            return;
        }

        if (staticLoopSource != null && staticLoopSource.clip != null)
        {
            staticLoopSource.Play();
        }

        if (choirLoopSource != null && choirLoopSource.clip != null)
        {
            choirLoopSource.Play();
        }

        hasStartedAudioLoops = true;
    }

    // stops all loop sources and clears started flag
    private void StopAudioLoops()
    {
        if (staticLoopSource != null && staticLoopSource.isPlaying)
        {
            staticLoopSource.Stop();
        }

        if (choirLoopSource != null && choirLoopSource.isPlaying)
        {
            choirLoopSource.Stop();
        }

        hasStartedAudioLoops = false;
    }
}
