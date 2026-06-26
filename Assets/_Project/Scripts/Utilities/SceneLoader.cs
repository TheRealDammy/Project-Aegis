using UnityEngine.SceneManagement;

/// <summary>
/// Central utility for all scene transitions.
/// Never call SceneManager directly elsewhere in the project — route through here.
/// </summary>
public static class SceneLoader
{
    /// <summary>Loads the named scene, replacing the current scene.</summary>
    public static void Load(string sceneName)
    {
        // Synchronous load is correct at M0 — all three scenes are empty.
        // with a loading screen. The call signature stays identical, so
        // nothing else in the project needs to change when that happens.
        SceneManager.LoadSceneAsync(sceneName);
    }
}