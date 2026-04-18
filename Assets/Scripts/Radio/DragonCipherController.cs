using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DragonCipher;

/// <summary>
/// Attach to a GameObject in the Dragon Cipher scene.
/// Each word starts in its cipher color. The clue word ("sending") starts red because
/// it is already decoded. Typing the correct answer turns ALL words red.
///
/// Scene setup required:
///   - One TMP label per word (assign in Inspector via WordLabels list, in sentence order)
///   - A TMP_InputField for the player to type their answer
///   - useDragonMessage: set true for the dragon/airborne variant, false for siren/waterborne
/// </summary>
public class DragonCipherController : MonoBehaviour
{
    [Header("Message Selection")]
    [Tooltip("True = dragon/airborne message. False = siren/waterborne message. Set this from your game manager before the puzzle starts.")]
    public bool useDragonMessage = true;

    [Header("Word Display")]
    [Tooltip("Parent panel containing all word labels. Disabled at scene start, activated when the puzzle begins.")]
    public GameObject messagePanel;
    [Tooltip("One TMP label per word, in sentence order (10 total).")]
    public List<TextMeshProUGUI> wordLabels;

    [Header("Hint Button")]
    [Tooltip("Separate hint button GameObject. Activated when the puzzle starts, deactivated when solved.")]
    public GameObject hintButton;

    [Header("Answer Input")]
    [Tooltip("The TMP InputField where the player types their decoded answer.")]
    public TMP_InputField answerInput;

    [Header("Feedback")]
    [Tooltip("Optional: displays a short message when the player submits a wrong answer.")]
    public TextMeshProUGUI feedbackLabel;

    [Header("Ink Dialogue")]
    [SerializeField] private InkDialogue inkDialogue;

    // ── Internal State ────────────────────────────────────────────────────────
    private List<DragonCipherWord> activeWords;
    private string correctAnswer;
    private bool solved  = false;
    private bool started = false;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────
    void Start()
    {
        // Use the siren variant only if the player explicitly chose the dragon option in Level 2.
        // Everything else (dragon, unicorn, or no choice yet) defaults to the dragon message.
        useDragonMessage = GameState.Level2CreatureChoice != GameState.CreatureChoice.Dragon;

        activeWords   = useDragonMessage ? DragonCipherPuzzle.DragonWords : DragonCipherPuzzle.SirenWords;
        correctAnswer = useDragonMessage ? DragonCipherPuzzle.DragonAnswer : DragonCipherPuzzle.SirenAnswer;

        if (answerInput != null)
            answerInput.onSubmit.AddListener(OnAnswerSubmit);
    }

    void Update()
    {
        if (solved)  return;
        if (!started) return;
        if (inkDialogue != null && inkDialogue.IsDialogueActive) return;
    }

    // ── Display ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Renders each word in its cipher starting color.
    /// The clue word ("sending") has StartingColor = Red so it appears red from the start.
    /// All other words appear in their unique cipher color (Orange, Yellow, Indigo, etc.).
    /// </summary>
    void DisplayWords()
    {
        for (int i = 0; i < activeWords.Count; i++)
        {
            if (i >= wordLabels.Count) break;

            var word = activeWords[i];
            // Clue word shows the decoded original; all others show the encoded text
            wordLabels[i].text  = word.IsClueWord ? word.Original : word.Encoded;
            // Each word's starting color is defined in DragonCipherPuzzle:
            //   clue word → Red (already "decoded"), others → their cipher color
            wordLabels[i].color = word.StartingColor;
        }
    }

    /// <summary>
    /// Turns every word label to red and shows the decoded original text.
    /// Red = +0 shift, so all words revert to their unencoded form.
    /// </summary>
    void TurnAllWordsRed()
    {
        for (int i = 0; i < activeWords.Count; i++)
        {
            if (i >= wordLabels.Count) break;
            wordLabels[i].color = DragonCipherPuzzle.Red;
            wordLabels[i].text  = activeWords[i].Original;
        }
    }

    /// <summary>
    /// Sets a word's display color and updates its text to match the shift.
    /// Red (+0) shows the decoded original; any other color shows the encoded text.
    /// Call this from any interactive color-change UI.
    /// </summary>
    public void SetWordColor(int index, Color color)
    {
        if (index < 0 || index >= activeWords.Count || index >= wordLabels.Count) return;
        wordLabels[index].color = color;
        wordLabels[index].text  = color == DragonCipherPuzzle.Red
            ? activeWords[index].Original
            : activeWords[index].Encoded;
    }

    // ── Answer Checking ───────────────────────────────────────────────────────
    void OnAnswerSubmit(string input)
    {
        if (solved) return;
        CheckAnswer(input);
    }

    void CheckAnswer(string input)
    {
        string typed = input.Trim().ToLower();

        if (typed == correctAnswer.ToLower())
        {
            solved = true;
            TurnAllWordsRed();
            if (hintButton != null) hintButton.SetActive(false);
            if (answerInput != null) answerInput.gameObject.SetActive(false);
            if (feedbackLabel != null) feedbackLabel.text = "";
            if (inkDialogue != null)
            {
                if (useDragonMessage)
                {
                    inkDialogue.StartDialogueAtKnot("Dragon_Solve");
                } else {
                    inkDialogue.StartDialogueAtKnot("Siren_Solve");}
            }
        }
        else
        {
            if (feedbackLabel != null) feedbackLabel.text = "Not quite. Try again.";
            if (inkDialogue != null) inkDialogue.StartDialogueAtKnot("Dragon_Wrong");
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this from your puzzle manager when it's time to show and start the cipher.
    /// </summary>
    public void StartPuzzle()
    {
        gameObject.SetActive(true);
        messagePanel?.SetActive(true);
        DisplayWords();
        if (answerInput != null)
        {
            answerInput.gameObject.SetActive(true);
            answerInput.text = "";
            answerInput.interactable = true;
        }
        if (hintButton != null) hintButton.SetActive(false);
        if (feedbackLabel != null) feedbackLabel.text = "";
        if (inkDialogue != null)
            inkDialogue.StartDialogueAtKnot("Dragon_Start");
        started = true;
    }

    public void NotifyDialogueStarted()
    {
        if (hintButton != null) hintButton.SetActive(false);
    }

    public void NotifyDialogueFinished()
    {
        if (solved) { hintButton?.SetActive(false); return; }
        hintButton?.SetActive(started);
    }

    /// <summary>
    /// Wire this to a Button on the message panel. Triggers an Ink knot explaining the cipher.
    /// Only fires while the puzzle is active and not yet solved.
    /// </summary>
    public void OnMessageClicked()
    {
        if (!started || solved) return;
        if (inkDialogue != null && inkDialogue.IsDialogueActive) return;
        inkDialogue?.StartDialogueAtKnot("Dragon_Hint");
    }

    /// <summary>
    /// Call this to hide the cipher without solving it.
    /// </summary>
    public void HidePuzzle()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Call this from an Ink dialogue event to hide the puzzle after the solve dialogue ends.
    /// </summary>
    public void HideAfterDialogue()
    {
        HidePuzzle();
    }
}
