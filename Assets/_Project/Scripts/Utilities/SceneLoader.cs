using UnityEngine;

/// <summary>
/// Central utility for all scene transitions. Never call SceneManager directly
/// elsewhere in the project — always route through here.
/// </summary>
public static class SceneLoader
{
    // Lazy-initialised runner persists across scenes via DontDestroyOnLoad.
    private static SceneLoaderRunner _runner;

    private static SceneLoaderRunner Runner
    {
        get
        {
            if (_runner != null) return _runner;

            var go = new GameObject("[SceneLoaderRunner]");
            Object.DontDestroyOnLoad(go);
            _runner = go.AddComponent<SceneLoaderRunner>();
            return _runner;
        }
    }

    /// <summary>Loads the named scene asynchronously, replacing the current scene.</summary>
    public static void Load(string sceneName)
    {
        Runner.StartCoroutine(Runner.LoadSceneAsync(sceneName));
    }
}