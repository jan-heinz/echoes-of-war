using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// generic clip-group volume slider
//  - uses the slider's configured min/max range
//  - persists one normalized value per slider key
//  - applies its multiplier to any assigned audio clips
//  - optionally mirrors min/max/current values into labels
[RequireComponent(typeof(Slider))]
public class VolumeSlider : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IEndDragHandler
{
    private const string VolumePrefPrefix = "settings.volume.";

    [Header("Settings")]
    [SerializeField] private string settingKey = "volume";
    [SerializeField] private AudioClip[] affectedClips;

    [Header("Preview")]
    [SerializeField] private AudioSource previewAudioSource;
    [SerializeField] private AudioClip previewClip;

    [Header("Optional Labels")]
    [SerializeField] private TMP_Text minValueLabel;
    [SerializeField] private TMP_Text maxValueLabel;
    [SerializeField] private TMP_Text currentValueLabel;

    private Slider slider;
    private string resolvedSettingKey;
    private bool isPreviewing;

    // caches slider reference, restores saved value, and registers clips
    private void Awake()
    {
        slider = GetComponent<Slider>();
        resolvedSettingKey = ResolveSettingKey();
        slider.SetValueWithoutNotify(GetSavedSliderValue());
        RefreshLabels();
        RegisterAffectedClips();
        slider.onValueChanged.AddListener(HandleSliderValueChanged);
    }

    // refreshes ui and clip registration if object becomes active later
    private void OnEnable()
    {
        if (slider == null)
        {
            slider = GetComponent<Slider>();
        }

        if (string.IsNullOrWhiteSpace(resolvedSettingKey))
        {
            resolvedSettingKey = ResolveSettingKey();
        }

        slider.SetValueWithoutNotify(GetSavedSliderValue());
        RefreshLabels();
        RegisterAffectedClips();
    }

    // removes event hook and clip registration
    private void OnDisable()
    {
        if (!string.IsNullOrWhiteSpace(resolvedSettingKey))
        {
            ClipVolumeRegistry.UnregisterClips(resolvedSettingKey);
        }

        StopPreview();
    }

    // unhooks slider event
    private void OnDestroy()
    {
        if (slider != null)
        {
            slider.onValueChanged.RemoveListener(HandleSliderValueChanged);
        }
    }

    // saves new slider value and updates all affected clips
    private void HandleSliderValueChanged(float value)
    {
        SaveSliderValue(value);
        RefreshLabels();
        RegisterAffectedClips();
        UpdatePreviewVolume();
    }

    // updates optional endpoint/current labels from the live slider range
    private void RefreshLabels()
    {
        if (minValueLabel != null)
        {
            minValueLabel.text = slider.minValue.ToString("0");
        }

        if (maxValueLabel != null)
        {
            maxValueLabel.text = slider.maxValue.ToString("0");
        }

        if (currentValueLabel != null)
        {
            currentValueLabel.text = slider.value.ToString("0");
        }
    }

    // registers the current normalized multiplier for every assigned clip
    private void RegisterAffectedClips()
    {
        ClipVolumeRegistry.RegisterClips(resolvedSettingKey, affectedClips, GetNormalizedSliderValue(slider.value));
    }

    // starts a looping preview when the player begins adjusting the slider
    private void StartPreview()
    {
        if (previewAudioSource == null || previewClip == null)
        {
            return;
        }

        previewAudioSource.clip = previewClip;
        previewAudioSource.loop = true;
        UpdatePreviewVolume();

        if (!previewAudioSource.isPlaying)
        {
            previewAudioSource.Play();
        }

        isPreviewing = true;
    }

    // stops the looping preview when slider interaction ends
    private void StopPreview()
    {
        if (previewAudioSource == null)
        {
            return;
        }

        if (previewAudioSource.isPlaying && isPreviewing)
        {
            previewAudioSource.Stop();
        }

        isPreviewing = false;
    }

    // keeps preview audio aligned to the slider's current effective volume
    private void UpdatePreviewVolume()
    {
        if (previewAudioSource == null || previewClip == null)
        {
            return;
        }

        previewAudioSource.volume = ClipVolumeRegistry.ScaleVolume(previewClip, 1f);
    }

    // reads saved normalized value and remaps it to the slider range
    private float GetSavedSliderValue()
    {
        var normalizedValue = PlayerPrefs.GetFloat(GetPlayerPrefsKey(), 1f);
        return SliderFromNormalized(normalizedValue);
    }

    // stores the current slider value as a normalized 0..1 percentage
    private void SaveSliderValue(float value)
    {
        PlayerPrefs.SetFloat(GetPlayerPrefsKey(), GetNormalizedSliderValue(value));
        PlayerPrefs.Save();
    }

    // converts the current slider range into a normalized value
    private float GetNormalizedSliderValue(float value)
    {
        var range = slider.maxValue - slider.minValue;
        if (range <= 0f)
        {
            return 1f;
        }

        return Mathf.InverseLerp(slider.minValue, slider.maxValue, value);
    }

    // converts a saved normalized value back into the slider's range
    private float SliderFromNormalized(float normalizedValue)
    {
        return Mathf.Lerp(slider.minValue, slider.maxValue, Mathf.Clamp01(normalizedValue));
    }

    // picks a stable settings key, defaulting to object name when blank
    private string ResolveSettingKey()
    {
        return string.IsNullOrWhiteSpace(settingKey) ? gameObject.name : settingKey.Trim();
    }

    // returns the player prefs key for this slider
    private string GetPlayerPrefsKey()
    {
        return VolumePrefPrefix + resolvedSettingKey;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        StartPreview();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopPreview();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        StartPreview();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        StopPreview();
    }
}
