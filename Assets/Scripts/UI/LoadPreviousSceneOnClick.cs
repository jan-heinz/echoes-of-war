using UnityEngine;

// UI hook for back button
// delegates to SceneHistory which tracks previously visited scenes
// clicking the back button returns the player to the last scene in the stack

public class LoadPreviousSceneOnClick : MonoBehaviour
{

    // called by a UI button OnClick event
    // asks SceneHistory to load the previous scene if one exists
    public void LoadPrevious()
    {
        SceneHistory.LoadPrevious();
    }
}
