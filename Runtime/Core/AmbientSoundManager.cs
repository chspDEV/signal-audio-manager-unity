using System.Collections;
using System.Collections.Generic;
using SignalAudioManagerUnity.Communication;
using SignalAudioManagerUnity.Data;
using UnityEngine;

namespace SignalAudioManagerUnity.Core
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

        public void ChangeAmbient(AmbientTypeSO newAmbient, bool forceChange = false, float overrideFadeOut = -1f, float overrideFadeIn = -1f)
        {
            if (currentAmbient == newAmbient && !forceChange) return;
            if (isTransitioning) return;

            if (_transitionCoroutine != null)
            {
                StopCoroutine(_transitionCoroutine);
            }
            _transitionCoroutine = StartCoroutine(TransitionToAmbient(newAmbient, overrideFadeOut, overrideFadeIn));
        }

        private IEnumerator TransitionToAmbient(AmbientTypeSO newAmbient, float overrideFadeOut, float overrideFadeIn)
        {
            isTransitioning = true;
            _configDict.TryGetValue(currentAmbient, out AmbientSoundConfig currentConfig);
            currentAmbient = newAmbient;

            if (newAmbient != null && _configDict.TryGetValue(newAmbient, out AmbientSoundConfig newConfig))
            {
                if (newConfig.audioIDs.Count > 0)
                {
                    float fadeInToUse = overrideFadeIn >= 0f ? overrideFadeIn : newConfig.fadeInDuration;
                    Signal.PlayMusic(newConfig.audioIDs[0], fadeInToUse);
                }
            }
            else
            {
                Signal.StopMusic();
            }

            float fadeOutTime = overrideFadeOut >= 0f ? overrideFadeOut : (currentConfig?.fadeOutDuration ?? 0f);
            float fadeInTime = overrideFadeIn >= 0f ? overrideFadeIn : ((newAmbient != null) ? _configDict[newAmbient].fadeInDuration : 0f);
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