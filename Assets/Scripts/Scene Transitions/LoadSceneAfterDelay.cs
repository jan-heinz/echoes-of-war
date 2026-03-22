using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// auto-loads a target scene after a fixed delay
//  1. validates the configured scene name
//  2. verifies the scene is in the build settings
//  3. waits for the configured delay
//  4. loads the requested scene
public class LoadSceneAfterDelay : MonoBehaviour
{
    // the name of the scene to load after the interstitial
    // must match the scene asset name
    [SerializeField] private string sceneName;
    // how long the interstitial stays on screen before advancing
    [SerializeField] [Min(0f)] private float loadDelaySeconds = 2f;

    private Coroutine loadCoroutine;

    // validates configuration and starts the timed load
    private void OnEnable()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("LoadSceneAfterDelay: sceneName is empty");
            return;
        }

        if (!SceneIsInBuild(sceneName))
        {
            Debug.LogError($"LoadSceneAfterDelay: Scene '{sceneName}' is not in build settings");
            return;
        }

        loadCoroutine = StartCoroutine(LoadAfterDelay(sceneName));
    }

    // stops pending load if object or scene is disabled early
    private void OnDisable()
    {
        if (loadCoroutine != null)
        {
            StopCoroutine(loadCoroutine);
            loadCoroutine = null;
        }
    }

    // checks build settings for a scene by path
    // assumes scenes are in Assets/Scenes
    private static bool SceneIsInBuild(string name)
    {
        var buildIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{name}.unity");
        return buildIndex >= 0;
    }

    // waits for the configured duration and then loads the target scene
    private IEnumerator LoadAfterDelay(string targetSceneName)
    {
        yield return new WaitForSeconds(loadDelaySeconds);
        SceneManager.LoadScene(targetSceneName);
    }
}
