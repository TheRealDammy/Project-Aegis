using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Main Menu scene. Handles player navigation to other scenes.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    // — Serialized Fields —————————————————————————————————————
    [SerializeField] private Button _newGameButton;

    // — Unity Lifecycle ———————————————————————————————————————
    private void Awake()
    {
        if (_newGameButton == null)
        {
            Debug.LogError("[MainMenuController] _newGameButton is not assigned in the Inspector.");
            return;
        }

        _newGameButton.onClick.AddListener(OnNewGameClicked);
    }

    private void OnDestroy()
    {
        // Always unsubscribe — prevents memory leaks if the scene is ever
        // loaded additively or reloaded without a full application restart.
        if (_newGameButton != null)
            _newGameButton.onClick.RemoveListener(OnNewGameClicked);
    }

    // — Private Methods ———————————————————————————————————————
    private void OnNewGameClicked()
    {
        Debug.Log("[MainMenu] New Game selected.");
        SceneLoader.Load(AegisConstants.SCENE_GAME);
    }
}