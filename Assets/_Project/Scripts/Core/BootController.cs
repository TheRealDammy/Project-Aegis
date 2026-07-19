using System.Collections;
using UnityEngine;

/// <summary>
/// Runs the application boot sequence then transitions to the Main Menu.
/// This is the entry point for the entire application — Boot.unity is scene index 0.
/// </summary>
public class BootController : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(RunBootSequence());
    }

    private IEnumerator RunBootSequence()
    {
        Debug.Log("[Boot] Boot sequence started.");

        // Initialize SettingsManager first — applies audio/display settings
        // before any scene renders visuals or audio.
        if (SettingsManager.Instance == null)
        {
            var go = new GameObject("[SettingsManager]");
            go.AddComponent<SettingsManager>();
            // SettingsManager.Awake handles DontDestroyOnLoad and loads prefs.
        }

        yield return null;

        Debug.Log("[Boot] Boot sequence complete. Loading Main Menu.");
        SceneLoader.Load(AegisConstants.SCENE_MAIN_MENU);
    }
}