using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSLider : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Slider volumeSlider;

    void Start()
    {
        // Load saved volume or default to full
        float savedVolume = PlayerPrefs.GetFloat("Volume", 0f);
        volumeSlider.value = savedVolume;
        SetVolume(savedVolume);
    }

    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("MasterVolume", volume);
        PlayerPrefs.SetFloat("Volume", volume);
    }
}
