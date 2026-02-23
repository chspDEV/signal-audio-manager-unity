using SignalAudioManagerUnity.Core;
using SignalAudioManagerUnity.Data;
using UnityEngine;

namespace SignalAudioManagerUnity.Components
{
    [RequireComponent(typeof(Collider))]
    public class AmbientTrigger : MonoBehaviour
    {
        [Header("Trigger Configuration")]
        [Tooltip("The ambient type to transition to when entering this trigger.")]
        [SerializeField] private AmbientTypeSO ambientType = null;

        [Tooltip("Should this trigger activate when the player enter?")]
        [SerializeField] private bool triggerOnEnter = false;

        [Tooltip("Should this trigger activate when the player exits?")]
        [SerializeField] private bool triggerOnExit = false;

        [Tooltip("The ambient type to transition to upon exiting. Assign null for silence.")]
        [SerializeField] private AmbientTypeSO exitAmbientType = null;

        [Header("Player Settings")]
        [SerializeField] private string playerTag = "Player";

        private void Start()
        {
            var col = GetComponent<Collider>();
            if (!col.isTrigger)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerOnEnter && other.CompareTag(playerTag))
            {
                AmbientSoundManager.Instance.ChangeAmbient(ambientType);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (triggerOnExit && other.CompareTag(playerTag))
            {
                AmbientSoundManager.Instance.ChangeAmbient(exitAmbientType);
            }
        }
    }
}