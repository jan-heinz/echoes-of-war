using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class RadioLeverClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RadioController controller;
    [SerializeField] private RadioTutorialPuzzle puzzle;
    [SerializeField] private bool leftClickOnly = true;

    [Header("Click SFX")]
    [FormerlySerializedAs("leverClickAudioSource")]
    [SerializeField] private AudioSource clickAudioSource;
    [FormerlySerializedAs("leverClickClip")]
    [SerializeField] private AudioClip clickClip;
    [FormerlySerializedAs("leverClickVolume")]
    [SerializeField] [Range(0f, 1f)] private float clickVolume = 1f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (leftClickOnly && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        if (controller == null)
        {
            Debug.LogWarning($"RadioLeverClickTarget ({name}): controller is not assigned");
            return;
        }

        if (puzzle != null && !puzzle.CanPullLever())
        {
            return;
        }

        PlayClickSfx();
        controller.PullLeverInteractively();
    }

    private void PlayClickSfx()
    {
        if (clickAudioSource == null || clickClip == null)
        {
            return;
        }

        clickAudioSource.PlayOneShot(clickClip, clickVolume);
    }
}
