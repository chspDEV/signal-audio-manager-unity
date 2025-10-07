using UnityEngine;
using SignalAudioManager;
public static class Signal
{
    private static long _nextInstanceId = 1;

    #region Sound Effects (SFX)
    public static void PlaySFX(string audioID)
    {
        AudioEventChannel.RaisePlayAudio(new AudioClipConfig { Category = AudioCategory.SFX, AudioID = audioID });
    }

    public static void PlaySFX(string audioID, Vector3 position)
    {
        AudioEventChannel.RaisePlayAudio(new AudioClipConfig { Category = AudioCategory.SFX, AudioID = audioID, Position = position });
    }

    public static void PlaySFX(string audioID, Transform target)
    {
        AudioEventChannel.RaisePlayAudio(new AudioClipConfig { Category = AudioCategory.SFX, AudioID = audioID, TargetTransform = target });
    }

    public static long PlaySFXLoop(string audioID, Transform target)
    {
        long id = _nextInstanceId++;
        var config = new AudioClipConfig
        {
            Category = AudioCategory.SFX,
            AudioID = audioID,
            TargetTransform = target,
            Loop = true,
            InstanceID = id
        };
        AudioEventChannel.RaisePlayAudio(config);
        return id;
    }
    #endregion

    #region UI Sounds
    public static void PlayUI(string audioID)
    {
        AudioEventChannel.RaisePlayAudio(new AudioClipConfig { Category = AudioCategory.UI, AudioID = audioID });
    }
    #endregion

    #region Music
    public static void PlayMusic(string audioID, float fadeDuration = 2.0f)
    {
        var config = new AudioClipConfig
        {
            Category = AudioCategory.Music,
            AudioID = audioID,
            Loop = true,
            FadeDuration = fadeDuration
        };
        AudioEventChannel.RaisePlayAudio(config);
    }
    #endregion

    #region Global Audio Control
    public static void StopMusic() => AudioEventChannel.RaiseStopMusic();
    public static void PauseMusic() => AudioEventChannel.RaisePauseMusic();
    public static void ResumeMusic() => AudioEventChannel.RaiseResumeMusic();
    public static void StopInstance(long instanceId)
    {
        if (instanceId > 0)
        {
            AudioEventChannel.RaiseStopInstance(instanceId);
        }
    }
    #endregion
}