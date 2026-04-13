using UnityEngine;

[RequireComponent(typeof(AudioSource))]
// keeps one looping ambience source alive across scene loads
public class AmbientLoopPlayer : MonoBehaviour
{
    private const string SfxVolumePrefKey = "settings.volume.sfx";

    private static AmbientLoopPlayer instance;

    [SerializeField] private AudioSource ambientAudioSource;
    [SerializeField] private AudioClip ambientClip;
    [SerializeField] [Range(0f, 1f)] private float baseVolume = 1f;
    private float appliedVolume = -1f;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureAudioSource();
        ConfigureAudioSource();
        ApplySavedVolume();
        StartPlaybackIfReady();
    }

    private void Update()
    {
        ApplySavedVolume();
    }

    private void OnValidate()
    {
        if (ambientAudioSource == null)
        {
            ambientAudioSource = GetComponent<AudioSource>();
        }
    }

    private void EnsureAudioSource()
    {
        if (ambientAudioSource == null)
        {
            ambientAudioSource = GetComponent<AudioSource>();
        }
    }

    private void ConfigureAudioSource()
    {
        if (ambientAudioSource == null)
        {
            return;
        }

        ambientAudioSource.playOnAwake = false;
        ambientAudioSource.loop = true;
        ambientAudioSource.spatialBlend = 0f;

        if (ambientClip != null)
        {
            ambientAudioSource.clip = ambientClip;
        }
    }

    private void ApplySavedVolume()
    {
        if (ambientAudioSource == null)
        {
            return;
        }

        var savedMultiplier = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumePrefKey, 1f));
        var targetVolume = baseVolume * savedMultiplier;

        if (Mathf.Approximately(appliedVolume, targetVolume))
        {
            return;
        }

        appliedVolume = targetVolume;
        ambientAudioSource.volume = targetVolume;
    }

    private void StartPlaybackIfReady()
    {
        if (ambientAudioSource == null || ambientAudioSource.clip == null || ambientAudioSource.isPlaying)
        {
            return;
        }

        ambientAudioSource.Play();
    }
}
