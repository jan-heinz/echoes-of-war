using UnityEngine;
using UnityEngine.UI;

// button click sfx helper
//  - listens to button onClick
//  - plays one-shot clip 
[RequireComponent(typeof(Button))]
public class ClickSFX : MonoBehaviour
{
    [SerializeField] private AudioSource clickAudioSource;
    [SerializeField] private AudioClip clickClip;
    [SerializeField] [Range(0f, 1f)] private float clickVolume = 1f;
    
    private Button button;

    // caches button reference and wires click event
    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(PlayClickSfx);
        }
    }

    // unhooks click event
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSfx);
        }
    }

    // called by button OnClick
    public void PlayClickSfx()
    {
        if (clickAudioSource == null || clickClip == null)
        {
            return;
        }
        
        clickAudioSource.PlayOneShot(clickClip, clickVolume);
    }
}
