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
            AudioEventChannel.OnAudioPlayRequested += PlayAudio;
            AudioEventChannel.OnStopMusicRequested += StopAllMusic;
            AudioEventChannel.OnPauseMusicRequested += PauseMusic;
            AudioEventChannel.OnResumeMusicRequested += ResumeMusic;
            AudioEventChannel.OnStopInstanceRequested += StopInstance;
        }

        private void OnDisable()
        {
            AudioEventChannel.OnAudioPlayRequested -= PlayAudio;
            AudioEventChannel.OnStopMusicRequested -= StopAllMusic;
            AudioEventChannel.OnPauseMusicRequested -= PauseMusic;
            AudioEventChannel.OnResumeMusicRequested -= ResumeMusic;
            AudioEventChannel.OnStopInstanceRequested -= StopInstance;
        }

        // Initialization and Setup
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
                if (entry.audioClips.Count > 0 && !string.IsNullOrEmpty(entry.audioID))
                {
                    _musicClips[entry.audioID.ToLowerInvariant()] = entry;
                }
            }
            foreach (var entry in _config.sfxDatabase)
            {
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

        // Event Handlers
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

        // Playback Logic
        private void PlayMusic(AudioClipConfig config)
        {
            if (!_musicClips.TryGetValue(config.AudioID.ToLowerInvariant(), out AudioEntry entry)) return;
            AudioClip clipToPlay = entry.audioClips[0];
            var musicSource = GetAvailableMusicSource();
            StartCoroutine(CrossfadeMusic(musicSource, clipToPlay, config));
        }

        private void PlaySFX(AudioClipConfig config)
        {
            if (!_sfxClips.TryGetValue(config.AudioID.ToLowerInvariant(), out AudioEntry entry) || entry.audioClips.Count == 0) return;

            AudioClip clipToPlay = entry.audioClips[Random.Range(0, entry.audioClips.Count)];
            float volume = Random.Range(entry.minVolume, entry.maxVolume) * config.VolumeScale;
            float pitch = Random.Range(entry.minPitch, entry.maxPitch) * config.Pitch;

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

            pooledSource.Play(clipToPlay, volume, pitch, config.Loop, false);

            if (config.Loop && config.InstanceID != 0)
            {
                _activeLoopingInstances[config.InstanceID] = pooledSource;
            }
        }

        private void PlayUISound(AudioClipConfig config)
        {
            if (!_sfxClips.TryGetValue(config.AudioID.ToLowerInvariant(), out AudioEntry entry) || entry.audioClips.Count == 0) return;

            AudioClip clipToPlay = entry.audioClips[0];
            var pooledSource = GetAvailableSourceFromPool(_uiPool);
            pooledSource.transform.SetParent(_audioHost.transform);
            pooledSource.Play(clipToPlay, config.VolumeScale, config.Pitch, false, true);
        }

        private void StopInstance(long instanceId)
        {
            if (_activeLoopingInstances.TryGetValue(instanceId, out PooledAudioSource source))
            {
                source.Stop();
                _activeLoopingInstances.Remove(instanceId);
            }
        }

        // Utility and Control Methods (Pooling, Fading, Volume)
        private PooledAudioSource GetAvailableSourceFromPool(Queue<PooledAudioSource> pool)
        {
            if (pool.Count == 0)
            {
                // Dynamically create more if pool runs out.
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

        private IEnumerator CrossfadeMusic(AudioSource newSource, AudioClip newClip, AudioClipConfig config)
        {
            if (_activeMusicSource != null && _activeMusicSource.isPlaying)
            {
                yield return StartCoroutine(FadeSource(_activeMusicSource, 0f, config.FadeDuration));
                _activeMusicSource.Stop();
            }
            _activeMusicSource = newSource;
            _activeMusicSource.clip = newClip;
            _activeMusicSource.volume = 0;
            _activeMusicSource.pitch = config.Pitch;
            _activeMusicSource.loop = config.Loop;
            _activeMusicSource.Play();
            yield return StartCoroutine(FadeSource(_activeMusicSource, config.VolumeScale, config.FadeDuration));
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