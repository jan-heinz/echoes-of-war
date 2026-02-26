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
}
