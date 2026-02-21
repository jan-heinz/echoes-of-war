using System.Collections;
using UnityEngine;

// UI hook for quit button
// in the unity editor, it stops play mode
// in a built game, quits the application
public class QuitOnClick : MonoBehaviour
{
    // short delay so click sfx can be heard before quit
    [SerializeField] [Min(0f)] private float quitDelaySeconds = 0.08f;

    // called by a UI button OnClick event
    public void Quit()
    {
        // quit immediately when delay is disabled
        if (quitDelaySeconds <= 0f)
        {
            ExecuteQuit();
            return;
        }

        // delay quit so button click sfx is audible
        StartCoroutine(QuitAfterDelay());
    }

    // quits app (or stops play mode in editor) after a short delay
    private IEnumerator QuitAfterDelay()
    {
        yield return new WaitForSeconds(quitDelaySeconds);
        ExecuteQuit();
    }

    // handles editor/build-specific quit behavior
    private static void ExecuteQuit()
    {
#if UNITY_EDITOR
        // if in editor, stops play mode
        Debug.Log("QuitOnClick: stopping play mode in editor");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // in a build, quit application
        Application.Quit();
#endif
    }
}
