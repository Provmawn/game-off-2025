using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Ambient Music")]
    public AudioClip[] ambientTracks; // Array of ambient music tracks
    public AudioSource ambientAudioSource; // For ambient background music
    public float ambientVolume = 0.3f;
    public float fadeSpeed = 2f; // Speed of fade in/out transitions
    
    [Header("Area Management")]
    public int currentAreaIndex = 0; // Which ambient track to play (0-based)
    
    private int lastAreaIndex = -1;
    private float targetVolume = 0f;
    
    void Start()
    {
        if (ambientAudioSource == null)
        {
            Debug.LogWarning("AudioManager: No ambient AudioSource assigned!");
            return;
        }
        
        // Start playing the current area's ambient track
        PlayAmbientTrack(currentAreaIndex);
    }
    
    void Update()
    {
        // Handle area changes
        if (currentAreaIndex != lastAreaIndex)
        {
            PlayAmbientTrack(currentAreaIndex);
            lastAreaIndex = currentAreaIndex;
        }
        
        // Smooth volume transitions
        if (ambientAudioSource != null)
        {
            ambientAudioSource.volume = Mathf.Lerp(ambientAudioSource.volume, targetVolume, fadeSpeed * Time.deltaTime);
        }
    }
    
    public void PlayAmbientTrack(int areaIndex)
    {
        if (ambientAudioSource == null || ambientTracks == null || ambientTracks.Length == 0)
        {
            Debug.LogWarning("AudioManager: Cannot play ambient track - missing components!");
            return;
        }
        
        if (areaIndex < 0 || areaIndex >= ambientTracks.Length)
        {
            Debug.LogWarning($"AudioManager: Area index {areaIndex} out of range!");
            return;
        }
        
        AudioClip newTrack = ambientTracks[areaIndex];
        if (newTrack == null)
        {
            Debug.LogWarning($"AudioManager: No ambient track assigned for area {areaIndex}!");
            return;
        }
        
        // Fade out current track if playing different music
        if (ambientAudioSource.clip != newTrack)
        {
            StartCoroutine(CrossfadeToTrack(newTrack));
        }
        else if (!ambientAudioSource.isPlaying)
        {
            // Same track, just start playing
            ambientAudioSource.clip = newTrack;
            ambientAudioSource.loop = true;
            ambientAudioSource.Play();
            targetVolume = ambientVolume;
        }
    }
    
    private System.Collections.IEnumerator CrossfadeToTrack(AudioClip newTrack)
    {
        // Fade out current track
        targetVolume = 0f;
        
        // Wait for fade out
        while (ambientAudioSource.volume > 0.01f)
        {
            yield return null;
        }
        
        // Switch to new track
        ambientAudioSource.clip = newTrack;
        ambientAudioSource.loop = true;
        ambientAudioSource.Play();
        
        // Fade in new track
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