using System.Collections;
using UnityEngine;

// UI hook for back button
// delegates to SceneHistory which tracks previously visited scenes
// clicking the back button returns the player to the last scene in the stack
public class LoadPreviousSceneOnClick : MonoBehaviour
{
    // short delay so click sfx can be heard before scene unload
    [SerializeField] [Min(0f)] private float loadDelaySeconds = 0.08f;

    // called by a UI button OnClick event
    // asks SceneHistory to load the previous scene if one exists
    public void LoadPrevious()
    {
        // load immediately when delay is disabled
        if (loadDelaySeconds <= 0f)
        {
            SceneHistory.LoadPrevious();
            return;
        }

        // delay load so button click sfx is audible
        StartCoroutine(LoadPreviousAfterDelay());
    }

    // loads previous scene after a short delay
    private IEnumerator LoadPreviousAfterDelay()
    {
        yield return new WaitForSeconds(loadDelaySeconds);
        SceneHistory.LoadPrevious();
    }
}
