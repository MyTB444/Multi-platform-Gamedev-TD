using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSLider : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider volumeSlider;
    public static AudioSLider instance;

    void Start()
    {
        // Load saved volume or default to full
        instance = this;
        float savedVolume = PlayerPrefs.GetFloat("Volume", 0f);
        volumeSlider.value = savedVolume;
        SetVolume(savedVolume);
    }
    public void SetToZero()
    {
        volumeSlider.value = -80;
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat("Volume", volume);
    }
}
