using System;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// minimal ink dialogue flow for tutorial MVP
//  - loads one compiled ink file
//  - starts at the configured knot
//  - advances per click
//  - applies speaker name + portrait
//  - can react to in-game events to jump to other dialogue
public class InkDialogue : MonoBehaviour
{
    // speaker tag format in ink: # speaker:<id>
    private const string SpeakerTagPrefix = "speaker:";
    
    // ui command tag format in ink: # ui:<command>
    private const string UiTagPrefix = "ui:";
    private const string DisplayNewspaperCommand = "display_newspaper";
    private const string HideSubtitlesCommand = "hide_subtitles";

    [Serializable]
    private class SpeakerVisual
    {
        [SerializeField] private string speakerId;
        [SerializeField] private string displayName;
        [SerializeField] private Sprite portrait;

        public string SpeakerId => speakerId;
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
    }

    [Header("Ink")]
    [SerializeField] private TextAsset compiledInkJson;
    [SerializeField] private string startKnot;
    [SerializeField] private string[] nextKnots;

    [Header("Gameplay Triggers")]
    [SerializeField] private RadioTutorialPuzzle radioTutorialPuzzle;
    [SerializeField] private bool useChannelSubmissionEvents;
    [SerializeField] private string trueChannelFoundKnot;
    [SerializeField] private string incorrectChannelSubmittedKnot;
    [SerializeField] private string rightKnobTunedKnot;
    [SerializeField] private string incorrectRightKnobSubmittedKnot;
    [SerializeField] private string leverPulledKnot;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text dialogueCounterText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image speakerPortraitImage;
    [SerializeField] private Button dialogueClickTarget;
    [SerializeField] private NewspaperPopup newspaperPopup;
    [SerializeField] private float charactersPerSecond = 45f;
    [SerializeField] private bool clickCompletesCurrentLine = true;
    [SerializeField] [Range(0f, 3f)] private float dialogueVoicePitch;
    [SerializeField] private AudioSource dialogueVoiceAudioSource;
    [SerializeField] private bool hidePanelWhenDone = true;

    [Header("Choices")]
    [SerializeField] private Button choiceButtonPrefab; 
    [SerializeField] private Transform choiceContainer; 

    [Header("Speakers")]
    [SerializeField] private SpeakerVisual[] speakers;

    public bool IsDialogueActive => dialoguePanel != null && dialoguePanel.activeSelf;

    private Story story;
    private readonly Queue<string> queuedKnots = new Queue<string>();
    private readonly Dictionary<string, SpeakerVisual> speakerLookup = new Dictionary<string, SpeakerVisual>();
    private bool isPausedForPopup;
    private bool wasDialoguePanelVisibleBeforePopup;
    private bool isLineRevealing;
    private string currentFullLine = string.Empty;
    private Coroutine lineRevealCoroutine;
    private int currentSequenceLineIndex;
    private int currentSequenceLineCount;

    // wires dialogue click target
    private void Awake()
    {
        if (dialogueClickTarget != null)
        {
            dialogueClickTarget.onClick.AddListener(AdvanceDialogue);
        }
    }

    // subscribes to optional puzzle events
    private void OnEnable()
    {
        if (radioTutorialPuzzle != null)
        {
            if (useChannelSubmissionEvents)
            {
                radioTutorialPuzzle.CorrectChannelSubmitted += HandleCorrectChannelSubmitted;
                radioTutorialPuzzle.IncorrectChannelSubmitted += HandleIncorrectChannelSubmitted;
                radioTutorialPuzzle.IncorrectRightKnobSubmitted += HandleIncorrectRightKnobSubmitted;
            }
            else
            {
                radioTutorialPuzzle.TrueChannelFound += HandleTrueChannelFound;
            }

            radioTutorialPuzzle.RightKnobTuned += HandleRightKnobTuned;
            radioTutorialPuzzle.LeverPulled += HandleLeverPulled;
        }

        if (newspaperPopup != null)
        {
            newspaperPopup.Closed += HandleNewspaperClosed;
        }
    }

    // unhooks optional puzzle events
    private void OnDisable()
    {
        if (radioTutorialPuzzle != null)
        {
            if (useChannelSubmissionEvents)
            {
                radioTutorialPuzzle.CorrectChannelSubmitted -= HandleCorrectChannelSubmitted;
                radioTutorialPuzzle.IncorrectChannelSubmitted -= HandleIncorrectChannelSubmitted;
                radioTutorialPuzzle.IncorrectRightKnobSubmitted -= HandleIncorrectRightKnobSubmitted;
            }
            else
            {
                radioTutorialPuzzle.TrueChannelFound -= HandleTrueChannelFound;
            }

            radioTutorialPuzzle.RightKnobTuned -= HandleRightKnobTuned;
            radioTutorialPuzzle.LeverPulled -= HandleLeverPulled;
        }

        if (newspaperPopup != null)
        {
            newspaperPopup.Closed -= HandleNewspaperClosed;
        }

        StopLineReveal();
    }

    // creates story and shows first line
    private void Start()
    {
        if (!ValidateSetup())
        {
            enabled = false;
            return;
        }

        story = new Story(compiledInkJson.text);
        CacheQueuedKnots();
        BuildSpeakerLookup();

        if (!string.IsNullOrWhiteSpace(startKnot))
        {
            if (!TryEnterKnot(startKnot))
            {
                TryEnterNextQueuedKnot();
            }
        }
        else
        {
            TryEnterNextQueuedKnot();
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        RefreshDialogueProgressFromCurrentState();
        ClearSpeakerVisuals();
        isPausedForPopup = false;
        dialogueClickTarget.interactable = true;
        AdvanceDialogue();
    }

    // unhooks click listener
    private void OnDestroy()
    {
        if (dialogueClickTarget != null)
        {
            dialogueClickTarget.onClick.RemoveListener(AdvanceDialogue);
        }

        StopLineReveal();
    }

    // starts dialogue when left knob is at correct channel
    private void HandleTrueChannelFound()
    {
        if (string.IsNullOrWhiteSpace(trueChannelFoundKnot))
        {
            return;
        }

        StartDialogueAtKnot(trueChannelFoundKnot);
    }

    private void HandleCorrectChannelSubmitted()
    {
        if (string.IsNullOrWhiteSpace(trueChannelFoundKnot))
        {
            return;
        }

        StartDialogueAtKnot(trueChannelFoundKnot);
    }

    private void HandleIncorrectChannelSubmitted()
    {
        if (string.IsNullOrWhiteSpace(incorrectChannelSubmittedKnot))
        {
            return;
        }

        StartDialogueAtKnot(incorrectChannelSubmittedKnot);
    }

    // starts dialogue when right knob is at correct tuning zone
    private void HandleRightKnobTuned()
    {
        if (string.IsNullOrWhiteSpace(rightKnobTunedKnot))
        {
            return;
        }

        StartDialogueAtKnot(rightKnobTunedKnot);
    }

    private void HandleIncorrectRightKnobSubmitted()
    {
        if (string.IsNullOrWhiteSpace(incorrectRightKnobSubmittedKnot))
        {
            return;
        }

        StartDialogueAtKnot(incorrectRightKnobSubmittedKnot);
    }

    // starts dialogue when lever is pulled
    private void HandleLeverPulled()
    {
        if (string.IsNullOrWhiteSpace(leverPulledKnot))
        {
            return;
        }

        StartDialogueAtKnot(leverPulledKnot);
    }

    // validates inspector 
    private bool ValidateSetup()
    {
        var isValid = true;

        if (compiledInkJson == null)
        {
            Debug.LogError("InkDialogue: compiledInkJson is not assigned.");
            isValid = false;
        }

        if (dialoguePanel == null)
        {
            Debug.LogError("InkDialogue: dialoguePanel is not assigned.");
            isValid = false;
        }

        if (dialogueText == null)
        {
            Debug.LogError("InkDialogue: dialogueText is not assigned.");
            isValid = false;
        }

        if (speakerNameText == null)
        {
            Debug.LogWarning("InkDialogue: speakerNameText is not assigned. Name label will be skipped.");
        }

        if (speakerPortraitImage == null)
        {
            Debug.LogWarning("InkDialogue: speakerPortraitImage is not assigned. Portrait will be skipped.");
        }

        if (dialogueClickTarget == null)
        {
            Debug.LogError("InkDialogue: dialogueClickTarget is not assigned.");
            isValid = false;
        }

        return isValid;
    }

    // validates references 
    private bool ValidateRuntimeSetup()
    {
        if (compiledInkJson == null || dialoguePanel == null || dialogueText == null || dialogueClickTarget == null)
        {
            Debug.LogError("InkDialogue: required runtime references are missing.");
            return false;
        }

        return true;
    }

    // advances to the next non-empty line
    public void AdvanceDialogue()
    {
        if (story == null || isPausedForPopup)
        {
            return;
        }

        if (isLineRevealing)
        {
            if (clickCompletesCurrentLine)
            {
                CompleteCurrentLine();
            }

            return;
        }

        while (story.canContinue)
        {
            var nextLine = story.Continue();
            if (TryHandleUiCommands(story.currentTags))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(nextLine))
            {
                continue;
            }

            currentSequenceLineIndex = Mathf.Min(currentSequenceLineIndex + 1, currentSequenceLineCount);
            UpdateDialogueProgressText();
            ApplySpeakerFromTags(story.currentTags);
            ShowLine(nextLine.Trim());
            return;
        }

        if (story.currentChoices.Count > 0) 
        {
            ShowChoices();
            return;
        }       

        /* if (TryEnterNextQueuedKnot())
        {
            AdvanceDialogue();
            return;
        }
        */

        dialogueText.text = string.Empty;
        ClearDialogueProgress();
        dialogueClickTarget.interactable = false;

        if (hidePanelWhenDone && dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    // starts a new dialogue sequence at the given knot
    public void StartDialogueAtKnot(string knotName)
    {
        if (string.IsNullOrWhiteSpace(knotName))
        {
            return;
        }

        if (!ValidateRuntimeSetup())
        {
            return;
        }

        if (story == null)
        {
            story = new Story(compiledInkJson.text);
            CacheQueuedKnots();
            BuildSpeakerLookup();
        }       

        isPausedForPopup = false;
        StopLineReveal();

        if (!TryEnterKnot(knotName))
        {
            return;
        }

        ClearSpeakerVisuals();
        RefreshDialogueProgressFromCurrentState();
        dialoguePanel.SetActive(true);
        dialogueClickTarget.interactable = true;
        AdvanceDialogue();
    }

    // builds speaker id -> portrait lookup table
    private void BuildSpeakerLookup()
    {
        speakerLookup.Clear();

        if (speakers == null)
        {
            return;
        }

        for (var i = 0; i < speakers.Length; i++)
        {
            var speaker = speakers[i];
            if (speaker == null || string.IsNullOrWhiteSpace(speaker.SpeakerId))
            {
                continue;
            }

            var key = speaker.SpeakerId.Trim().ToLowerInvariant();
            if (speakerLookup.ContainsKey(key))
            {
                Debug.LogWarning($"InkDialogue: duplicate speaker id '{speaker.SpeakerId}' ignored.");
                continue;
            }

            speakerLookup.Add(key, speaker);
        }
    }

    // applies speaker portrait 
    private void ApplySpeakerFromTags(List<string> tags)
    {
        var speakerId = ExtractSpeakerId(tags);
        if (string.IsNullOrWhiteSpace(speakerId))
        {
            return;
        }

        if (!speakerLookup.TryGetValue(speakerId.ToLowerInvariant(), out var speaker))
        {
            Debug.LogWarning($"InkDialogue: speaker id '{speakerId}' not found in Speakers list.");
            ClearSpeakerVisuals();
            return;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = speaker.DisplayName;
        }

        if (speakerPortraitImage != null)
        {
            speakerPortraitImage.sprite = speaker.Portrait;
            speakerPortraitImage.enabled = speaker.Portrait != null;
        }
    }

    // checks ink tags for ui commands
    private bool TryHandleUiCommands(List<string> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < tags.Count; i++)
        {
            var tag = tags[i];
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var trimmed = tag.Trim();
            if (!trimmed.StartsWith(UiTagPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var command = trimmed.Substring(UiTagPrefix.Length).Trim();
            if (string.Equals(command, DisplayNewspaperCommand, StringComparison.OrdinalIgnoreCase))
            {
                ShowNewspaperPopup();
                return true;
            }

            if (string.Equals(command, HideSubtitlesCommand, StringComparison.OrdinalIgnoreCase))
            {
                HideSubtitleText();
                continue;
            }
        }

        return false;
    }

    // hides translated subtitle text
    private void HideSubtitleText()
    {
        if (radioTutorialPuzzle == null)
        {
            Debug.LogWarning("InkDialogue: radioTutorialPuzzle is not assigned. Cannot hide subtitles.");
            return;
        }

        radioTutorialPuzzle.HideSubtitleText();
    }

    // opens newspaper popup and pauses dialogue 
    private void ShowNewspaperPopup()
    {
        if (newspaperPopup == null)
        {
            Debug.LogWarning("InkDialogue: newspaperPopup is not assigned.");
            return;
        }

        StopLineReveal();
        newspaperPopup.ShowDefault();
        if (!newspaperPopup.IsOpen)
        {
            Debug.LogWarning("InkDialogue: newspaper popup did not open.");
            return;
        }

        wasDialoguePanelVisibleBeforePopup = dialoguePanel != null && dialoguePanel.activeSelf;
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        isPausedForPopup = true;
        if (dialogueClickTarget != null)
        {
            dialogueClickTarget.interactable = false;
        }
    }

    // resumes dialogue when newspaper is closed
    private void HandleNewspaperClosed()
    {
        isPausedForPopup = false;

        var shouldRestoreDialoguePanel = wasDialoguePanelVisibleBeforePopup
            && story != null
            && (story.canContinue || queuedKnots.Count > 0);

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(shouldRestoreDialoguePanel);
        }


        if (dialogueClickTarget != null)
        {
            dialogueClickTarget.interactable = shouldRestoreDialoguePanel;
        }

        wasDialoguePanelVisibleBeforePopup = false;
        AdvanceDialogue();
    }

    // starts displaying a line with typewriter effect
    private void ShowLine(string line)
    {
        currentFullLine = line ?? string.Empty;
        StopLineReveal();

        if (dialogueText == null)
        {
            return;
        }

        if (charactersPerSecond <= 0f || string.IsNullOrEmpty(currentFullLine))
        {
            dialogueText.text = currentFullLine;
            isLineRevealing = false;
            return;
        }

        isLineRevealing = true;
        lineRevealCoroutine = StartCoroutine(RevealLineRoutine(currentFullLine));
    }

    // reveals one more character over time
    private System.Collections.IEnumerator RevealLineRoutine(string line)
    {
        dialogueText.text = string.Empty;
        var revealDelay = 1f / charactersPerSecond;
        StartDialogueVoice();

        for (var i = 1; i <= line.Length; i++)
        {
            dialogueText.text = line.Substring(0, i);
            if (i < line.Length)
            {
                yield return new WaitForSeconds(revealDelay);
            }
        }

        isLineRevealing = false;
        lineRevealCoroutine = null;
        StopDialogueVoice();
    }

    // instantly shows the full current line
    private void CompleteCurrentLine()
    {
        StopLineReveal();
        if (dialogueText != null)
        {
            dialogueText.text = currentFullLine;
        }
    }

    // stops any active line reveal coroutine
    private void StopLineReveal()
    {
        if (lineRevealCoroutine != null)
        {
            StopCoroutine(lineRevealCoroutine);
            lineRevealCoroutine = null;
        }

        isLineRevealing = false;
        StopDialogueVoice();
    }

    // starts dialogue voice while text is revealing
    private void StartDialogueVoice()
    {
        if (dialogueVoiceAudioSource == null)
        {
            return;
        }

        // apply inspector voice settings
        dialogueVoiceAudioSource.pitch = dialogueVoicePitch;

        if (!dialogueVoiceAudioSource.isPlaying)
        {
            dialogueVoiceAudioSource.Play();
        }
    }

    // stops dialogue voice when reveal is done
    private void StopDialogueVoice()
    {
        if (dialogueVoiceAudioSource == null)
        {
            return;
        }

        if (dialogueVoiceAudioSource.isPlaying)
        {
            dialogueVoiceAudioSource.Stop();
        }
    }

    // extracts speaker id from tag list
    private string ExtractSpeakerId(List<string> tags)
    {
        if (tags == null || tags.Count == 0)
        {
            return string.Empty;
        }

        for (var i = 0; i < tags.Count; i++)
        {
            var tag = tags[i];
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            var trimmed = tag.Trim();
            if (!trimmed.StartsWith(SpeakerTagPrefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return trimmed.Substring(SpeakerTagPrefix.Length).Trim();
        }

        return string.Empty;
    }

    private void ClearSpeakerVisuals()
    {
        if (speakerNameText != null)
        {
            speakerNameText.text = string.Empty;
        }

        if (speakerPortraitImage != null)
        {
            speakerPortraitImage.sprite = null;
            speakerPortraitImage.enabled = false;
        }
    }

    private void RefreshDialogueProgressFromCurrentState()
    {
        currentSequenceLineIndex = 0;
        currentSequenceLineCount = CountRemainingDisplayLines();
        UpdateDialogueProgressText();
    }

    private int CountRemainingDisplayLines()
    {
        if (story == null || compiledInkJson == null)
        {
            return 0;
        }

        try
        {
            var previewStory = new Story(compiledInkJson.text);
            previewStory.state.LoadJson(story.state.ToJson());

            var count = 0;
            while (previewStory.canContinue)
            {
                var nextLine = previewStory.Continue();
                if (string.IsNullOrWhiteSpace(nextLine))
                {
                    continue;
                }

                count++;
            }

            return count;
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"InkDialogue: failed to count dialogue lines. {ex.Message}");
            return 0;
        }
    }

    private void UpdateDialogueProgressText()
    {
        if (dialogueCounterText == null)
        {
            return;
        }

        if (currentSequenceLineCount <= 0 || currentSequenceLineIndex <= 0)
        {
            dialogueCounterText.text = string.Empty;
            return;
        }

        dialogueCounterText.text = $"{currentSequenceLineIndex}/{currentSequenceLineCount}";
    }

    private void ClearDialogueProgress()
    {
        currentSequenceLineIndex = 0;
        currentSequenceLineCount = 0;
        UpdateDialogueProgressText();
    }

    private void CacheQueuedKnots()
    {
        queuedKnots.Clear();

        if (nextKnots == null)
        {
            return;
        }

        for (var i = 0; i < nextKnots.Length; i++)
        {
            var knot = nextKnots[i];
            if (string.IsNullOrWhiteSpace(knot))
            {
                continue;
            }

            queuedKnots.Enqueue(knot.Trim());
        }
    }

    // enters the next queued knot
     private bool TryEnterNextQueuedKnot()
    {
        while (queuedKnots.Count > 0)
        {
            if (TryEnterKnot(queuedKnots.Dequeue()))
            {
                return true;
            }
        }

        return false;
    }
        

    // tries to jump to a knot path in current story
    private bool TryEnterKnot(string knotName)
    {
        if (string.IsNullOrWhiteSpace(knotName))
        {
            return false;
        }

        try
        {
            story.ChoosePathString(knotName);
            return true;
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"InkDialogue: failed to enter knot '{knotName}'. {ex.Message}");
            return false;
        }
    }

        private void ShowChoices()
    {
        dialogueClickTarget.interactable = false;

        // Clear old buttons
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < story.currentChoices.Count; i++)
        {
            var choice = story.currentChoices[i];
            var button = Instantiate(choiceButtonPrefab, choiceContainer);

            button.GetComponentInChildren<TMP_Text>().text = choice.text;
            int choiceIndex = i;

            button.onClick.AddListener(() =>
            {
                ChooseInkChoice(choiceIndex);
            });
        }
    }
        private void ChooseInkChoice(int index)
    {
        foreach (Transform child in choiceContainer)
        {
            Destroy(child.gameObject);
        }

        dialogueClickTarget.interactable = true;

        story.ChooseChoiceIndex(index);
        RefreshDialogueProgressFromCurrentState();
        AdvanceDialogue();
    }

}
