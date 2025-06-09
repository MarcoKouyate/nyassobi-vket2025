
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;

namespace Vket.ExhibitorUdonManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class RigidbodyManager : UdonSharpBehaviour
    {
        public Rigidbody[] Rigidbodies;
        public VRCObjectSync[] ObjectSyncs;
        public BoxCollider BoxCollider;

        private VRCPlayerApi _localPlayer;
        private int _playerCount;

        private void Start()
        {
            SetKinematic(true);
            SendCustomEventDelayedSeconds(nameof(_DelayEnabled), 1.0f, VRC.Udon.Common.Enums.EventTiming.Update);
        }

        public void _DelayEnabled()
        {
            BoxCollider.enabled = true;
        }

        private void SetKinematic(bool kinematic)
        {
            if (_localPlayer == null)
                _localPlayer = Networking.LocalPlayer;

            foreach (var rb in Rigidbodies)
                rb.isKinematic = kinematic;

            foreach(var objectSync in ObjectSyncs)
            {
                if (Networking.IsOwner(_localPlayer, objectSync.gameObject))
                    objectSync.SetKinematic(kinematic);
            }
        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            _playerCount++;
            SetKinematic(_playerCount == 0);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            _playerCount--;
            SetKinematic(_playerCount == 0);
        }

        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            _playerCount = 0;

            BoxCollider.enabled = false;
            BoxCollider.enabled = true;

            SendCustomEventDelayedFrames("_CountCheck", 2, VRC.Udon.Common.Enums.EventTiming.Update);
        }

        public void _CountCheck()
        {
            if (_playerCount == 0)
                SetKinematic(true);
        }
    }
}