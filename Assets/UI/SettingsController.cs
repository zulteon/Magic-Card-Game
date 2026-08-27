using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SettingsController : MonoBehaviour
{
    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    private Resolution[] availableResolutions;

    [Header("Audio")]
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider soundEffectsSlider;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource soundEffectsSource;

    private void Start()
    {
        InitResolutionDropdown();
        InitAudioSliders();
    }
    [Header("Panel toggle")]
    [SerializeField] private GameObject settingsCanvas;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            settingsCanvas.SetActive(!settingsCanvas.activeSelf);
        }
    }
    // ───────── RESOLUTION ─────────

    private void InitResolutionDropdown()
    {
        var allResolutions = Screen.resolutions;
        var uniqueResolutions = new List<Resolution>();
        var seen = new HashSet<(int, int)>();

        foreach (var res in allResolutions)
        {
            var key = (res.width, res.height);
            if (seen.Contains(key)) continue;
            seen.Add(key);
            uniqueResolutions.Add(res);
        }

        availableResolutions = uniqueResolutions.ToArray();

        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            options.Add($"{availableResolutions[i].width} : {availableResolutions[i].height}");

            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();

        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
    }

    public void OnResolutionChanged(int index)
    {
        var res = availableResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
        PlayerPrefs.SetInt("ResolutionIndex", index);
    }

    // ───────── AUDIO ─────────

    private void InitAudioSliders()
    {
        float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 0.8f);
        float savedSfx = PlayerPrefs.GetFloat("SfxVolume", 0.8f);

        musicSlider.value = savedMusic;
        soundEffectsSlider.value = savedSfx;

        SetMusicVolume(savedMusic);
        SetSoundEffectsVolume(savedSfx);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        soundEffectsSlider.onValueChanged.AddListener(SetSoundEffectsVolume);
    }

    public void SetMusicVolume(float value)
    {
        if (musicSource != null) musicSource.volume = value;
        PlayerPrefs.SetFloat("MusicVolume", value);
    }

    public void SetSoundEffectsVolume(float value)
    {
        if (soundEffectsSource != null) soundEffectsSource.volume = value;
        PlayerPrefs.SetFloat("SfxVolume", value);
    }

    // ───────── KÉPVÁLASZTÁS (üres, később töltendő) ─────────

    public void OnFrontCardImageSelected(Sprite selected)
    {
        // TODO: elmenteni és alkalmazni a kártya-hátlap grafikát
    }

    public void OnBoardBackgroundSelected(Sprite selected)
    {
        // TODO: elmenteni és alkalmazni a board hátterét
    }

    public void OnMinionFrameStyleSelected(Sprite selected)
    {
        // TODO: elmenteni és alkalmazni a minion-keret stílusát
    }
}