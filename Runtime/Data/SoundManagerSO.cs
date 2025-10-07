using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;


namespace SignalAudioManager
{
    [CreateAssetMenu(fileName = "SoundManagerSO", menuName = "Signal Audio Manager/Configuration")]
    public class SoundManagerSO : ScriptableObject
    {
        [Header("Audio Mixer Configuration")]
        public AudioMixer audioMixer;
        public string masterVolumeParam = "MasterVolume";
        public string musicVolumeParam = "MusicVolume";
        public string sfxVolumeParam = "SFXVolume";
        public string uiVolumeParam = "UIVolume";

        [Header("Asset References")]
        public GameObject sfxPrefab;

        [Header("General Settings")]
        [Range(1, 10)]
        public int maxSimultaneousMusic = 3;
        [Range(1, 100)]
        public int sfxPoolSize = 20;

        [Header("Audio Databases")]
        public List<AudioEntry> musicDatabase = new List<AudioEntry>();
        public List<AudioEntry> sfxDatabase = new List<AudioEntry>();
    }

}