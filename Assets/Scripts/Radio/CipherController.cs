using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;
using UnicornCipher;

/// <summary>
/// Attach this to a GameObject in your scene.
/// Displays the Unicorn Cipher puzzle and handles player input.
///
/// Scene setup required:
///   - One TMP text object per word (assign in Inspector via WordLabels list)
///   - A RectTransform for the unicorn horn sprite (HornCursor) — it will slide under the selected word
/// </summary>
public class CipherController : MonoBehaviour
{
    [Tooltip("Optional: assign a panel to show/hide with the puzzle.")]
    public GameObject messagePanel;

    [Header("Intercept Log")]
    [Tooltip("Optional intelligence page popup used as the container for this puzzle.")]
    [SerializeField] private NewspaperPopup intelligencePopup;
    [Tooltip("Optional close button on the intelligence page. Hidden while the puzzle is active.")]
    [SerializeField] private Button intelligenceCloseButton;

    [Header("Word Display")]
    [Tooltip("One TMP label per word, in sentence order (10 total).")]
    public List<TextMeshProUGUI> wordLabels;

    [Header("Color Indicator")]
    [Tooltip("Optional: displays the currently selected color name for the active word.")]
    public TextMeshProUGUI colorLabel;

    [Header("Horn Cursor")]
    [Tooltip("RectTransform of the unicorn horn sprite. Moves under the active word.")]
    public RectTransform hornCursor;
    [Tooltip("Optional: assign the horn sprite here to show/hide it with the puzzle.")]
    public GameObject hornSprite;
    [Tooltip("Image component on the horn cursor (for sprite swapping on W/S press).")]
    public Image hornImage;
    [Tooltip("Sprite shown when horn is idle.")]
    public Sprite hornNormalSprite;
    [Tooltip("Sprite shown while W or S is held (color cycling).")]
    public Sprite hornPressedSprite;
    [Tooltip("How far below each word label the horn sits (negative = below).")]
    public float hornYOffset = -40f;

    [Tooltip("Seconds between input repeats when key is held.")]
    public float inputRepeatDelay = 0.15f;

    [Header("Word Glow Effect")]
    [Tooltip("A single glow Image that sits above the horn and flashes when the color changes.")]
    public Image glowImage;
    [Tooltip("How far above the horn cursor the glow sits.")]
    public float glowYOffset = 20f;
    [Tooltip("How long the glow takes to fade in then out (seconds).")]
    public float glowDuration = 0.35f;

    [Header("Audio")]
    [Tooltip("AudioSource used to play all cipher sounds.")]
    public AudioSource audioSource;
    [Tooltip("One clip per color, indexed by CipherColor enum (0=Red, 1=Orange, 2=Yellow, 3=Green, 4=Blue, 5=Indigo, 6=Violet).")]
    public AudioClip[] colorSounds = new AudioClip[7];
    [Tooltip("Plays when the selected word's color becomes correct.")]
    public AudioClip wordCorrectSound;
    [Tooltip("Plays when the entire sentence is solved.")]
    public AudioClip sentenceCorrectSound;

    [Header("Ink Dialogue")]
    [SerializeField] private InkDialogue inkDialogue;

    // ── Internal State ──────────────────────────────────────────────────
    private List<CipherColor> selectedColors = new List<CipherColor>();
    private int cursorIndex = 0;
    private bool solved = false;
    private bool started = false;

    private float heldTimer = 0f;
    private string lastKey = "";
    private bool first = false;
    private bool reopenInterceptLogAfterDialogue;
    private bool pendingSolvedDialogueOnClose;

    // ROYGBIV colors mapped for TMP display
    private static readonly Dictionary<CipherColor, Color> ColorMap = new Dictionary<CipherColor, Color>
    {
        { CipherColor.Red,    new Color(0.96f, 0.62f, 0.67f) },  // pastel rose
        { CipherColor.Orange, new Color(1.00f, 0.78f, 0.62f) },  // pastel peach
        { CipherColor.Yellow, new Color(0.97f, 0.95f, 0.72f) },  // pastel lemon
        { CipherColor.Green,  new Color(0.62f, 0.92f, 0.80f) },  // pastel mint
        { CipherColor.Blue,   new Color(0.62f, 0.78f, 0.98f) },  // pastel periwinkle
        { CipherColor.Indigo, new Color(0.68f, 0.62f, 0.95f) },  // pastel lavender
        { CipherColor.Violet, new Color(0.82f, 0.65f, 0.97f) },  // pastel lilac
    };

    // ── Unity Lifecycle ─────────────────────────────────────────────────
    void OnEnable()
    {
        if (intelligencePopup != null)
        {
            intelligencePopup.Closed += HandleIntelligencePopupClosed;
        }
    }

    void OnDisable()
    {
        if (intelligencePopup != null)
        {
            intelligencePopup.Closed -= HandleIntelligencePopupClosed;
        }
    }

    void Start()
    {
        foreach (var word in UnicornCipherPuzzle.Words)
            selectedColors.Add(word.StartingColor);

        RefreshAllWords();
        StartCoroutine(PlaceHornAfterLayout());
        if (!started)
        {
            StartPuzzle();
        }
    }

    void Update()
    {
        if (solved) return;
        if (!started) return;
        HandleInput();
    }

    // ── Input ────────────────────────────────────────────────────────────
    void HandleInput()
    {
        var kb = Keyboard.current;

        // Fresh key presses
        if (kb.aKey.wasPressedThisFrame)      { heldTimer = 0f; lastKey = "A"; MoveCursor(-1); }
        else if (kb.dKey.wasPressedThisFrame) { heldTimer = 0f; lastKey = "D"; MoveCursor(1);  }
        else if (kb.wKey.wasPressedThisFrame) { heldTimer = 0f; lastKey = "W"; CycleColor(-1); SetHornSprite(true); }
        else if (kb.sKey.wasPressedThisFrame) { heldTimer = 0f; lastKey = "S"; CycleColor(1);  SetHornSprite(true); }

        // Key-held repeat
        if (lastKey != "" && IsKeyHeld(lastKey))
        {
            heldTimer += Time.deltaTime;
            if (heldTimer >= inputRepeatDelay)
            {
                heldTimer = 0f;
                switch (lastKey)
                {
                    case "A": MoveCursor(-1); break;
                    case "D": MoveCursor(1);  break;
                    case "W": CycleColor(-1); break;
                    case "S": CycleColor(1);  break;
                }
            }
        }

        // Swap back to normal sprite when W/S released
        if ((lastKey == "W" || lastKey == "S") && !IsKeyHeld(lastKey))
            SetHornSprite(false);

        // Clear lastKey when released
        if (lastKey != "" && !IsKeyHeld(lastKey))
            lastKey = "";
    }

    // ── Cursor Movement ──────────────────────────────────────────────────
    void MoveCursor(int dir)
    {
        int count = UnicornCipherPuzzle.Words.Count;
        cursorIndex = (cursorIndex + dir + count) % count;
        MoveCursorTo(cursorIndex);
    }

    void MoveCursorTo(int idx)
    {
        cursorIndex = idx;

        if (hornCursor != null && wordLabels != null && idx < wordLabels.Count)
        {
            var target = wordLabels[idx].rectTransform;
            hornCursor.position = new Vector3(
                target.position.x,
                target.position.y + hornYOffset,
                hornCursor.position.z
            );
        }

        UpdateColorLabel();
    }

    // ── Horn Sprite Swap ─────────────────────────────────────────────────
    void SetHornSprite(bool pressed)
    {
        if (hornImage == null) return;
        var sprite = pressed ? hornPressedSprite : hornNormalSprite;
        if (sprite != null) hornImage.sprite = sprite;
    }

    // ── Color Cycling ────────────────────────────────────────────────────
    void CycleColor(int dir)
    {
        if (UnicornCipherPuzzle.Words[cursorIndex].IsClueWord) {
            inkDialogue.StartDialogueAtKnot("Unicorn_Hint");
            return;
        }

        int current = (int)selectedColors[cursorIndex];
        int next = (current + dir + 7) % 7;
        selectedColors[cursorIndex] = (CipherColor)next;

        RefreshWord(cursorIndex);

        // Color-change sound
        PlayColorSound(selectedColors[cursorIndex]);

        // Word-correct sound
        if (UnicornCipherPuzzle.Words[cursorIndex].IsSolved(selectedColors[cursorIndex])
            && wordCorrectSound != null && audioSource != null)
            audioSource.PlayOneShot(wordCorrectSound);

        if (glowImage != null && hornCursor != null)
        {
            glowImage.rectTransform.position = new Vector3(
                hornCursor.position.x,
                hornCursor.position.y + glowYOffset,
                hornCursor.position.z
            );
            StopCoroutine("FlashGlow");
            StartCoroutine("FlashGlow");
        }

        UpdateColorLabel();
        CheckSolved();

        //Check if first word solved
        if (!first)
        {
            if (selectedColors[cursorIndex] == UnicornCipherPuzzle.Words[cursorIndex].CorrectColor)
            {
                first = true;
                inkDialogue.StartDialogueAtKnot("Unicorn_First");
            }
        }
    }

    // ── Display Refresh ──────────────────────────────────────────────────
    void RefreshAllWords()
    {
        for (int i = 0; i < UnicornCipherPuzzle.Words.Count; i++)
            RefreshWord(i);
    }

    void RefreshWord(int idx)
    {
        if (wordLabels == null || idx >= wordLabels.Count) return;

        var word  = UnicornCipherPuzzle.Words[idx];
        var color = selectedColors[idx];

        wordLabels[idx].text  = word.GetEncoded(color);
        wordLabels[idx].color = ColorMap[color];
    }

    IEnumerator PlaceHornAfterLayout()
    {
        yield return null; // wait one frame for Canvas layout to finalize
        MoveCursorTo(0);
    }

    IEnumerator FlashGlow()
    {
        float half = glowDuration * 0.5f;
        // Fade in
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            var c = glowImage.color;
            c.a = t / half;
            glowImage.color = c;
            yield return null;
        }
        // Fade out
        for (float t = 0; t < half; t += Time.deltaTime)
        {
            var c = glowImage.color;
            c.a = 1f - t / half;
            glowImage.color = c;
            yield return null;
        }
        var final = glowImage.color;
        final.a = 0f;
        glowImage.color = final;
    }

    void UpdateColorLabel()
    {
        if (colorLabel == null) return;
        var color = selectedColors[cursorIndex];
        colorLabel.text  = color.ToString();
        colorLabel.color = ColorMap[color];
    }

    // ── Public API ───────────────────────────────────────────────────────

    /// <summary>
    /// Call this from your puzzle manager when it's time to show and start the cipher.
    /// </summary>
    public void StartPuzzle()
    {
        gameObject.SetActive(true);
        if (intelligencePopup != null) intelligencePopup.ShowExistingText();
        if (messagePanel != null) messagePanel.SetActive(true);
        if (hornSprite != null)   hornSprite.SetActive(true);
        SetIntelligenceCloseButtonVisible(true);
        started = true;
    }

    /// <summary>
    /// Call this to hide the cipher without solving it (e.g. returning to a menu).
    /// </summary>
    public void HidePuzzle()
    {
        if (messagePanel != null) messagePanel.SetActive(false);
        if (hornSprite != null)   hornSprite.SetActive(false);
        if (intelligencePopup != null) intelligencePopup.Hide();
        SetIntelligenceCloseButtonVisible(true);
        gameObject.SetActive(false);
    }

    // ── Win Condition ────────────────────────────────────────────────────
    void CheckSolved()
    {
        for (int i = 0; i < UnicornCipherPuzzle.Words.Count; i++)
        {
            if (!UnicornCipherPuzzle.Words[i].IsSolved(selectedColors[i]))
                return;
        }

        solved = true;
        if (hornSprite != null) hornSprite.SetActive(false);
        if (sentenceCorrectSound != null && audioSource != null)
            audioSource.PlayOneShot(sentenceCorrectSound);
        pendingSolvedDialogueOnClose = intelligencePopup != null && intelligencePopup.IsOpen;
        if (!pendingSolvedDialogueOnClose)
        {
            inkDialogue.StartDialogueAtKnot("Unicorn_Solve");
        }
        Debug.Log("Puzzle solved! The message is: We have formed an alliance with the dragons and sirens");
    }

    /// <summary>
    /// Call this (e.g. from an Ink dialogue event or UI button) to hide the puzzle after the solve dialogue ends.
    /// </summary>
    public void HideAfterDialogue()
    {
        HidePuzzle();
    }

    public void NotifyDialogueStarted()
    {
        if (intelligencePopup == null || !intelligencePopup.IsOpen)
        {
            return;
        }

        intelligencePopup.Hide();
        reopenInterceptLogAfterDialogue = true;
    }

    public void NotifyDialogueFinished()
    {
        if (!reopenInterceptLogAfterDialogue || intelligencePopup == null || intelligencePopup.IsOpen)
        {
            return;
        }

        reopenInterceptLogAfterDialogue = false;
        intelligencePopup.ShowExistingText();

        if (messagePanel != null)
        {
            messagePanel.SetActive(true);
        }

        if (hornSprite != null)
        {
            hornSprite.SetActive(!solved);
        }
    }

    // ── Audio ────────────────────────────────────────────────────────────
    void PlayColorSound(CipherColor color)
    {
        if (audioSource == null) return;
        int idx = (int)color;
        if (colorSounds != null && idx < colorSounds.Length && colorSounds[idx] != null)
            audioSource.PlayOneShot(colorSounds[idx]);
    }

    // ── Helpers ──────────────────────────────────────────────────────────
    bool IsKeyHeld(string k)
    {
        var kb = Keyboard.current;
        switch (k)
        {
            case "A": return kb.aKey.isPressed;
            case "D": return kb.dKey.isPressed;
            case "W": return kb.wKey.isPressed;
            case "S": return kb.sKey.isPressed;
            default:  return false;
        }
    }

    void SetIntelligenceCloseButtonVisible(bool visible)
    {
        if (intelligenceCloseButton == null)
        {
            return;
        }

        intelligenceCloseButton.gameObject.SetActive(visible);
    }

    void HandleIntelligencePopupClosed()
    {
        if (!pendingSolvedDialogueOnClose)
        {
            return;
        }

        pendingSolvedDialogueOnClose = false;
        inkDialogue.StartDialogueAtKnot("Unicorn_Solve");
    }
}
