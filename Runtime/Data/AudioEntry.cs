/* ==============================================================================
 * CLASS: AudioEntry
 * DESCRIPTION: Represents a single audio event in the database. Contains variations 
 * (clips, volume, pitch) and helper methods to retrieve randomized runtime values.
 * ==============================================================================*/
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SignalAudioManagerUnity.Data
{
    [Serializable]
    public class AudioEntry
    {
        [Tooltip("The unique identifier used to call this audio event via code or signals.")]
        public string audioID;

        [Space(5)]
        [Tooltip("List of audio variations. One clip will be selected randomly when played.")]
        public List<AudioClip> audioClips = new List<AudioClip>();

        [Space(5f)]
        [Header("Variation Settings")]
        [Space(2f)]
        [Tooltip("Minimum volume applied when this audio plays. 1 is default.")]
        [Range(0f, 2f)] 
        public float minVolume = 1f;
        
        [Tooltip("Maximum volume applied when this audio plays. 1 is default.")]
        [Range(0f, 2f)] 
        public float maxVolume = 1f;

        [Space(5)]
        
        [Tooltip("Minimum pitch (speed/tone) applied. 1 is normal speed, lower is deeper.")]
        [Range(0.1f, 3f)] 
        public float minPitch = 1f;
        
        [Tooltip("Maximum pitch (speed/tone) applied. 1 is normal speed, higher is chipmunk.")]
        [Range(0.1f, 3f)] 
        public float maxPitch = 1f;

        [Space(5f)]
        [Header("3D Spatial Settings")]
        [Space(2f)]
        
        [Tooltip("0 = 2D (no panning), 1 = 3D (fully spatialized based on position).")]
        [Range(0f, 1f)]
        public float spatialBlend = 1f;
        
        [Tooltip("How much the pitch shifts when moving fast. Default is 0 (no weird pitch bending).")]
        [Range(0f, 5f)]
        public float dopplerLevel = 0f;
        
        [Tooltip("How much the sound spreads across speakers. 0 = tight point source, 360 = fully wrapped around.")]
        [Range(0, 360)]
        public int spread = 0;
        
        [Tooltip("Distance at which the sound starts getting quieter.")]
        public float minDistance = 1f;
        
        [Tooltip("Distance at which the sound can barely be heard anymore.")]
        public float maxDistance = 500f;

        // ==========================================
        // HELPER METHODS (Functional Improvements)
        // ==========================================

        /// <summary>
        /// Returns a random AudioClip from the variations list. Returns null if empty.
        /// </summary>
        public AudioClip GetRandomClip()
        {
            if (audioClips == null || audioClips.Count == 0) return null;
            return audioClips[Random.Range(0, audioClips.Count)];
        }

        /// <summary>
        /// Returns a random volume between the configured min and max bounds.
        /// </summary>
        public float GetRandomVolume()
        {
            return Random.Range(minVolume, maxVolume);
        }

        /// <summary>
        /// Returns a random pitch between the configured min and max bounds.
        /// </summary>
        public float GetRandomPitch()
        {
            return Random.Range(minPitch, maxPitch);
        }
    }
}