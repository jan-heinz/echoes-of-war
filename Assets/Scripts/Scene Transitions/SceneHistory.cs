using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// tracks a history (in the form of a stack) of visited scenes
// pushes the current scene before a transition
// loades the previous scene when back is clicked

public static class SceneHistory
{
    // LIFO stack of scene names
    // ie most recent scene is returned first
    private static readonly Stack<string> BackStack = new Stack<string>();

    // records a scene name
    public static void PushCurrent(string sceneName)
    {
        // checks if scene name is empty or invalid
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return;
        }

        // push the scene onto the back stack
        BackStack.Push(sceneName);
    }

    // attempts to pop the most recent scene from the stack
    // returns true and the name if available
    // otherwise returns false
    public static bool TryGetPrevious(out string sceneName)
    {
        // nothing to pop if the stack is empty
        if (BackStack.Count == 0)
        {
            sceneName = string.Empty;
            return false;
        }

        // pop the last scene and return it
        sceneName = BackStack.Pop();
        return true;
    }

    // load the most recent scene in history
    public static void LoadPrevious()
    {
        // if there is no previous scene, log and exit
        if (!TryGetPrevious(out var previous))
        {
            Debug.LogWarning("SceneHistory: no previous scene to load");
            return;
        }

        // load previous scene by name
        SceneManager.LoadScene(previous);
    }

    // clear the history 
    public static void Clear()
    {
        BackStack.Clear();
    }
}
