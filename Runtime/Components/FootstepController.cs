/* ==============================================================================
 * CLASS: FootstepController
 * DESCRIPTION: A plug-and-play component that fires Signal audio events based on
 * the surface detected beneath the character. Supports two activation modes:
 *
 *   — Animation Event: Call PlayFootstep() from an Animation Event on the exact
 *     frame the foot contacts the ground. Most precise option.
 *
 *   — Velocity Based: Automatically triggers footsteps when the character moves
 *     above a speed threshold, with a configurable cooldown between steps.
 *
 * Surface detection uses a downward Raycast and checks for:
 *   1. Terrain → reads the dominant TerrainLayer texture at the hit point.
 *   2. PhysicsMaterial → reads the sharedMaterial of the Collider hit.
 *
 * Assign a SurfaceSoundMapSO in the Inspector to map surfaces to Audio IDs.
 * ==============================================================================*/

using SignalAudioManagerUnity.Communication;
using SignalAudioManagerUnity.Data;
using UnityEngine;

namespace SignalAudioManagerUnity.Components
{
    public class FootstepController : MonoBehaviour
    {
        // ==========================================
        // ENUMS
        // ==========================================

        public enum FootstepMode
        {
            AnimationEvent,
            VelocityBased,
            Both
        }

        // ==========================================
        // INSPECTOR FIELDS
        // ==========================================

        [Header("Surface Detection")]
        [Tooltip("The ScriptableObject that maps Physics Materials and Terrain textures to Audio IDs.")]
        [SerializeField] private SurfaceSoundMapSO surfaceSoundMap;

        [Tooltip("How far down to raycast to detect the ground surface.")]
        [SerializeField] private float raycastDistance = 1.2f;

        [Tooltip("Which layers count as ground. Set this to your Ground layer to avoid hitting the character's own collider.")]
        [SerializeField] private LayerMask groundLayer = -1;

        [Header("Footstep Mode")]
        [Tooltip("AnimationEvent: call PlayFootstep() from an animation event.\n" +
                 "VelocityBased: triggers automatically when moving above the speed threshold.\n" +
                 "Both: supports both methods simultaneously.")]
        [SerializeField] private FootstepMode mode = FootstepMode.Both;

        [Header("Velocity-Based Settings")]
        [Tooltip("Minimum time (seconds) between automatic footstep triggers.")]
        [SerializeField] private float stepCooldown = 0.4f;

        [Tooltip("Minimum movement speed required to trigger a footstep automatically.")]
        [SerializeField] private float minSpeedThreshold = 0.5f;

        // ==========================================
        // PRIVATE STATE
        // ==========================================

        private CharacterController _characterController;
        private Rigidbody _rigidbody;
        private float _stepTimer;

        // ==========================================
        // LIFECYCLE
        // ==========================================

        private void Awake()
        {
            // Grab optional locomotion components for velocity-based mode
            _characterController = GetComponent<CharacterController>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void Update()
        {
            if (mode == FootstepMode.AnimationEvent) return;

            float speed = GetCurrentSpeed();
            if (speed < minSpeedThreshold) return;

            _stepTimer -= Time.deltaTime;
            if (_stepTimer <= 0f)
            {
                _stepTimer = stepCooldown;
                PlayFootstep();
            }
        }

        // ==========================================
        // PUBLIC API
        // ==========================================

        /// <summary>
        /// Triggers a footstep sound based on the surface beneath the character.
        /// Call this from an Animation Event on the frame the foot hits the ground,
        /// or from any custom locomotion script.
        /// </summary>
        public void PlayFootstep()
        {
            if (surfaceSoundMap == null)
            {
                Debug.LogWarning("[Signal FootstepController] No SurfaceSoundMap assigned! Please assign one in the Inspector.");
                return;
            }

            string audioID = DetectSurface();
            if (!string.IsNullOrEmpty(audioID))
            {
                Signal.PlaySFX(audioID, transform);
            }
        }

        // ==========================================
        // SURFACE DETECTION
        // ==========================================

        private string DetectSurface()
        {
            if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance, groundLayer))
            {
                // No ground found — use fallback
                return surfaceSoundMap.defaultAudioID;
            }

            // Priority 1: Terrain (check dominant texture layer at hit point)
            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
            {
                int dominantIndex = GetDominantTerrainTextureIndex(terrain, hit.point);
                return surfaceSoundMap.GetAudioIDForTerrainIndex(dominantIndex);
            }

            // Priority 2: Physics Material on the collider
            PhysicMaterial material = hit.collider.sharedMaterial;
            return surfaceSoundMap.GetAudioIDForPhysicsMaterial(material);
        }

        /// <summary>
        /// Samples the Terrain's alphamap at the world position and returns the 
        /// index of the dominant (highest-weight) TerrainLayer.
        /// </summary>
        private int GetDominantTerrainTextureIndex(Terrain terrain, Vector3 worldPosition)
        {
            TerrainData data = terrain.terrainData;
            Vector3 localPos = worldPosition - terrain.transform.position;

            // Convert world position to alphamap coordinates
            int mapX = Mathf.Clamp(
                Mathf.RoundToInt(localPos.x / data.size.x * data.alphamapWidth),
                0, data.alphamapWidth - 1);
            int mapZ = Mathf.Clamp(
                Mathf.RoundToInt(localPos.z / data.size.z * data.alphamapHeight),
                0, data.alphamapHeight - 1);

            float[,,] alphaMap = data.GetAlphamaps(mapX, mapZ, 1, 1);

            int dominantIndex = 0;
            float dominantWeight = 0f;
            for (int i = 0; i < alphaMap.GetLength(2); i++)
            {
                if (alphaMap[0, 0, i] > dominantWeight)
                {
                    dominantWeight = alphaMap[0, 0, i];
                    dominantIndex = i;
                }
            }

            return dominantIndex;
        }

        // ==========================================
        // LOCOMOTION UTILITY
        // ==========================================

        private float GetCurrentSpeed()
        {
            if (_characterController != null)
                return _characterController.velocity.magnitude;

            if (_rigidbody != null)
                return _rigidbody.linearVelocity.magnitude;

            return 0f;
        }

        // ==========================================
        // EDITOR VISUALIZATION
        // ==========================================

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Draw the raycast line in yellow for easy Inspector debugging
            Gizmos.color = Color.yellow;
            Vector3 origin = transform.position;
            Vector3 end = origin + Vector3.down * raycastDistance;
            Gizmos.DrawLine(origin, end);
            Gizmos.DrawWireSphere(end, 0.06f);
        }
#endif
    }
}
