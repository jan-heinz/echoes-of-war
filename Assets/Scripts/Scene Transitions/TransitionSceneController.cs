using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// drives the interstitial transition scene and loads the requested follow-up scene
public class TransitionSceneController : MonoBehaviour
{
    private struct PendingTransition
    {
        public string TargetSceneName;
        public string[] Messages;
    }

    private static PendingTransition pendingTransition;

    [Header("Defaults")]
    [SerializeField] private string defaultTargetSceneName = "Office v2";
    [SerializeField] [TextArea] private string[] defaultMessages = { "Audio Required\n\nHeadphones Recommended" };

    [Header("UI")]
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] [Min(0f)] private float displayDurationSeconds = 3.5f;
    [SerializeField] [Min(0f)] private float fadeDurationSeconds = 0.45f;

    public static void QueueTransition(string targetSceneName, string[] messages)
    {
        pendingTransition = new PendingTransition
        {
            TargetSceneName = targetSceneName?.Trim(),
            Messages = messages
        };
    }

    private void Start()
    {
        var targetSceneName = defaultTargetSceneName;
        var displayMessages = SanitizeMessages(defaultMessages);

        if (!string.IsNullOrWhiteSpace(pendingTransition.TargetSceneName))
        {
            targetSceneName = pendingTransition.TargetSceneName;
            var queuedMessages = SanitizeMessages(pendingTransition.Messages);
            if (queuedMessages.Length > 0)
            {
                displayMessages = queuedMessages;
            }
        }

        pendingTransition = default;
        StartCoroutine(RunTransition(targetSceneName, displayMessages));
    }

    private IEnumerator RunTransition(string targetSceneName, string[] messages)
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
        {
            Debug.LogWarning("TransitionSceneController: targetSceneName is empty.");
            yield break;
        }

        if (!SceneIsInBuild(targetSceneName))
        {
            Debug.LogError($"TransitionSceneController: Scene '{targetSceneName}' is not in build settings.");
            yield break;
        }

        var displayMessages = messages != null && messages.Length > 0
            ? messages
            : new[] { string.Empty };

        for (var i = 0; i < displayMessages.Length; i++)
        {
            if (messageText != null)
            {
                messageText.text = displayMessages[i];
            }

            yield return new WaitForSecondsRealtime(displayDurationSeconds);
        }

        var fadeOverlay = ScreenFadeOverlay.EnsureInstance();
        yield return fadeOverlay.FadeToBlack(fadeDurationSeconds);
        fadeOverlay.PrepareFadeInOnNextSceneLoad(fadeDurationSeconds);

        SceneManager.LoadScene(targetSceneName);
    }

    private static bool SceneIsInBuild(string sceneName)
    {
        var buildIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{sceneName}.unity");
        return buildIndex >= 0;
    }

    private static string[] SanitizeMessages(string[] messages)
    {
        if (messages == null || messages.Length == 0)
        {
            return Array.Empty<string>();
        }

        var sanitizedMessages = new System.Collections.Generic.List<string>(messages.Length);
        for (var i = 0; i < messages.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(messages[i]))
            {
                continue;
            }

            sanitizedMessages.Add(messages[i]);
        }

        return sanitizedMessages.ToArray();
    }
}
