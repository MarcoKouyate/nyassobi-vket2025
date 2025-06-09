
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketVideoUrlTrigger : UdonSharpBehaviour
    {
        [SerializeField]
        private VRCUrl videoUrl;
        [SerializeField]
        private VketVideoPlayer vketVideoPlayer;
        [SerializeField]
        private bool isInteract = true;

        private void Start()
        {
            if (!isInteract)
                DisableInteractive = true;
        }

        private void PlayUrl()
        {
            vketVideoPlayer.VideoUrl = videoUrl;
            vketVideoPlayer._LoadVideo();
        }

        public override void Interact()
        {
            PlayUrl();
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!player.isLocal || isInteract)
                return;

            PlayUrl();
        }
        
#if !COMPILER_UDONSHARP && UNITY_EDITOR

        private BoxCollider _collider;
        private bool _isInitGizmos;
        void OnDrawGizmosSelected() 
        {
            if (!_isInitGizmos)
            {
                _collider = GetComponent<BoxCollider>();
                _isInitGizmos = true;
            }

            if (!_collider)
                return;
            
            Gizmos.color = new Color (0f, 1f, 0, 0.3f);
            Gizmos.DrawCube (transform.position + _collider.center, Vector3.Scale(transform.lossyScale, _collider.size));
        }

#endif
    }
}