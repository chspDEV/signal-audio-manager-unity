using System.Collections.Generic;
using UnityEngine;

namespace SignalAudioManagerUnity.Data
{

    [System.Serializable]
    public class AmbientSoundConfig
    {
        [Tooltip("The ambient type this configuration represents.")]
        public AmbientTypeSO ambientType;

        public List<string> audioIDs = new();
        [Range(0f, 1f)]
        public float volume = 0.5f;
        [Range(-3f, 3f)]
        public float pitch = 1f;
        [Range(0.1f, 10f)]
        public float fadeInDuration = 2f;
        [Range(0.1f, 10f)]
        public float fadeOutDuration = 2f;
    }
}