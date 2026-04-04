using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum KnobRotationMode
{
    Continuous,
    Click
}

// controller for radio interaction
//  1. opens/closes close-up mode
//  2. tracks left/right knob angles
//  3. rotates active knob with keyboard input
//  4. keeps world and close-up visuals in sync
//  5. displays knob-change events for effects
public class RadioController : MonoBehaviour
{
    // per knob runtime configuration
    // lets each knob choose continuous vs click behavior 
    [Serializable]
    public class KnobRotationSettings
    {
        [SerializeField] private KnobRotationMode mode = KnobRotationMode.Continuous;
        [SerializeField] private int clickCount = 8;
        [SerializeField] private float degreesPerClick = 45f;
        [SerializeField] private float turnSpeedDegreesPerSecond = 120f;
        [SerializeField] private float startAngleDegrees = 0f;

        public KnobRotationMode Mode => mode;
        public int ClickCount => clickCount;
        public float TurnSpeedDegreesPerSecond => turnSpeedDegreesPerSecond;
        public float StartAngleDegrees => startAngleDegrees;

        // clamps inspector values
        // degrees per click is calculated from click count for full 360 coverage
        public void Validate()
        {
            clickCount = Mathf.Max(1, clickCount);
            degreesPerClick = 360f / clickCount;
            turnSpeedDegreesPerSecond = Mathf.Max(0f, turnSpeedDegreesPerSecond);
            startAngleDegrees = Mathf.Repeat(startAngleDegrees, 360f);
        }
    }

    // notification hook whenever a knob changes
    // when player turns a knob, RadioController sends a KnobChanged and which knob moved (including new angle/value)
    // this is so other systems such as audio, puzzle logic, etc can react without being built into this controller
    public event Action<RadioKnobId, float, float> KnobChanged;
    public event Action LeverThresholdReached;

    [Header("Knob Pivots")]
    [SerializeField] private Transform leftKnobPivot;
    [SerializeField] private Transform rightKnobPivot;

    [Header("Close-Up UI")]
    [SerializeField] private GameObject closeUpPanel;
    [SerializeField] private GameObject worldRadioView;
    [SerializeField] private GameObject worldJuicerIdleView;
    [SerializeField] private Transform leftCloseUpKnobPivot;
    [SerializeField] private Transform rightCloseUpKnobPivot;
    [SerializeField] private RectTransform closeUpLever;
    [SerializeField] private RectTransform closeUpLeverEndpoint;
    
    [Header("Close-Up Selection Sprite Swap (Optional)")]
    [SerializeField] private Image leftCloseUpKnobImage;
    [SerializeField] private Image rightCloseUpKnobImage;
    [SerializeField] private Sprite closeUpKnobSelectedSprite;

    [Header("Input")]
    [SerializeField] private Key turnLeftKey = Key.A;
    [SerializeField] private Key turnRightKey = Key.D;
    [SerializeField] private Key exitCloseUpKey = Key.Escape;

    [Header("Left Knob Settings")]
    [SerializeField] private KnobRotationSettings leftKnobSettings = new KnobRotationSettings();

    [Header("Right Knob Settings")]
    [SerializeField] private KnobRotationSettings rightKnobSettings = new KnobRotationSettings();

    [Header("Lever Settings")]
    [SerializeField] private float leverPullDurationSeconds = 0.2f;

    // runtime angle state
    private float leftAngleDegrees;
    private float rightAngleDegrees;
    private float leftRawAngleDegrees;
    private float rightRawAngleDegrees;
    // click-mode helpers
    private float clickRepeatTimerSeconds;
    private float lastClickDirection;

    // close-up runtime state
    private bool isCloseUpOpen;
    private RadioKnobId activeKnob = RadioKnobId.Left;
    private bool isLeverPulling;
    // stores baseline close-up sprites so deselect can restore them
    private Sprite leftCloseUpKnobDefaultSprite;
    private Sprite rightCloseUpKnobDefaultSprite;
    private Vector2 closeUpLeverStartAnchoredPosition;

    // lets other scripts query close-up state
    public bool IsCloseUpOpen => isCloseUpOpen;

    // lets other scripts query which knob is currently active
    public RadioKnobId ActiveKnob => activeKnob;

    private void Start()
    {
        ValidateSettings();
        CacheCloseUpDefaultSprites();
        CacheLeverStartPosition();
        ResetKnobsToDefaults();

        // close-up should start closed
        isCloseUpOpen = false;
        if (closeUpPanel != null)
        {
            closeUpPanel.SetActive(false);
        }

        // close-up starts closed
        // selection outline should be off
        UpdateCloseUpSelectionVisuals();
    }

    private void Update()
    {
        // only process turn input while in close-up mode
        if (!isCloseUpOpen)
        {
            clickRepeatTimerSeconds = 0f;
            lastClickDirection = 0f;
            return;
        }

        // escape key exits close-up
        if (IsKeyDown(exitCloseUpKey))
        {
            CloseCloseUp();
            return;
        }

        UpdateLeverPullAnimation();
        if (isLeverPulling)
        {
            return;
        }

        // read hold input for knob rotation
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

        var settings = GetKnobSettings(activeKnob);
        if (settings.Mode == KnobRotationMode.Click)
        {
            HandleClickModeInput(settings);
            return;
        }

        clickRepeatTimerSeconds = 0f;
        lastClickDirection = 0f;

        // convert input into degrees for this frame
        var angleDelta = turnDirection * settings.TurnSpeedDegreesPerSecond * Time.deltaTime;
        RotateActiveKnob(angleDelta);
    }

    // opens close-up mode and sets the active knob
    public void OpenCloseUp(RadioKnobId knobId)
    {
        // close-up panel is required
        if (closeUpPanel == null)
        {
            Debug.LogError("RadioController: closeUpPanel is not assigned");
            return;
        }

        // set active knob from whichever target was clicked
        activeKnob = knobId;
        isLeverPulling = false;

        // show close-up and enable input loop
        isCloseUpOpen = true;
        closeUpPanel.SetActive(true);
        SetWorldRadioVisible(false);

        // keep both world and close-up knob visuals in sync
        SyncAllVisuals();

        // highlight whichever knob is active in close-up
        UpdateCloseUpSelectionVisuals();
    }

    public void PullLeverInteractively()
    {
        if (closeUpPanel == null)
        {
            Debug.LogError("RadioController: closeUpPanel is not assigned");
            return;
        }

        isCloseUpOpen = true;
        closeUpPanel.SetActive(true);
        isLeverPulling = true;
        SetWorldRadioVisible(false);

        SyncAllVisuals();
        UpdateCloseUpSelectionVisuals();
    }

    // closes close-up mode
    public void CloseCloseUp()
    {
        isCloseUpOpen = false;

        if (closeUpPanel != null)
        {
            closeUpPanel.SetActive(false);
        }

        SetWorldRadioVisible(true);

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
        // raw angle stores true position
        // output angle stores post-mode value (snapped or continuous)
        leftRawAngleDegrees = NormalizeAngle(leftKnobSettings.StartAngleDegrees);
        rightRawAngleDegrees = NormalizeAngle(rightKnobSettings.StartAngleDegrees);

        leftAngleDegrees = ApplyRotationMode(leftRawAngleDegrees, leftKnobSettings);
        rightAngleDegrees = ApplyRotationMode(rightRawAngleDegrees, rightKnobSettings);
        isLeverPulling = false;
        RestoreLeverToStart();

        SyncAllVisuals();
    }

    // rotates whichever knob is currently active
    private void RotateActiveKnob(float deltaDegrees)
    {
        var settings = GetKnobSettings(activeKnob);
        var currentRawAngle = GetKnobRawAngle(activeKnob);
        var currentOutputAngle = GetKnobAngle(activeKnob);

        var nextRawAngle = NormalizeAngle(currentRawAngle + deltaDegrees);
        var nextOutputAngle = ApplyRotationMode(nextRawAngle, settings);

        // always update raw angle so click-mode can accumulate toward next detent
        SetKnobRawAngle(activeKnob, nextRawAngle);

        if (Mathf.Approximately(currentOutputAngle, nextOutputAngle))
        {
            return;
        }

        // update internal state for active knob
        SetKnobAngle(activeKnob, nextOutputAngle);

        ApplyKnobVisual(activeKnob, nextOutputAngle);

        // publish event for any future radio effects systems
        KnobChanged?.Invoke(activeKnob, nextOutputAngle, nextOutputAngle / 360f);
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

    private float GetKnobRawAngle(RadioKnobId knobId)
    {
        return knobId == RadioKnobId.Left ? leftRawAngleDegrees : rightRawAngleDegrees;
    }

    private void SetKnobRawAngle(RadioKnobId knobId, float angleDegrees)
    {
        if (knobId == RadioKnobId.Left)
        {
            leftRawAngleDegrees = angleDegrees;
            return;
        }

        rightRawAngleDegrees = angleDegrees;
    }

    private KnobRotationSettings GetKnobSettings(RadioKnobId knobId)
    {
        return knobId == RadioKnobId.Left ? leftKnobSettings : rightKnobSettings;
    }

    // applies rotation mode to a raw angle
    // click mode snaps to nearest detent
    // continuous mode passes through
    private static float ApplyRotationMode(float angleDegrees, KnobRotationSettings settings)
    {
        var normalized = NormalizeAngle(angleDegrees);
        if (settings.Mode != KnobRotationMode.Click)
        {
            return normalized;
        }

        var stepDegrees = 360f / settings.ClickCount;
        var snapped = Mathf.Round(normalized / stepDegrees) * stepDegrees;
        return NormalizeAngle(snapped);
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

    // updates close-up selected state visuals
    private void UpdateCloseUpSelectionVisuals()
    {
        ApplyCloseUpKnobSpriteSelection(
            leftCloseUpKnobImage,
            leftCloseUpKnobDefaultSprite,
            closeUpKnobSelectedSprite,
            isCloseUpOpen && !isLeverPulling && activeKnob == RadioKnobId.Left);

        ApplyCloseUpKnobSpriteSelection(
            rightCloseUpKnobImage,
            rightCloseUpKnobDefaultSprite,
            closeUpKnobSelectedSprite,
            isCloseUpOpen && !isLeverPulling && activeKnob == RadioKnobId.Right);
    }

    private void UpdateLeverPullAnimation()
    {
        if (!isLeverPulling)
        {
            return;
        }

        if (closeUpLever == null || closeUpLeverEndpoint == null)
        {
            isLeverPulling = false;
            return;
        }

        var duration = Mathf.Max(0.0001f, leverPullDurationSeconds);
        var nextPosition = Vector2.MoveTowards(
            closeUpLever.anchoredPosition,
            closeUpLeverEndpoint.anchoredPosition,
            Vector2.Distance(closeUpLeverStartAnchoredPosition, closeUpLeverEndpoint.anchoredPosition) * (Time.deltaTime / duration));

        closeUpLever.anchoredPosition = nextPosition;

        if (Vector2.Distance(closeUpLever.anchoredPosition, closeUpLeverEndpoint.anchoredPosition) <= 0.001f)
        {
            closeUpLever.anchoredPosition = closeUpLeverEndpoint.anchoredPosition;
            isLeverPulling = false;
            LeverThresholdReached?.Invoke();
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

    private void OnValidate()
    {
        ValidateSettings();
    }

    private void ValidateSettings()
    {
        if (leftKnobSettings != null)
        {
            leftKnobSettings.Validate();
        }

        if (rightKnobSettings != null)
        {
            rightKnobSettings.Validate();
        }

        leverPullDurationSeconds = Mathf.Max(0.01f, leverPullDurationSeconds);
    }

    // stores current close-up image sprites as default non-selected visuals
    private void CacheCloseUpDefaultSprites()
    {
        if (leftCloseUpKnobImage != null)
        {
            leftCloseUpKnobDefaultSprite = leftCloseUpKnobImage.sprite;
        }

        if (rightCloseUpKnobImage != null)
        {
            rightCloseUpKnobDefaultSprite = rightCloseUpKnobImage.sprite;
        }
    }

    private static void ApplyCloseUpKnobSpriteSelection(
        Image targetImage,
        Sprite defaultSprite,
        Sprite selectedSprite,
        bool isSelected)
    {
        if (targetImage == null || selectedSprite == null)
        {
            return;
        }

        targetImage.sprite = isSelected ? selectedSprite : defaultSprite;
    }

    private void CacheLeverStartPosition()
    {
        if (closeUpLever != null)
        {
            closeUpLeverStartAnchoredPosition = closeUpLever.anchoredPosition;
        }
    }

    private void RestoreLeverToStart()
    {
        if (closeUpLever != null)
        {
            closeUpLever.anchoredPosition = closeUpLeverStartAnchoredPosition;
        }
    }

    private void SetWorldRadioVisible(bool isVisible)
    {
        if (worldRadioView != null)
        {
            worldRadioView.SetActive(isVisible);
        }

        if (worldJuicerIdleView != null)
        {
            worldJuicerIdleView.SetActive(isVisible);
        }
    }

    // click-mode knobs move by discrete detents 
    private void HandleClickModeInput(KnobRotationSettings settings)
    {
        var clickDirection = 0f;
        if (IsKeyHeld(turnLeftKey))
        {
            clickDirection -= 1f;
        }
        if (IsKeyHeld(turnRightKey))
        {
            clickDirection += 1f;
        }

        if (Mathf.Approximately(clickDirection, 0f))
        {
            // no held key means reset repeat state
            clickRepeatTimerSeconds = 0f;
            lastClickDirection = 0f;
            return;
        }

        var stepDegrees = 360f / settings.ClickCount;
        var intervalSeconds = settings.TurnSpeedDegreesPerSecond <= 0f
            ? float.MaxValue
            : stepDegrees / settings.TurnSpeedDegreesPerSecond;

        var pressedThisFrame = (clickDirection < 0f && IsKeyDown(turnLeftKey)) ||
                               (clickDirection > 0f && IsKeyDown(turnRightKey));

        if (!Mathf.Approximately(lastClickDirection, clickDirection))
        {
            // direction changed
            // step immediately and restart repeat timing
            lastClickDirection = clickDirection;
            clickRepeatTimerSeconds = 0f;
            pressedThisFrame = true;
        }

        if (pressedThisFrame)
        {
            RotateActiveKnob(clickDirection * stepDegrees);
            return;
        }

        clickRepeatTimerSeconds += Time.deltaTime;
        if (clickRepeatTimerSeconds < intervalSeconds)
        {
            return;
        }

        clickRepeatTimerSeconds -= intervalSeconds;
        RotateActiveKnob(clickDirection * stepDegrees);
    }
}
