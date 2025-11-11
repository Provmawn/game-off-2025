using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Ambient Music")]
    public AudioClip[] ambientTracks;
    public AudioSource ambientAudioSource;
    public float ambientVolume = 0.3f;
    public float fadeSpeed = 2f;
    
    [Header("Area Management")]
    public int currentAreaIndex = 0;
    
    private int lastAreaIndex = -1;
    private float targetVolume = 0f;
    
    void Start()
    {
        if (ambientAudioSource == null)
        {
            return;
        }
        
        PlayAmbientTrack(currentAreaIndex);
    }
    
    void Update()
    {
        if (currentAreaIndex != lastAreaIndex)
        {
            PlayAmbientTrack(currentAreaIndex);
            lastAreaIndex = currentAreaIndex;
        }
        
        if (ambientAudioSource != null)
        {
            ambientAudioSource.volume = Mathf.Lerp(ambientAudioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
        }
    }
    
    public void PlayAmbientTrack(int areaIndex)
    {
        if (ambientAudioSource == null || ambientTracks == null || ambientTracks.Length == 0)
        {
            return;
        }
        
        if (areaIndex < 0 || areaIndex >= ambientTracks.Length)
        {
            return;
        }
        
        AudioClip newTrack = ambientTracks[areaIndex];
        if (newTrack == null)
        {
            return;
        }
        
        if (ambientAudioSource.clip != newTrack)
        {
            StartCoroutine(CrossfadeToTrack(newTrack));
        }
        else if (!ambientAudioSource.isPlaying)
        {
            ambientAudioSource.clip = newTrack;
            ambientAudioSource.loop = true;
            ambientAudioSource.Play();
            targetVolume = ambientVolume;
        }
    }
    
    private System.Collections.IEnumerator CrossfadeToTrack(AudioClip newTrack)
    {
        targetVolume = 0f;
        
        while (ambientAudioSource.volume > 0.01f)
        {
            yield return null;
        }
        
        ambientAudioSource.clip = newTrack;
        ambientAudioSource.loop = true;
        ambientAudioSource.Play();
        
        targetVolume = ambientVolume;
    }
    
    public void ChangeArea(int newAreaIndex)
    {
        currentAreaIndex = newAreaIndex;
    }
    
    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        targetVolume = ambientVolume;
    }
    
    public void StopAmbient()
    {
        targetVolume = 0f;
    }
    
    public void ResumeAmbient()
    {
        targetVolume = ambientVolume;
    }
}