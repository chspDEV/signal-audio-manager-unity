/* ==============================================================================
 * FILE: AudioEventChannel (Includes AudioClipConfig and AudioCategory)
 * DESCRIPTION: The core communication bridge (Event Bus) between the Signal API 
 * and the SoundManager. Decouples the callers from the audio engine.
 * ==============================================================================*/

using UnityEngine;

namespace SignalAudioManagerUnity.Communication
{
    public static class AudioEventChannel
    {
        // ==========================================
        // EVENTS
        // ==========================================
        
        // Event for requesting audio playback
        public delegate void PlayAudioAction(AudioClipConfig config);
        public static event PlayAudioAction OnAudioPlayRequested;

        // Events for controlling music playback
        public delegate void AudioControlAction();
        public static event AudioControlAction OnStopMusicRequested;
        public static event AudioControlAction OnPauseMusicRequested;
        public static event AudioControlAction OnResumeMusicRequested;

        // Event for stopping a specific instance of a sound
        public delegate void StopInstanceAction(long instanceId);
        public static event StopInstanceAction OnStopInstanceRequested;
        
        public static event System.Action<AudioCategory, float> OnSetGroupVolume;

        // ==========================================
        // RAISE METHODS
        // ==========================================
        
        public static void RaisePlayAudio(AudioClipConfig config) => OnAudioPlayRequested?.Invoke(config);
        public static void RaiseStopMusic() => OnStopMusicRequested?.Invoke();
        public static void RaisePauseMusic() => OnPauseMusicRequested?.Invoke();
        public static void RaiseResumeMusic() => OnResumeMusicRequested?.Invoke();
        public static void RaiseStopInstance(long instanceId) => OnStopInstanceRequested?.Invoke(instanceId);
        
        public static void RaiseSetGroupVolume(AudioCategory category, float normalizedVolume) 
        {
            OnSetGroupVolume?.Invoke(category, normalizedVolume);
        }
    }

    public enum AudioCategory
    {
        Master,
        Music,
        SFX,
        UI
    }

    public class AudioClipConfig
    {
        public AudioCategory Category { get; set; }
        public string AudioID { get; set; }
        public Vector3 Position { get; set; } = Vector3.zero;
        public Transform TargetTransform { get; set; } = null;
        
        public float VolumeMultiplier { get; set; } = 1f; 
        public float PitchMultiplier { get; set; } = 1f; 
        public float Delay { get; set; } = 0f; 
        
        public bool Loop { get; set; } = false;
        public float FadeDuration { get; set; } = 0f;
        public long InstanceID { get; set; } = 0;
    }
}