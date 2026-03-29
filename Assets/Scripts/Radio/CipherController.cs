using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
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

    [Tooltip("How far below each word label the horn sits (negative = below).")]
    public float hornYOffset = -40f;

    [Tooltip("Seconds between input repeats when key is held.")]
    public float inputRepeatDelay = 0.15f;

    [Header("Solved State")]
    [Tooltip("Object to activate when the puzzle is solved (e.g. a victory panel).")]
    public GameObject solvedPanel;

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

    // ROYGBIV colors mapped for TMP display
    private static readonly Dictionary<CipherColor, Color> ColorMap = new Dictionary<CipherColor, Color>
    {
        { CipherColor.Red,    new Color(0.93f, 0.18f, 0.18f) },
        { CipherColor.Orange, new Color(1.00f, 0.55f, 0.10f) },
        { CipherColor.Yellow, new Color(0.95f, 0.90f, 0.10f) },
        { CipherColor.Green,  new Color(0.18f, 0.80f, 0.25f) },
        { CipherColor.Blue,   new Color(0.20f, 0.55f, 1.00f) },
        { CipherColor.Indigo, new Color(0.35f, 0.20f, 0.85f) },
        { CipherColor.Violet, new Color(0.70f, 0.25f, 0.90f) },
    };

    // ── Unity Lifecycle ─────────────────────────────────────────────────
    void Start()
    {
        StartPuzzle();

        foreach (var word in UnicornCipherPuzzle.Words)
            selectedColors.Add(word.StartingColor);

        if (solvedPanel != null)
            solvedPanel.SetActive(false);

        RefreshAllWords();
        MoveCursorTo(0);
        inkDialogue.StartDialogueAtKnot("Unicorn_Start");
    }

    void Update()
    {
    if (solved) return;
    if (!started) return;
    if (inkDialogue != null && inkDialogue.IsDialogueActive) return;
    HandleInput();
    }

    // ── Input ────────────────────────────────────────────────────────────
    void HandleInput()
    {
        var kb = Keyboard.current;

        // Fresh key presses
        if (kb.aKey.wasPressedThisFrame)      { heldTimer = 0f; lastKey = "A"; MoveCursor(-1); }
        else if (kb.dKey.wasPressedThisFrame) { heldTimer = 0f; lastKey = "D"; MoveCursor(1);  }
        else if (kb.wKey.wasPressedThisFrame) { heldTimer = 0f; lastKey = "W"; CycleColor(-1); }
        else if (kb.sKey.wasPressedThisFrame) { heldTimer = 0f; lastKey = "S"; CycleColor(1);  }

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
        UpdateColorLabel();
        CheckSolved();

        //Check if first word solved 
            if (!first)
        {
           if (selectedColors[cursorIndex] == CipherColor.Blue)
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
        if (messagePanel != null) messagePanel.SetActive(true);
        if (hornSprite != null)   hornSprite.SetActive(true);
        started = true;
    }

    /// <summary>
    /// Call this to hide the cipher without solving it (e.g. returning to a menu).
    /// </summary>
    public void HidePuzzle()
    {
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
        if (solvedPanel != null)
            solvedPanel.SetActive(true);
        
        inkDialogue.StartDialogueAtKnot("Unicorn_Solve");
        Debug.Log("Puzzle solved! The message is: We have formed an alliance with the dragons and sirens");
        messagePanel.SetActive(false);
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
}