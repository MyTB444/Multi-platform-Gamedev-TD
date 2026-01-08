using UnityEngine;
using UnityEngine.Audio;

public class SFXPlayer : MonoBehaviour
{
    public static SFXPlayer instance;
    
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    
    private void Awake() => instance = this;
    
    public void Play(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;
        
        GameObject temp = new GameObject("TempAudio");
        temp.transform.position = position;
        AudioSource source = temp.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = volume;
        source.spatialBlend = 1f;
        source.outputAudioMixerGroup = sfxMixerGroup;
        source.Play();
        Destroy(temp, clip.length);
    }
}