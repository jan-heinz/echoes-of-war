using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// lets a UI button load a target scene by name
//  1. validates the configured scene name
//  2. verifies the scene is in the build settings
//  3. stores the current scene in the back stack
//  4. loads requested scene (optionally after a short delay)

public class LoadSceneOnClick : MonoBehaviour
{
    // the name of the scene to load
    // must match the scene asset name
    [SerializeField] private string sceneName;
    // short delay so click sfx can be heard before scene unload
    [SerializeField] [Min(0f)] private float loadDelaySeconds = 0.08f;

    // called by a UI button OnClick event
    public void Load()
    {
        // guard for missing/blank scene name in inspector
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("LoadSceneOnClick: sceneName is empty");
            return;
        }

        // ensure the scene is added to the build settings
        if (!SceneIsInBuild(sceneName))
        {
            Debug.LogError($"LoadSceneOnClick: Scene '{sceneName}' is not in build settings");
            return;
        }

        // record the current scene so back button can retrieve it
        SceneHistory.PushCurrent(SceneManager.GetActiveScene().name);

        // load immediately when delay is disabled
        if (loadDelaySeconds <= 0f)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        // delay scene load so button click sfx is audible
        StartCoroutine(LoadAfterDelay(sceneName));
    }
    
    // checks build settings for a scene by path
    // assumes scenes are in Assets/Scenes
    private static bool SceneIsInBuild(string name)
    {
        var buildIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{name}.unity");
        return buildIndex >= 0;
    }

    // loads the requested scene after a short delay
    private IEnumerator LoadAfterDelay(string targetSceneName)
    {
        yield return new WaitForSeconds(loadDelaySeconds);
        SceneManager.LoadScene(targetSceneName);
    }
}
