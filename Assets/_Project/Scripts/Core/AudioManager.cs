using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Centralises all audio playback. Singleton persists across scenes.
/// Two AudioSources routed through AegisAudioMixer (set up in M6 settings work).
/// SFX clips: assign in Inspector once assets are imported.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // — Singleton ——————————————————————————————————————————
    public static AudioManager Instance { get; private set; }

    // — Serialized Fields ——————————————————————————————————
    [Header("Sources")]
    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioSource _sfxSource;

    [Header("Music")]
    [Tooltip("Ambient background track — loops. See audio sourcing brief.")]
    [SerializeField] private AudioClip _bgMusic;

    [Header("SFX")]
    [Tooltip("Short click sound for UI buttons.")]
    [SerializeField] private AudioClip _sfxUiClick;
    [Tooltip("Confirmation sound for contract acceptance.")]
    [SerializeField] private AudioClip _sfxContractAccept;
    [Tooltip("Positive resolution sound for contract success.")]
    [SerializeField] private AudioClip _sfxContractSuccess;
    [Tooltip("Negative resolution sound for contract failure.")]
    [SerializeField] private AudioClip _sfxContractFail;
    [Tooltip("Confirmation chime for successful hire.")]
    [SerializeField] private AudioClip _sfxHireConfirm;

    // — Stored lambda references (required for correct unsubscription) ———
    private System.Action<Contract> _onContractAccepted;
    private System.Action<Contract> _onContractCompleted;
    private System.Action<Contract> _onContractFailed;
    private System.Action<Employee> _onEmployeeHired;

    // — Unity Lifecycle ————————————————————————————————————
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartMusic();
    }

    private void OnEnable()
    {
        _onContractAccepted = _ => PlaySFX(_sfxContractAccept);
        _onContractCompleted = _ => PlaySFX(_sfxContractSuccess);
        _onContractFailed = _ => PlaySFX(_sfxContractFail);
        _onEmployeeHired = _ => PlaySFX(_sfxHireConfirm);

        ContractManager.OnContractAccepted += _onContractAccepted;
        ContractManager.OnContractCompleted += _onContractCompleted;
        ContractManager.OnContractFailed += _onContractFailed;
        EmployeeManager.OnEmployeeHired += _onEmployeeHired;
    }

    private void OnDisable()
    {
        ContractManager.OnContractAccepted -= _onContractAccepted;
        ContractManager.OnContractCompleted -= _onContractCompleted;
        ContractManager.OnContractFailed -= _onContractFailed;
        EmployeeManager.OnEmployeeHired -= _onEmployeeHired;
    }

    // — Public Methods —————————————————————————————————————

    /// <summary>
    /// Plays the UI click SFX. Call this from SettingsPanel tab buttons,
    /// nav buttons, and other interactive elements.
    /// </summary>
    public void PlayUIClick() => PlaySFX(_sfxUiClick);

    // — Private ————————————————————————————————————————————

    private void StartMusic()
    {
        if (_musicSource == null || _bgMusic == null) return;

        _musicSource.clip = _bgMusic;
        _musicSource.loop = true;
        _musicSource.volume = SettingsManager.Instance != null
            ? SettingsManager.Instance.MusicVolume
            : 0.6f;
        _musicSource.Play();
    }

    private void PlaySFX(AudioClip clip)
    {
        if (clip == null || _sfxSource == null) return;
        _sfxSource.PlayOneShot(clip);
    }
}