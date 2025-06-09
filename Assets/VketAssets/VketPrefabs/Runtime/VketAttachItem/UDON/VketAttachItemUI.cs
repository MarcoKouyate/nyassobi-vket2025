
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using UnityEngine.UI;

namespace VketAssets.VketPrefabs.VketAttachItem.Udon
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketAttachItemUI : UdonSharpBehaviour
    {
        [SerializeField] private Transform playerCam;
        [SerializeField] private Transform uiCanvas;
        [SerializeField] private Transform boneSelectPanel;

        [SerializeField] private Slider _size;
        [SerializeField] private Slider _upDown;
        [SerializeField] private Slider _fwdBak;
        [SerializeField] private Slider _lR;
        [SerializeField] private Slider _xRoll;
        [SerializeField] private Slider _yRoll;
        [SerializeField] private Slider _zRoll;
        [SerializeField] private VketAttachHumanoidBoneSelector _humanoidSelector;

        private VketAttachItem _currentSkinItem;

        public void _ActiveSetCurrentSkinItem(VketAttachItem skinItem)
        {
            if (skinItem == null) return;
            if (_currentSkinItem)
            {
                _currentSkinItem.settingButton.SetActive(true);
            }
            _currentSkinItem = skinItem;
            _SliderSetup(); 
            _ApplySlider();
            boneSelectPanel.gameObject.SetActive(false);    
            uiCanvas.gameObject.SetActive(true);
            uiCanvas.transform.SetPositionAndRotation(skinItem.uiPoint.transform.position, skinItem.uiPoint.transform.rotation);
            uiCanvas.transform.localScale = skinItem.uiPoint.localScale;
            SendCustomEventDelayedSeconds(nameof(_LateUpdateSeri), 1, VRC.Udon.Common.Enums.EventTiming.LateUpdate);
            skinItem.settingButton.SetActive(false);
        }
        public void _CloseUI()
        {
            if (_currentSkinItem != null)
            {
                _currentSkinItem._SetUse(false);
                _currentSkinItem = null;
            }
            uiCanvas.gameObject.SetActive(false);
        }
        public void _CloseUIOnly() { 
            uiCanvas.gameObject.SetActive(false);
            if (_currentSkinItem)
            {
                _currentSkinItem.settingButton.SetActive(true);
            }
        }
        private void _SliderSetup()
        {
            if (_currentSkinItem!=null)
            {
                _humanoidSelector._SetCurrentSelectBone((int)_currentSkinItem.GetProgramVariable(nameof(VketAttachItem.humanBodyBone)));
                _size.SetValueWithoutNotify( (float)_currentSkinItem.GetProgramVariable(nameof(VketAttachItem.Size)));
                _upDown.SetValueWithoutNotify( (float)_currentSkinItem.GetProgramVariable(nameof(VketAttachItem.UpDown)));
                _fwdBak.SetValueWithoutNotify((float)_currentSkinItem.GetProgramVariable(nameof(VketAttachItem.FwdBak)));
                _lR.SetValueWithoutNotify((float)_currentSkinItem.GetProgramVariable(nameof(VketAttachItem.LR)));
                _xRoll.SetValueWithoutNotify((float)_currentSkinItem.GetProgramVariable(nameof(VketAttachItem.XRoll)));
                _yRoll.SetValueWithoutNotify((float)_currentSkinItem.GetProgramVariable(nameof(VketAttachItem.YRoll)));
                _zRoll.SetValueWithoutNotify((float)_currentSkinItem.GetProgramVariable(nameof(VketAttachItem.ZRoll)));   
            }
        }
        public void _ApplySlider()
        {
            if (_currentSkinItem != null)
            {
                _currentSkinItem.SetProgramVariable(nameof(VketAttachItem.humanBodyBone),_humanoidSelector.CurrentSelectBone);
                _currentSkinItem.SetProgramVariable(nameof(VketAttachItem.Size), _size.value);
                _currentSkinItem.SetProgramVariable(nameof(VketAttachItem.UpDown), _upDown.value);
                _currentSkinItem.SetProgramVariable(nameof(VketAttachItem.FwdBak), _fwdBak.value);
                _currentSkinItem.SetProgramVariable(nameof(VketAttachItem.LR), _lR.value);
                _currentSkinItem.SetProgramVariable(nameof(VketAttachItem.XRoll), _xRoll.value);
                _currentSkinItem.SetProgramVariable(nameof(VketAttachItem.YRoll), _yRoll.value);
                _currentSkinItem.SetProgramVariable(nameof(VketAttachItem.ZRoll), _zRoll.value);
                _LateUpdateSeri();
            }
        }
        public void _LateUpdateSeri()
        {
            if (_currentSkinItem!=null)
            {
                _currentSkinItem.RequestSerialization();
            }
        }
        private void Update()
        {
            if (_currentSkinItem != null)
            {
                playerCam.position = Vector3.Lerp(playerCam.position, _currentSkinItem.visual.transform.position, Time.deltaTime*10);
                playerCam.rotation= Quaternion.Lerp(playerCam.rotation, _currentSkinItem.visual.transform.rotation, Time.deltaTime*10);
            }
        }
    }
}