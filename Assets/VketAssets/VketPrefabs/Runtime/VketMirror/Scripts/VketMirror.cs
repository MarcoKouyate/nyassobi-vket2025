
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketMirror : UdonSharpBehaviour
    {
        [SerializeField]
        private VRC_MirrorReflection _mirror;
        [SerializeField]
        private Collider _areaCollider;
        [SerializeField]
        private bool _isManualMode;

        private bool _isInBooth;
        public void _VketStart()
        {
            if (!_mirror) enabled = false;
            if (_mirror.gameObject.activeSelf)
                _mirror.gameObject.SetActive(false);


            if (_isManualMode)
            {
                if(_areaCollider)
                    _areaCollider.enabled = false;
            }
            else
            {
                if (!_areaCollider) enabled = false;
                else if (!_areaCollider.isTrigger)
                    _areaCollider.isTrigger = true;
            }



        }

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (!player.isLocal || _isManualMode || !_isInBooth) return;
            SetMirrorEnable(true);
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (!player.isLocal || _isManualMode || !_isInBooth) return;
            SetMirrorEnable(false);
        }
        /*
        public override void OnPlayerTriggerStay(VRCPlayerApi player)
        {
            if (!player.isLocal || _isManualMode) return;
            if (_isInBooth && !_mirror.gameObject.activeSelf)
                SetMirrorEnable(true);
        }
        */


        private void SetMirrorEnable(bool isEnable, bool manual = false)
        {
            if (_mirror.gameObject.activeSelf == isEnable) return;
            if (_isManualMode != manual) return;
            if (!_isInBooth && isEnable) return;
            _mirror.gameObject.SetActive(isEnable);
        }

        public void _SetEnableMirror() => SetMirrorEnable(true, true);
        public void _SetDisableMirror() => SetMirrorEnable(false, true);
        public void _SetToggleMirror() => SetMirrorEnable(!_mirror.gameObject.activeSelf, true);
        public void _VketOnBoothEnter()
        {
            _isInBooth = true;
        }
        public void _VketOnBoothExit()
        {
            _isInBooth = false;
            SetMirrorEnable(false, _isManualMode);
        }

    }
}