using System.Collections.Generic;
using UnityEngine;

namespace SignalAudioManagerUnity.Data
{
    [System.Serializable]
    public class AudioEntry
    {
        public string audioID;
        public List<AudioClip> audioClips = new List<AudioClip>();

        [Header("Variation Settings")]
        [Range(0f, 2f)]
        public float minVolume = 1f;
        [Range(0f, 2f)]
        public float maxVolume = 1f;
        [Range(-3f, 3f)]
        public float minPitch = 1f;
        [Range(-3f, 3f)]
        public float maxPitch = 1f;
    }
}