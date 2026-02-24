/* ==============================================================================
 * CLASS: SoundManager
 * DESCRIPTION: The core audio engine. Listens to the AudioEventChannel, manages 
 * object pools for SFX/UI, handles music crossfading, and applies dynamic 
 * multipliers to the base AudioEntry settings defined in the Dashboard.
 * ==============================================================================*/

using System.Collections;
using System.Collections.Generic;
using SignalAudioManagerUnity.Communication;
using SignalAudioManagerUnity.Data;
using UnityEngine;

namespace SignalAudioManagerUnity.Core
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [SerializeField] private SoundManagerSO _config;

        private Queue<PooledAudioSource> _sfxPool = new Queue<PooledAudioSource>();
        private Queue<PooledAudioSource> _uiPool = new Queue<PooledAudioSource>();
        private List<AudioSource> _musicSources = new List<AudioSource>();
        private AudioSource _activeMusicSource;
        private GameObject _audioHost;

        private readonly Dictionary<string, AudioEntry> _musicClips = new Dictionary<string, AudioEntry>();
        private readonly Dictionary<string, AudioEntry> _sfxClips = new Dictionary<string, AudioEntry>();

        private readonly Dictionary<long, PooledAudioSource> _activeLoopingInstances = new Dictionary<long, PooledAudioSource>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnEnable()
        {
            if (Instance != null && Instance != this) return;

            AudioEventChannel.OnAudioPlayRequested += PlayAudio;
            AudioEventChannel.OnStopMusicRequested += StopAllMusic;
            AudioEventChannel.OnPauseMusicRequested += PauseMusic;
            AudioEventChannel.OnResumeMusicRequested += ResumeMusic;
            AudioEventChannel.OnStopInstanceRequested += StopInstance;
            AudioEventChannel.OnSetGroupVolume += HandleSetGroupVolume;
        }

        private void OnDisable()
        {
            if (Instance != null && Instance != this) return;

            AudioEventChannel.OnAudioPlayRequested -= PlayAudio;
            AudioEventChannel.OnStopMusicRequested -= StopAllMusic;
            AudioEventChannel.OnPauseMusicRequested -= PauseMusic;
            AudioEventChannel.OnResumeMusicRequested -= ResumeMusic;
            AudioEventChannel.OnStopInstanceRequested -= StopInstance;
            AudioEventChannel.OnSetGroupVolume -= HandleSetGroupVolume;
        }

        // ==========================================
        // INITIALIZATION
        // ==========================================
        private void Initialize()
        {
            if (_config == null) return;
            LoadAudioClips();
            CreateAudioHost();
            CreateMusicSources();
            CreateSFXPools();
            LoadVolumeSettings();
        }

        private void LoadAudioClips()
        {
            foreach (var entry in _config.musicDatabase)
            {
                entry.audioClips.RemoveAll(clip => clip == null);
                
                if (entry.audioClips.Count > 0 && !string.IsNullOrEmpty(entry.audioID))
                {
                    _musicClips[entry.audioID.ToLowerInvariant()] = entry;
                }
            }
            foreach (var entry in _config.sfxDatabase)
            {
                entry.audioClips.RemoveAll(clip => clip == null);
                
                if (entry.audioClips.Count > 0 && !string.IsNullOrEmpty(entry.audioID))
                {
                    _sfxClips[entry.audioID.ToLowerInvariant()] = entry;
                }
            }
        }

        private void CreateAudioHost()
        {
            _audioHost = new GameObject("AudioHost");
            _audioHost.transform.SetParent(this.transform);
        }

        private void CreateMusicSources()
        {
            for (int i = 0; i < _config.maxSimultaneousMusic; i++)
            {
                var sourceGO = new GameObject($"MusicSource_{i}");
                sourceGO.transform.SetParent(_audioHost.transform);
                var source = sourceGO.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = _config.audioMixer.FindMatchingGroups("Music")[0];
                _musicSources.Add(source);
            }
        }

        private void CreateSFXPools()
        {
            if (_config.sfxPrefab == null) return;
            for (int i = 0; i < _config.sfxPoolSize; i++)
            {
                CreatePoolableSource(_sfxPool, "SFX");
            }
            for (int i = 0; i < 10; i++) // Smaller pool for UI
            {
                CreatePoolableSource(_uiPool, "UI");
            }
        }

        private void CreatePoolableSource(Queue<PooledAudioSource> pool, string mixerGroup)
        {
            var instance = Instantiate(_config.sfxPrefab, _audioHost.transform);
            instance.name = $"{mixerGroup}AudioSource_{pool.Count}";
            var audioSource = instance.GetComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = _config.audioMixer.FindMatchingGroups(mixerGroup)[0];
            instance.SetActive(false);
            var pooledSource = instance.GetComponent<PooledAudioSource>();
            pooledSource.Configure(source => ReturnToPool(source, pool));
            pool.Enqueue(pooledSource);
        }

        // ==========================================
        // EVENT ROUTING
        // ==========================================
        private void PlayAudio(AudioClipConfig config)
        {
            if (config == null) return;
            
            switch (config.Category)
            {
                case AudioCategory.Music: PlayMusic(config); break;
                case AudioCategory.SFX: PlaySFX(config); break;
                case AudioCategory.UI: PlayUISound(config); break;
            }
        }

        // ==========================================
        // PLAYBACK LOGIC
        // ==========================================
        private void PlayMusic(AudioClipConfig config)
        {
            if (!_musicClips.TryGetValue(config.AudioID.ToLowerInvariant(), out var entry))
            {
                Debug.LogWarning($"[Signal Audio] Music ID '{config.AudioID}' not found! Check your spelling or the Signal Dashboard.");
                return;
            }
            
            AudioClip clipToPlay = entry.GetRandomClip();
            if (clipToPlay == null) return;

            var musicSource = GetAvailableMusicSource();
            StartCoroutine(CrossfadeMusic(musicSource, clipToPlay, config, entry));
        }

        private void PlaySFX(AudioClipConfig config)
        {
            if (!_musicClips.TryGetValue(config.AudioID.ToLowerInvariant(), out var entry))
            {
                Debug.LogWarning($"[Signal Audio] Music ID '{config.AudioID}' not found! Check your spelling or the Signal Dashboard.");
                return;
            }

            AudioClip clipToPlay = entry.GetRandomClip();
            if (clipToPlay == null) return;
            
            float finalVolume = entry.GetRandomVolume() * config.VolumeMultiplier;
            float finalPitch = entry.GetRandomPitch() * config.PitchMultiplier;

            var pooledSource = GetAvailableSourceFromPool(_sfxPool);

            if (config.TargetTransform != null)
            {
                pooledSource.transform.SetParent(config.TargetTransform);
                pooledSource.transform.localPosition = Vector3.zero;
            }
            else
            {
                pooledSource.transform.SetParent(_audioHost.transform);
                pooledSource.transform.position = config.Position;
            }

            pooledSource.GetComponent<AudioSource>().spatialBlend = (config.TargetTransform != null || config.Position != Vector3.zero) ? 1f : 0f;

            if (config.Delay > 0f)
            {
                StartCoroutine(PlayPooledDelayed(pooledSource, clipToPlay, finalVolume, finalPitch, config.Loop, false, config.Delay));
            }
            else
            {
                pooledSource.Play(clipToPlay, finalVolume, finalPitch, config.Loop, false);
            }

            if (config.Loop && config.InstanceID != 0)
            {
                _activeLoopingInstances[config.InstanceID] = pooledSource;
            }
        }

        private void PlayUISound(AudioClipConfig config)
        {
            if (!_musicClips.TryGetValue(config.AudioID.ToLowerInvariant(), out var entry))
            {
                Debug.LogWarning($"[Signal Audio] Music ID '{config.AudioID}' not found! Check your spelling or the Signal Dashboard.");
                return;
            }

            AudioClip clipToPlay = entry.GetRandomClip();
            if (clipToPlay == null) return;

            float finalVolume = entry.GetRandomVolume() * config.VolumeMultiplier;
            float finalPitch = entry.GetRandomPitch() * config.PitchMultiplier;

            var pooledSource = GetAvailableSourceFromPool(_uiPool);
            pooledSource.transform.SetParent(_audioHost.transform);

            if (config.Delay > 0f)
            {
                StartCoroutine(PlayPooledDelayed(pooledSource, clipToPlay, finalVolume, finalPitch, false, true, config.Delay));
            }
            else
            {
                pooledSource.Play(clipToPlay, finalVolume, finalPitch, false, true);
            }
        }

        private IEnumerator PlayPooledDelayed(PooledAudioSource source, AudioClip clip, float vol, float pitch, bool loop, bool isUI, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (source != null)
            {
                source.Play(clip, vol, pitch, loop, isUI);
            }
        }

        private void StopInstance(long instanceId)
        {
            if (_activeLoopingInstances.TryGetValue(instanceId, out PooledAudioSource source))
            {
                source.Stop();
                _activeLoopingInstances.Remove(instanceId);
            }
        }

        // ==========================================
        // UTILITY, FADING & POOLING
        // ==========================================
        private PooledAudioSource GetAvailableSourceFromPool(Queue<PooledAudioSource> pool)
        {
            if (pool.Count == 0)
            {
                string group = pool == _sfxPool ? "SFX" : "UI";
                CreatePoolableSource(pool, group);
            }
            return pool.Dequeue();
        }

        private void ReturnToPool(PooledAudioSource source, Queue<PooledAudioSource> pool)
        {
            source.gameObject.SetActive(false);
            source.transform.SetParent(_audioHost.transform, true);
            pool.Enqueue(source);
        }

        private IEnumerator CrossfadeMusic(AudioSource newSource, AudioClip newClip, AudioClipConfig config, AudioEntry entry)
        {
            if (_activeMusicSource != null && _activeMusicSource.isPlaying)
            {
                yield return StartCoroutine(FadeSource(_activeMusicSource, 0f, config.FadeDuration));
                _activeMusicSource.Stop();
            }

            float finalVolume = entry.GetRandomVolume() * config.VolumeMultiplier;
            float finalPitch = entry.GetRandomPitch() * config.PitchMultiplier;

            _activeMusicSource = newSource;
            _activeMusicSource.clip = newClip;
            _activeMusicSource.volume = 0;
            _activeMusicSource.pitch = finalPitch;
            _activeMusicSource.loop = config.Loop;

            if (config.Delay > 0f)
            {
                _activeMusicSource.PlayDelayed(config.Delay);
                yield return new WaitForSeconds(config.Delay);
            }
            else
            {
                _activeMusicSource.Play();
            }
            
            yield return StartCoroutine(FadeSource(_activeMusicSource, finalVolume, config.FadeDuration));
        }

        private IEnumerator FadeSource(AudioSource source, float targetVolume, float duration)
        {
            if (source == null || duration <= 0)
            {
                if (source != null) source.volume = targetVolume;
                yield break;
            }
            float startVolume = source.volume;
            float time = 0;
            while (time < duration)
            {
                if (source == null) yield break;
                source.volume = Mathf.Lerp(startVolume, targetVolume, time / duration);
                time += Time.deltaTime;
                yield return null;
            }
            if (source != null) source.volume = targetVolume;
        }

        private AudioSource GetAvailableMusicSource()
        {
            foreach (var source in _musicSources)
            {
                if (!source.isPlaying) return source;
            }
            return _musicSources[0];
        }

        // ==========================================
        // GLOBAL CONTROLS & MIXER
        // ==========================================
        public void StopAllMusic()
        {
            foreach (var source in _musicSources)
            {
                if (source.isPlaying) StartCoroutine(FadeSource(source, 0f, 1f));
            }
            _activeMusicSource = null;
        }

        public void PauseMusic()
        {
            if (_activeMusicSource != null) _activeMusicSource.Pause();
        }

        public void ResumeMusic()
        {
            if (_activeMusicSource != null) _activeMusicSource.UnPause();
        }

        private void LoadVolumeSettings()
        {
            SetMasterVolume(PlayerPrefs.GetFloat(_config.masterVolumeParam, 1f));
            SetMusicVolume(PlayerPrefs.GetFloat(_config.musicVolumeParam, 1f));
            SetSFXVolume(PlayerPrefs.GetFloat(_config.sfxVolumeParam, 1f));
            SetUIVolume(PlayerPrefs.GetFloat(_config.uiVolumeParam, 1f));
        }
        
        private void HandleSetGroupVolume(AudioCategory category, float normalizedVolume)
        {
            switch (category)
            {
                case AudioCategory.Master: SetMasterVolume(normalizedVolume); break;
                case AudioCategory.Music: SetMusicVolume(normalizedVolume); break;
                case AudioCategory.SFX: SetSFXVolume(normalizedVolume); break;
                case AudioCategory.UI: SetUIVolume(normalizedVolume); break;
            }
        }

        public void SetMasterVolume(float volume) => SetVolume(_config.masterVolumeParam, volume);
        public void SetMusicVolume(float volume) => SetVolume(_config.musicVolumeParam, volume);
        public void SetSFXVolume(float volume) => SetVolume(_config.sfxVolumeParam, volume);
        public void SetUIVolume(float volume) => SetVolume(_config.uiVolumeParam, volume);

        private void SetVolume(string parameter, float volume)
        {
            _config.audioMixer.SetFloat(parameter, Mathf.Log10(volume > 0 ? volume : 0.0001f) * 20f);
            PlayerPrefs.SetFloat(parameter, volume);
        }
    }
}