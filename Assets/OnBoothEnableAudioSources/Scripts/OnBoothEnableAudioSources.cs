using UdonSharp;
using UnityEngine;

namespace OnBoothEnableAudioSources.Scripts
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class OnBoothEnableAudioSources : UdonSharpBehaviour
    {
        [SerializeField] private AudioSource[] _audioSources;

        public void _VketOnBoothEnter()
        {
            foreach (var audioSource in _audioSources)
            {
                if (audioSource != null)
                    audioSource.enabled = true;
            }
        }

        public void _VketOnBoothExit()
        {
            foreach (var audioSource in _audioSources)
            {
                if (audioSource != null)
                    audioSource.enabled = false;
            }
        }
    }
}