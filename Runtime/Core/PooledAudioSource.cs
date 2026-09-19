using System;
using System.Collections;
using UnityEngine;

namespace SignalAudioManagerUnity.Core
{

    [RequireComponent(typeof(AudioSource))]
    public class PooledAudioSource : MonoBehaviour
    {
        private AudioSource _audioSource;
        private Action<PooledAudioSource> _returnToPoolAction;
        private Coroutine _returnCoroutine;
        
        public long InstanceID { get; set; }
        private bool _isPaused;

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;
        }

        public void Configure(Action<PooledAudioSource> returnAction)
        {
            _returnToPoolAction = returnAction;
        }

        public void Play(AudioClip clip, float volumeScale, float pitch = 1f, bool loop = false, bool isUISound = false)
        {
            _isPaused = false;
            gameObject.SetActive(true);
            _audioSource.pitch = pitch;
            _audioSource.loop = loop;
            _audioSource.spatialBlend = isUISound ? 0f : 1f;
            _audioSource.clip = clip;
            _audioSource.volume = volumeScale;
            _audioSource.Play();

            if (!loop)
            {
                if (_returnCoroutine != null) StopCoroutine(_returnCoroutine);
                float delay = clip.length / Mathf.Abs(pitch);
                _returnCoroutine = StartCoroutine(isUISound ? ReturnToPoolAfterDelayRealtime(delay) : ReturnToPoolAfterDelay(delay));
            }
        }

        public void Stop()
        {
            _audioSource.Stop();
            if (_returnCoroutine != null)
            {
                StopCoroutine(_returnCoroutine);
                _returnCoroutine = null;
            }
            _returnToPoolAction?.Invoke(this);
        }

        public void Pause()
        {
            _isPaused = true;
            _audioSource.Pause();
        }

        public void Resume()
        {
            if (!_isPaused) return;
            _isPaused = false;
            _audioSource.UnPause();
        }

        private IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                if (!_isPaused) elapsed += Time.deltaTime;
                yield return null;
            }
            _returnCoroutine = null;
            _returnToPoolAction?.Invoke(this);
        }

        private IEnumerator ReturnToPoolAfterDelayRealtime(float delay)
        {
            float elapsed = 0f;
            while (elapsed < delay)
            {
                if (!_isPaused) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            _returnCoroutine = null;
            _returnToPoolAction?.Invoke(this);
        }
    }
}