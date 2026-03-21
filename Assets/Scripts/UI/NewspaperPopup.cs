using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// simple newspaper popup for tutorial mvp
//  - shows one inspector text body
//  - closes via button
public class NewspaperPopup : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private Button closeButton;
    [SerializeField] [TextArea] private string newspaperText;

    [Header("Sfx")]
    [SerializeField] private AudioSource rustleAudioSource;
    [SerializeField] private AudioClip[] rustleClips;
    [SerializeField] [Range(0f, 1f)] private float rustleVolume = 1f;

    public event Action Closed;

    public bool IsOpen { get; private set; }

    // wires close button event
    private void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HandleCloseClicked);
        }

        IsOpen = popupRoot != null && popupRoot.activeSelf;
    }

    // unhooks close button event
    private void OnDestroy()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseClicked);
        }
    }

    // shows popup with default inspector text
    public void ShowDefault()
    {
        if (bodyText != null)
        {
            bodyText.text = newspaperText;
        }
        else
        {
            Debug.LogWarning("NewspaperPopup: bodyText is not assigned.");
        }

        var opened = false;
        if (popupRoot != null)
        {
            popupRoot.SetActive(true);
            opened = true;
        }
        else
        {
            Debug.LogWarning("NewspaperPopup: popupRoot is not assigned.");
        }

        IsOpen = opened;
        PlayRandomRustleSfx();
    }

    // forces popup hidden
    public void Hide()
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }

        IsOpen = false;
    }

    // closes popup and broadcasts closed event
    private void HandleCloseClicked()
    {
        Hide();
        Closed?.Invoke();
    }

    // plays one random rustle clip on popup show
    private void PlayRandomRustleSfx()
    {
        if (rustleAudioSource == null || rustleClips == null || rustleClips.Length == 0)
        {
            return;
        }

        var clip = rustleClips[UnityEngine.Random.Range(0, rustleClips.Length)];
        if (clip == null)
        {
            return;
        }

        rustleAudioSource.PlayOneShot(clip, ClipVolumeRegistry.ScaleVolume(clip, rustleVolume));
    }
}
