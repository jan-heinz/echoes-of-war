using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// controller for radio interaction
// responsibilities:
//  1. opens/closes close-up mode
//  2. tracks left/right knob angles
//  3. rotates active knob with keyboard input
//  4. keeps world and close-up visuals in sync
//  5. displays knob-change events for effects
public class RadioController : MonoBehaviour
{
   
    // notification hook whenever a knob changes
    // when player turns a knob, RadioController sends a KnobChanged and which knob moved (including new angle/value)
    // this is so other systems such as audio, puzzle logic, etc can react without being built into this controller
    public event Action<RadioKnobId, float, float> KnobChanged;
    
    [Header("Knob Pivots")]
    [SerializeField] private Transform leftKnobPivot;
    [SerializeField] private Transform rightKnobPivot;

    [Header("Close-Up UI")]
    [SerializeField] private GameObject closeUpPanel;
    [SerializeField] private Button closeUpBackButton;
    [SerializeField] private Transform leftCloseUpKnobPivot;
    [SerializeField] private Transform rightCloseUpKnobPivot;

    [Header("Close-Up Selection")]
    [SerializeField] private Outline leftCloseUpKnobOutline;
    [SerializeField] private Outline rightCloseUpKnobOutline;

    [Header("Input")]
    [SerializeField] private Key turnLeftKey = Key.A;
    [SerializeField] private Key turnRightKey = Key.D;
    [SerializeField] private Key exitCloseUpKey = Key.Escape;
    [SerializeField] private float turnSpeedDegreesPerSecond = 120f;

    [Header("Default Knob Values")]
    [SerializeField] private float leftStartAngleDegrees = 0f;
    [SerializeField] private float rightStartAngleDegrees = 0f;

    // runtime angle state
    private float leftAngleDegrees;
    private float rightAngleDegrees;

    // close-up runtime state
    private bool isCloseUpOpen;
    private RadioKnobId activeKnob = RadioKnobId.Left;

    // lets other scripts query close-up state
    public bool IsCloseUpOpen => isCloseUpOpen;

    // lets other scripts query which knob is currently active
    public RadioKnobId ActiveKnob => activeKnob;

    private void Awake()
    {
        if (closeUpBackButton != null)
        {
            closeUpBackButton.onClick.AddListener(CloseCloseUp);
        }
    }

    private void Start()
    {
        // reset knob values for each scene load
        leftAngleDegrees = NormalizeAngle(leftStartAngleDegrees);
        rightAngleDegrees = NormalizeAngle(rightStartAngleDegrees);

        // close-up should start closed
        isCloseUpOpen = false;
        if (closeUpPanel != null)
        {
            closeUpPanel.SetActive(false);
        }

        // apply starting angles to all pivots
        SyncAllVisuals();

        // close-up starts closed
        // selection outline should be off
        UpdateCloseUpSelectionVisuals();
    }

    private void Update()
    {
        // only process turn input while in close-up mode
        if (!isCloseUpOpen)
        {
            return;
        }

        // escape key exits close-up
        if (IsKeyDown(exitCloseUpKey))
        {
            CloseCloseUp();
            return;
        }

        // read hold input for smooth rotation
        var turnDirection = 0f;
        if (IsKeyHeld(turnLeftKey))
        {
            turnDirection -= 1f;
        }
        if (IsKeyHeld(turnRightKey))
        {
            turnDirection += 1f;
        }

        // if no input this frame
        // skip rotation
        if (Mathf.Approximately(turnDirection, 0f))
        {
            return;
        }

        // convert input into degrees for this frame
        var angleDelta = turnDirection * turnSpeedDegreesPerSecond * Time.deltaTime;
        RotateActiveKnob(angleDelta);
    }

    private void OnDestroy()
    {
        // unhook back button listener 
        if (closeUpBackButton != null)
        {
            closeUpBackButton.onClick.RemoveListener(CloseCloseUp);
        }
    }

    // opens close-up mode and sets the active knob
    public void OpenCloseUp(RadioKnobId knobId)
    {
        // close-up panel is required 
        if (closeUpPanel == null)
        {
            Debug.LogError("RadioPuzzleController: closeUpPanel is not assigned");
            return;
        }

        // set active knob from whichever target was clicked
        activeKnob = knobId;

        // show close-up and enable input loop
        isCloseUpOpen = true;
        closeUpPanel.SetActive(true);

        // keep both world and close-up knob visuals in sync
        SyncAllVisuals();

        // highlight whichever knob is active in close-up
        UpdateCloseUpSelectionVisuals();
    }

    // closes close-up mode
    // esc key and back button both work
    public void CloseCloseUp()
    {
        isCloseUpOpen = false;

        if (closeUpPanel != null)
        {
            closeUpPanel.SetActive(false);
        }

        // close-up is no longer visible
        // selection outline should be off
        UpdateCloseUpSelectionVisuals();
    }

    // returns current angle for a specific knob
    public float GetKnobAngle(RadioKnobId knobId)
    {
        return knobId == RadioKnobId.Left ? leftAngleDegrees : rightAngleDegrees;
    }

    // returns normalized knob value [0, 1)
    public float GetKnobNormalized(RadioKnobId knobId)
    {
        return GetKnobAngle(knobId) / 360f;
    }

    // helper to reset knobs
    public void ResetKnobsToDefaults()
    {
        leftAngleDegrees = NormalizeAngle(leftStartAngleDegrees);
        rightAngleDegrees = NormalizeAngle(rightStartAngleDegrees);
        SyncAllVisuals();
    }

    // rotates whichever knob is currently active
    private void RotateActiveKnob(float deltaDegrees)
    {
        var currentAngle = GetKnobAngle(activeKnob);
        var nextAngle = NormalizeAngle(currentAngle + deltaDegrees);

        if (Mathf.Approximately(currentAngle, nextAngle))
        {
            return;
        }

        // update internal state for active knob
        SetKnobAngle(activeKnob, nextAngle);
        
        ApplyKnobVisual(activeKnob, nextAngle);

        // publish event for any future radio effects systems
        KnobChanged?.Invoke(activeKnob, nextAngle, nextAngle / 360f);
    }

    // writes angle state by knob id
    private void SetKnobAngle(RadioKnobId knobId, float angleDegrees)
    {
        if (knobId == RadioKnobId.Left)
        {
            leftAngleDegrees = angleDegrees;
            return;
        }

        rightAngleDegrees = angleDegrees;
    }

    // applies both knob angles to both views
    private void SyncAllVisuals()
    {
        ApplyKnobVisual(RadioKnobId.Left, leftAngleDegrees);
        ApplyKnobVisual(RadioKnobId.Right, rightAngleDegrees);
    }

    // applies one knob angle to world 
    private void ApplyKnobVisual(RadioKnobId knobId, float angleDegrees)
    {
        if (knobId == RadioKnobId.Left)
        {
            SetLocalZRotation(leftKnobPivot, angleDegrees);
            SetLocalZRotation(leftCloseUpKnobPivot, angleDegrees);
            return;
        }

        SetLocalZRotation(rightKnobPivot, angleDegrees);
        SetLocalZRotation(rightCloseUpKnobPivot, angleDegrees);
    }

    // applies z-axis local rotation while preserving x/y
    private static void SetLocalZRotation(Transform target, float angleDegrees)
    {
        if (target == null)
        {
            return;
        }

        var euler = target.localEulerAngles;
        euler.z = angleDegrees;
        target.localEulerAngles = euler;
    }

    // wraps angle into [0, 360)
    private static float NormalizeAngle(float angleDegrees)
    {
        return Mathf.Repeat(angleDegrees, 360f);
    }

    // toggles an outline so player can tell which knob is currently active
    private void UpdateCloseUpSelectionVisuals()
    {
        if (leftCloseUpKnobOutline != null)
        {
            leftCloseUpKnobOutline.enabled = isCloseUpOpen && activeKnob == RadioKnobId.Left;
        }

        if (rightCloseUpKnobOutline != null)
        {
            rightCloseUpKnobOutline.enabled = isCloseUpOpen && activeKnob == RadioKnobId.Right;
        }
    }

    // input helper for key down
    private static bool IsKeyDown(Key key)
    {
        var keyboard = Keyboard.current;
        return keyboard != null && key != Key.None && keyboard[key].wasPressedThisFrame;
    }

    // input helper for key held
    private static bool IsKeyHeld(Key key)
    {
        var keyboard = Keyboard.current;
        return keyboard != null && key != Key.None && keyboard[key].isPressed;
    }
}
