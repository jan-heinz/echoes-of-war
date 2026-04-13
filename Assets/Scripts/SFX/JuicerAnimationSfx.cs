using System.Collections;
using UnityEngine;

// plays one juicer sound cue at a fixed time after the execute animation starts
public class JuicerAnimationSfx : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clip;
    [SerializeField] [Min(0f)] private float delaySeconds;
    [SerializeField] [Range(0f, 1f)] private float volume = 1f;

    private Coroutine playRoutine;

    public void PlaySequence()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlaySequenceRoutine());
    }

    private IEnumerator PlaySequenceRoutine()
    {
        yield return WaitAndPlay(delaySeconds, audioSource, clip, volume);
        playRoutine = null;
    }

    private static IEnumerator WaitAndPlay(float delaySeconds, AudioSource audioSource, AudioClip clip, float volume)
    {
        if (delaySeconds > 0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }

        PlayClip(audioSource, clip, volume);
    }

    private static void PlayClip(AudioSource audioSource, AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, volume);
    }
}
