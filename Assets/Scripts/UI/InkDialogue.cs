using System;
using System.Collections.Generic;
using Ink.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
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
    [SerializeField] private string trueChannelFoundKnot;
    [SerializeField] private string rightKnobTunedKnot;
    [SerializeField] private string leverPulledKnot;

    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private Image speakerPortraitImage;
    [SerializeField] private Button dialogueClickTarget;
    [SerializeField] private bool hidePanelWhenDone = true;

    [Header("Speakers")]
    [SerializeField] private SpeakerVisual[] speakers;

    private Story story;
    private readonly Queue<string> queuedKnots = new Queue<string>();
    private readonly Dictionary<string, SpeakerVisual> speakerLookup = new Dictionary<string, SpeakerVisual>();

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
            radioTutorialPuzzle.TrueChannelFound += HandleTrueChannelFound;
            radioTutorialPuzzle.RightKnobTuned += HandleRightKnobTuned;
            radioTutorialPuzzle.LeverPulled += HandleLeverPulled;
        }
    }

    // unhooks optional puzzle events
    private void OnDisable()
    {
        if (radioTutorialPuzzle != null)
        {
            radioTutorialPuzzle.TrueChannelFound -= HandleTrueChannelFound;
            radioTutorialPuzzle.RightKnobTuned -= HandleRightKnobTuned;
            radioTutorialPuzzle.LeverPulled -= HandleLeverPulled;
        }
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

        ClearSpeakerVisuals();
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

    // starts dialogue when right knob is at correct tuning zone
    private void HandleRightKnobTuned()
    {
        if (string.IsNullOrWhiteSpace(rightKnobTunedKnot))
        {
            return;
        }

        StartDialogueAtKnot(rightKnobTunedKnot);
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
        if (story == null)
        {
            return;
        }

        while (story.canContinue)
        {
            var nextLine = story.Continue();
            if (string.IsNullOrWhiteSpace(nextLine))
            {
                continue;
            }

            ApplySpeakerFromTags(story.currentTags);
            dialogueText.text = nextLine.Trim();
            return;
        }

        if (TryEnterNextQueuedKnot())
        {
            AdvanceDialogue();
            return;
        }

        dialogueText.text = string.Empty;
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

        story = new Story(compiledInkJson.text);
        queuedKnots.Clear();
        BuildSpeakerLookup();

        if (!TryEnterKnot(knotName))
        {
            return;
        }

        ClearSpeakerVisuals();
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
}
