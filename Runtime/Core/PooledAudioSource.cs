using System;
using System.Collections;
using UnityEngine;

namespace SignalAudioManager
{

    [RequireComponent(typeof(AudioSource))]
    public class PooledAudioSource : MonoBehaviour
    {
        private AudioSource _audioSource;
        private Action<PooledAudioSource> _returnToPoolAction;
        private Coroutine _returnCoroutine;

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

        private IEnumerator ReturnToPoolAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            _returnCoroutine = null;
            _returnToPoolAction?.Invoke(this);
        }

        private IEnumerator ReturnToPoolAfterDelayRealtime(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _returnCoroutine = null;
            _returnToPoolAction?.Invoke(this);
        }
    }
}