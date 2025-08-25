using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettingsManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("UI Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    // PlayerPrefs keys
    private const string MasterVolumeKey = "MasterVolume";
    private const string BGMVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";

    // Mixer exposed parameter names
    private const string MasterVolumeParam = "MasterVolume";
    private const string BGMVolumeParam = "MusicVolume";
    private const string SFXVolumeParam = "SFXVolume";

    private void Awake()
    {
        // Add listeners to the sliders
        masterSlider.onValueChanged.AddListener(SetMasterVolume);
        bgmSlider.onValueChanged.AddListener(SetBGMVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Load saved settings
        LoadVolumeSettings();
    }

    private void LoadVolumeSettings()
    {
        // Load slider values from PlayerPrefs, defaulting to 1.0f (max volume)
        masterSlider.value = PlayerPrefs.GetFloat(MasterVolumeKey, 1.0f);
        bgmSlider.value = PlayerPrefs.GetFloat(BGMVolumeKey, 1.0f);
        sfxSlider.value = PlayerPrefs.GetFloat(SFXVolumeKey, 1.0f);

        // Apply the loaded values to the mixer
        SetMasterVolume(masterSlider.value);
        SetBGMVolume(bgmSlider.value);
        SetSFXVolume(sfxSlider.value);
    }

    public void SetMasterVolume(float value)
    {
        SetVolume(MasterVolumeParam, value);
        PlayerPrefs.SetFloat(MasterVolumeKey, value);
    }

    public void SetBGMVolume(float value)
    {
        SetVolume(BGMVolumeParam, value);
        PlayerPrefs.SetFloat(BGMVolumeKey, value);
    }

    public void SetSFXVolume(float value)
    {
        SetVolume(SFXVolumeParam, value);
        PlayerPrefs.SetFloat(SFXVolumeKey, value);
    }

    private void SetVolume(string parameterName, float sliderValue)
    {
        float db;

        if (sliderValue <= 0.5f)
        {
            // Part 1: Attenuation (-80dB to 0dB) using a logarithmic scale
            // Map slider range [0, 0.5] to a [0, 1] range for calculation
            float remappedValue = sliderValue * 2f;
            db = Mathf.Log10(Mathf.Max(remappedValue, 0.0001f)) * 20;
        }
        else
        {
            // Part 2: Amplification (0dB to +10dB) using a linear scale
            // Map slider range [0.5, 1] to a [0, 1] range for interpolation
            float remappedValue = (sliderValue - 0.5f) * 2f;
            db = Mathf.Lerp(0, 10, remappedValue);
        }

        audioMixer.SetFloat(parameterName, db);
    }

    private void OnDestroy()
    {
        // Save settings when the object is destroyed (e.g., scene change or game quit)
        PlayerPrefs.Save();
    }
}