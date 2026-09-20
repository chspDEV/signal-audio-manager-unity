/* ==============================================================================
 * CLASS: SurfaceSoundMapSO
 * DESCRIPTION: ScriptableObject that maps Physics Materials and Terrain texture
 * indices to Audio IDs for the FootstepController. Create one asset per
 * environment/character via the Asset menu.
 * ==============================================================================*/

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SignalAudioManagerUnity.Data
{
    [CreateAssetMenu(fileName = "SurfaceSoundMap", menuName = "Signal Audio/Surface Sound Map")]
    public class SurfaceSoundMapSO : ScriptableObject
    {
        // ==========================================
        // DATA STRUCTURES
        // ==========================================

        [Serializable]
        public class PhysicsMaterialEntry
        {
            [Tooltip("The Physics Material to detect. Leave null to match surfaces with no material assigned.")]
            public PhysicMaterial physicsMaterial;

            [Tooltip("The Audio ID to play when the player steps on this material (must exist in the Signal Dashboard).")]
            public string audioID;
        }

        [Serializable]
        public class TerrainTextureEntry
        {
            [Tooltip("Index of the TerrainLayer in the Terrain's Terrain Layers list (0 = first layer, 1 = second, etc.).")]
            public int terrainTextureIndex;

            [Tooltip("The Audio ID to play when this terrain texture is dominant underfoot.")]
            public string audioID;
        }

        // ==========================================
        // INSPECTOR FIELDS
        // ==========================================

        [Header("Physics Material Mapping")]
        [Tooltip("Maps Physics Materials to Audio IDs. Used for regular colliders (cubes, capsules, etc.).")]
        public List<PhysicsMaterialEntry> physicsMaterialMap = new List<PhysicsMaterialEntry>();

        [Header("Terrain Texture Mapping")]
        [Tooltip("Maps Terrain Layer indices to Audio IDs. Index 0 = first layer in the Terrain's Terrain Layers list.")]
        public List<TerrainTextureEntry> terrainTextureMap = new List<TerrainTextureEntry>();

        [Header("Fallback")]
        [Tooltip("Audio ID played when no matching surface is found. Make sure this ID exists in the Signal Dashboard.")]
        public string defaultAudioID = "footstep_default";

        // ==========================================
        // LOOKUP METHODS
        // ==========================================

        /// <summary>
        /// Returns the Audio ID for a given Physics Material. Falls back to defaultAudioID if not found.
        /// </summary>
        public string GetAudioIDForPhysicsMaterial(PhysicMaterial material)
        {
            foreach (var entry in physicsMaterialMap)
            {
                if (entry.physicsMaterial == material)
                    return entry.audioID;
            }
            return defaultAudioID;
        }

        /// <summary>
        /// Returns the Audio ID for a given Terrain texture index. Falls back to defaultAudioID if not found.
        /// </summary>
        public string GetAudioIDForTerrainIndex(int index)
        {
            foreach (var entry in terrainTextureMap)
            {
                if (entry.terrainTextureIndex == index)
                    return entry.audioID;
            }
            return defaultAudioID;
        }
    }
}
