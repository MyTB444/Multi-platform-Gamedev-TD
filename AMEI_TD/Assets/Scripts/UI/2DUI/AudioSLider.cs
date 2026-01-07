using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSLider : MonoBehaviour
{
    public AudioMixer audioMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider effectSlider;

    public static AudioSLider instance;

    private float savedMusicVolume;
    private float savedEffectVolume;
    private const float minVolume = -80f;

    void Start()
    {
        instance = this;

        // Load saved volumes or default to full (0 dB)
        float savedMaster = PlayerPrefs.GetFloat("MasterVolume", 0f);
        savedMusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0f);
        savedEffectVolume = PlayerPrefs.GetFloat("EffectVolume", 0f);

        masterSlider.value = savedMaster;
        musicSlider.value = savedMusicVolume;
        effectSlider.value = savedEffectVolume;

        SetMasterVolume(savedMaster);
        SetMusicVolume(savedMusicVolume);
        SetEffectVolume(savedEffectVolume);
    }

    public void SetMasterVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat("MasterVolume", volume);

        // When master is at minimum, mute others visually and audibly
        if (volume <= minVolume)
        {
            // Save current values before muting
            if (musicSlider.value > minVolume)
                savedMusicVolume = musicSlider.value;
            if (effectSlider.value > minVolume)
                savedEffectVolume = effectSlider.value;

            musicSlider.value = minVolume;
            effectSlider.value = minVolume;
            musicSlider.interactable = false;
            effectSlider.interactable = false;
        }
        else
        {
            // Restore sliders when master is raised
            if (!musicSlider.interactable)
            {
                musicSlider.interactable = true;
                effectSlider.interactable = true;
                musicSlider.value = savedMusicVolume;
                effectSlider.value = savedEffectVolume;
            }
        }
    }

    public void SetMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", volume);
        PlayerPrefs.SetFloat("MusicVolume", volume);
        savedMusicVolume = volume;
    }

    public void SetEffectVolume(float volume)
    {
        audioMixer.SetFloat("EffectVolume", volume);
        PlayerPrefs.SetFloat("EffectVolume", volume);
        savedEffectVolume = volume;
    }

    public void SetAllToZero()
    {
        masterSlider.value = minVolume;
    }

    public void SetMasterToZero()
    {
        masterSlider.value = minVolume;
    }

    public void SetMusicToZero()
    {
        musicSlider.value = minVolume;
    }

    public void SetEffectToZero()
    {
        effectSlider.value = minVolume;
    }
}