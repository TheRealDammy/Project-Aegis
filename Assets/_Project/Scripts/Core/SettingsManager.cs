using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Manages audio and display settings. Persists to PlayerPrefs.
/// Singleton — initialized in Boot, persists across all scenes.
///
/// Audio note: AudioMixer is optional. If no mixer is assigned,
/// volume changes are persisted but have no audible effect until
/// audio assets and an AudioMixer are added to the project.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    // — Singleton ——————————————————————————————————————————
    public static SettingsManager Instance { get; private set; }

    // — Serialized Fields ——————————————————————————————————
    /// <summary>
    /// AudioMixer with three exposed parameters: MasterVolume, SFXVolume, MusicVolume.
    /// Assign in Inspector once AudioMixer asset is created.
    /// If null: volume changes persist to PlayerPrefs but don't affect audio.
    /// </summary>
    [SerializeField] private AudioMixer _audioMixer;

    // — Public Properties ——————————————————————————————————
    public float MasterVolume { get; private set; }
    public float SFXVolume { get; private set; }
    public float MusicVolume { get; private set; }
    public bool IsFullscreen { get; private set; }
    public int ResolutionIndex { get; private set; }

    // — Unity Lifecycle ————————————————————————————————————
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAndApply();
    }

    // — Public Methods —————————————————————————————————————

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        ApplyVolume(AegisConstants.AUDIO_PARAM_MASTER, MasterVolume);
        PlayerPrefs.SetFloat(AegisConstants.PREF_MASTER_VOLUME, MasterVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        ApplyVolume(AegisConstants.AUDIO_PARAM_SFX, SFXVolume);
        PlayerPrefs.SetFloat(AegisConstants.PREF_SFX_VOLUME, SFXVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        ApplyVolume(AegisConstants.AUDIO_PARAM_MUSIC, MusicVolume);
        PlayerPrefs.SetFloat(AegisConstants.PREF_MUSIC_VOLUME, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool fullscreen)
    {
        IsFullscreen = fullscreen;
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(AegisConstants.PREF_FULLSCREEN, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int index)
    {
        Resolution[] resolutions = GetUniqueResolutions();
        if (index < 0 || index >= resolutions.Length) return;

        ResolutionIndex = index;
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, IsFullscreen);
        PlayerPrefs.SetInt(AegisConstants.PREF_RESOLUTION_INDEX, ResolutionIndex);
        PlayerPrefs.Save();
    }

    /// <summary>Returns the unique screen resolutions available on this display.</summary>
    public Resolution[] GetUniqueResolutions()
    {
        var all = Screen.resolutions;
        var unique = new System.Collections.Generic.List<Resolution>();
        var seen = new System.Collections.Generic.HashSet<string>();

        foreach (Resolution r in all)
        {
            string key = $"{r.width}x{r.height}";
            if (!seen.Add(key)) continue;
            unique.Add(r);
        }

        return unique.ToArray();
    }

    // — Private ————————————————————————————————————————————

    private void LoadAndApply()
    {
        MasterVolume = PlayerPrefs.GetFloat(AegisConstants.PREF_MASTER_VOLUME, AegisConstants.DEFAULT_MASTER_VOLUME);
        SFXVolume = PlayerPrefs.GetFloat(AegisConstants.PREF_SFX_VOLUME, AegisConstants.DEFAULT_SFX_VOLUME);
        MusicVolume = PlayerPrefs.GetFloat(AegisConstants.PREF_MUSIC_VOLUME, AegisConstants.DEFAULT_MUSIC_VOLUME);
        IsFullscreen = PlayerPrefs.GetInt(AegisConstants.PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        ResolutionIndex = PlayerPrefs.GetInt(AegisConstants.PREF_RESOLUTION_INDEX, GetCurrentResolutionIndex());

        ApplyVolume(AegisConstants.AUDIO_PARAM_MASTER, MasterVolume);
        ApplyVolume(AegisConstants.AUDIO_PARAM_SFX, SFXVolume);
        ApplyVolume(AegisConstants.AUDIO_PARAM_MUSIC, MusicVolume);
        Screen.fullScreen = IsFullscreen;

        Debug.Log($"[SettingsManager] Loaded. " +
                  $"Vol: {MasterVolume:F2}/{SFXVolume:F2}/{MusicVolume:F2} " +
                  $"Fullscreen: {IsFullscreen}");
    }

    private void ApplyVolume(string paramName, float linearValue)
    {
        if (_audioMixer == null) return; // Graceful degradation — no audio yet.

        // Convert linear 0–1 to decibels. Clamp to prevent -infinity at zero.
        float db = linearValue > 0.001f ? Mathf.Log10(linearValue) * 20f : -80f;
        _audioMixer.SetFloat(paramName, db);
    }

    private int GetCurrentResolutionIndex()
    {
        Resolution[] unique = GetUniqueResolutions();
        Resolution current = Screen.currentResolution;

        for (int i = 0; i < unique.Length; i++)
            if (unique[i].width == current.width && unique[i].height == current.height)
                return i;

        return unique.Length - 1; // Default to highest if not found.
    }
}