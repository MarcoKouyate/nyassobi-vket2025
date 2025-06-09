
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.ExhibitorUdonManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ExhibitorBoothActivator : UdonSharpBehaviour
    {
        [SerializeField] private ExhibitorBoothManager _boothManager;
        [SerializeField] private CapsuleCollider _playerTriggerCollider;

        private void OnTriggerEnter(Collider other)
        {
            if (other != _playerTriggerCollider)
                return;

            _boothManager._ActivateBooth();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other != _playerTriggerCollider)
                return;

            _boothManager._DeactivateBooth();
        }
    }
}