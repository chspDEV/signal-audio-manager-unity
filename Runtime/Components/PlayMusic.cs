using SignalAudioManagerUnity.Communication;
using UnityEngine;

namespace SignalAudioManagerUnity.Components
{
    public class PlayMusic : MonoBehaviour
    {
        [SerializeField] private bool playOnStart = false;
        [SerializeField] private string musicID = "DefaultMusic";
        [SerializeField] private float fadeDuration = 2f;

        private void Start()
        {
            if (playOnStart)
            {
                Play();
            }
        }

        public void Play()
        {
            Signal.PlayMusic(musicID, fadeDuration);
        }
    }
}