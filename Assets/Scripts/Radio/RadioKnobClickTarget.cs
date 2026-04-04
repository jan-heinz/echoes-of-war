using UnityEngine;
using UnityEngine.EventSystems;

// click target used by radio knobs
// routes click to radio controller and opens close-up
public class RadioKnobClickTarget : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private RadioController controller;
    [SerializeField] private RadioKnobId knobId;
    [SerializeField] private bool leftClickOnly = true;

    [Header("Click SFX")]
    [SerializeField] private AudioSource clickAudioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] [Range(0f, 1f)] private float clickVolume = 1f;

    // called by event system on pointer click
    public void OnPointerClick(PointerEventData eventData)
    {
        // ignore right/middle click when left-only is enabled
        if (leftClickOnly && eventData.button != PointerEventData.InputButton.Left)
        {
            return;
        }

        // controller is required to open close-up
        if (controller == null)
        {
            Debug.LogWarning($"RadioKnobClickTarget ({name}): controller is not assigned");
            return;
        }

        PlayClickSfx();

        // open close-up and focus this knob
        controller.OpenCloseUp(knobId);
    }

    // plays optional click audio
    // no-op when clip/source is missing
    private void PlayClickSfx()
    {
        if (clickAudioSource == null || clickClip == null)
        {
            return;
        }

        clickAudioSource.PlayOneShot(clickClip, ClipVolumeRegistry.ScaleVolume(clickClip, clickVolume));
    }
}
