using SignalAudioManagerUnity.Communication;
using UnityEngine;

namespace SignalAudioManagerUnity.Components
{
    public class PlaySFX : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private string sfxID = "DefaultSFX";
        [SerializeField] private bool playAtObjectPosition = true;

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            if (playAtObjectPosition)
            {
                Signal.PlaySFX(sfxID, transform.position);
            }
            else
            {
                Signal.PlaySFX(sfxID);
            }
        }
    }
}