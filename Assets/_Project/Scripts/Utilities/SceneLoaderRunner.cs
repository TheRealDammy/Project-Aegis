using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Internal coroutine host for SceneLoader. Do not reference this class directly.
/// Created at runtime by SceneLoader on first use and persists across scenes.
/// </summary>
internal class SceneLoaderRunner : MonoBehaviour
{
    internal IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError($"[SceneLoader] LoadSceneAsync returned null for '{sceneName}'. " +
                           "Confirm the scene name matches Build Settings exactly.");
            yield break;
        }

        while (!operation.isDone)
            yield return null;
    }
}