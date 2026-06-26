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

        // M0: nothing to initialise yet.
        // Future: AudioManager.Initialize(), SettingsManager.Load(), etc. go here,
        // each as their own yield step so they can be async if needed.

        // Yield one frame before transitioning. Prevents a single-frame black flash
        // that could be mistaken for a crash on slower machines.
        yield return null;

        Debug.Log("[Boot] Boot sequence complete. Loading Main Menu.");
        SceneLoader.Load(AegisConstants.SCENE_MAIN_MENU);
    }
}