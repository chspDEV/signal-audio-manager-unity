
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SignalAudioManager
{
    public class AmbientSoundManager : MonoBehaviour
    {
        public static AmbientSoundManager Instance { get; private set; }

        [SerializeField] private List<AmbientSoundConfig> ambientConfigs = new List<AmbientSoundConfig>();
        [SerializeField] private AmbientTypeSO currentAmbient = null;
        private bool isTransitioning = false;
        private Dictionary<AmbientTypeSO, AmbientSoundConfig> _configDict;
        private Coroutine _transitionCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeConfigs();
        }

        private void InitializeConfigs()
        {
            _configDict = new Dictionary<AmbientTypeSO, AmbientSoundConfig>();
            foreach (var config in ambientConfigs)
            {
                if (!_configDict.ContainsKey(config.ambientType))
                {
                    _configDict.Add(config.ambientType, config);
                }
            }
        }

        public void ChangeAmbient(AmbientTypeSO newAmbient, bool forceChange = false)
        {
            if (currentAmbient == newAmbient && !forceChange) return;
            if (isTransitioning) return;

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            _transitionCoroutine = StartCoroutine(TransitionToAmbient(newAmbient));
        }

        private IEnumerator TransitionToAmbient(AmbientTypeSO newAmbient)
        {
            isTransitioning = true;
            _configDict.TryGetValue(currentAmbient, out AmbientSoundConfig currentConfig);
            currentAmbient = newAmbient;

            if (newAmbient != null && _configDict.TryGetValue(newAmbient, out AmbientSoundConfig newConfig))
            {
                if (newConfig.audioIDs.Count > 0)
                {
                    Signal.PlayMusic(newConfig.audioIDs[0], newConfig.fadeInDuration);
                }
            }
            else
            {
                Signal.StopMusic();
            }

            float fadeOutTime = currentConfig?.fadeOutDuration ?? 0f;
            float fadeInTime = (newAmbient != null) ? _configDict[newAmbient].fadeInDuration : 0f;
            yield return new WaitForSeconds(Mathf.Max(fadeOutTime, fadeInTime));

            isTransitioning = false;
            _transitionCoroutine = null;
        }

        public void StopAmbient()
        {
            ChangeAmbient(null);
        }
    }
}