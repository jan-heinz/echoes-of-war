using UnityEngine;

// UI hook for quit button
// in the unity editor, it stops play mode
// in a built game, quits the application
public class QuitOnClick : MonoBehaviour
{
    // called by a UI button OnClick event
    public void Quit()
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
