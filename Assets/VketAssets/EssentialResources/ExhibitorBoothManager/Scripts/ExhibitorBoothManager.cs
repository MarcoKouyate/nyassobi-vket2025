
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Components;
using Vket.EssentialResources;
using Vket.EssentialResources.Attribute;
using BaseVRCVideoPlayer = VRC.SDK3.Video.Components.Base.BaseVRCVideoPlayer;

namespace Vket.ExhibitorUdonManager
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class ExhibitorBoothManager : UdonSharpBehaviour
    {
        [SerializeField] private GameObject _uiRootObject;
        [SerializeField] private Transform[] _userInterfaces;
        [SerializeField] private UdonBehaviour _storePreviewUI;

        [Header("AutoSetup")]
        [ReadOnly] public Component[] StartComponents;
        [ReadOnly] public Component[] SoundFadeComponents;
        [ReadOnly] public Component[] LanguageSwitcherComponents;
        [ReadOnly] public Component[] IsQuestComponents;
        [ReadOnly] public Component[] UdonComponents;
        [ReadOnly] public int[] CallbackMasks;
        [ReadOnly] public VRCPickup[] Pickups;
        [ReadOnly] public Vector3[] InitPositions;
        [ReadOnly] public Quaternion[] InitRotations;
        [ReadOnly] public AudioSource[] AudioSources;
        [ReadOnly] public Camera[] Cameras;
        [ReadOnly] public GameObject[] ProjectorObjects;
        [ReadOnly] public BaseVRCVideoPlayer[] VideoPlayers;

        private const float EXECUTE_START_TIME = 1.0f;
        private const string NAME_OF_STOP_SOUND_FADE = "_StopSoundFade";
        private const string NAME_OF_SWITCH_TO_EN = "_SwitchToEn";
        private const string NAME_OF_SWITCH_TO_JP = "_SwitchToJp";

        private VRCPlayerApi _localPlayer;
        private Transform _playerTriggerTransform;
        private float _defaultJumpImpulse;
        private float _defaultWalkSpeed;
        private float _defaultRunSpeed;
        private float _defaultStrafeSpeed;
        private bool _isInBooth;
        private bool _isSwitchedEnglish;
        private bool _hasStart;

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            _playerTriggerTransform = transform.Find("PlayerTrigger");

#if UNITY_ANDROID
            // Set Variable "Vket_IsQuest"
            foreach (var component in IsQuestComponents)
            {
                UdonBehaviour ub = (UdonBehaviour)component;
                ub.SetProgramVariable(VketCallbacks.NAME_OF_VKET_IS_QUEST, true);
            }
#endif

            SendCustomEventDelayedSeconds(nameof(_DelayExecuteStart), EXECUTE_START_TIME);
        }

        public void _DelayExecuteStart()
        {
            // Send Callback "_VketStart"
            foreach (var component in StartComponents)
            {
                UdonBehaviour ub = (UdonBehaviour)component;
                ub.SendCustomEvent(VketCallbacks.NAME_OF_VKET_START);
            }

            if (Utilities.IsValid(_localPlayer))
            {
                _defaultJumpImpulse = _localPlayer.GetJumpImpulse();
                _defaultWalkSpeed = _localPlayer.GetWalkSpeed();
                _defaultRunSpeed = _localPlayer.GetRunSpeed();
                _defaultStrafeSpeed = _localPlayer.GetStrafeSpeed();
            }

            _hasStart = true;
        }

        private void Update()
        {
            if (!_isInBooth)
                return;

            // Send Callback "_VketUpdate"
            for (int i = 0; i < UdonComponents.Length; i++)
            {
                int hasBit = CallbackMasks[i] & 1;
                if (hasBit != 0)
                {
                    if (UdonComponents[i].gameObject.activeInHierarchy)
                    {
                        UdonBehaviour ub = (UdonBehaviour)UdonComponents[i];
                        ub.SendCustomEvent(VketCallbacks.NAME_OF_VKET_UPDATE);
                    }
                }
            }
        }

        private void FixedUpdate()
        {
            if (!_isInBooth)
                return;

            // Send Callback "_VketFixedUpdate"
            for (int i = 0; i < UdonComponents.Length; i++)
            {
                int hasBit = CallbackMasks[i] & 2;
                if (hasBit != 0)
                {
                    if (UdonComponents[i].gameObject.activeInHierarchy)
                    {
                        UdonBehaviour ub = (UdonBehaviour)UdonComponents[i];
                        ub.SendCustomEvent(VketCallbacks.NAME_OF_VKET_FIXED_UPDATE);
                    }
                }
            }
        }

        private void LateUpdate()
        {
            if (_hasStart && Utilities.IsValid(_localPlayer))
            {
                _playerTriggerTransform.position = _localPlayer.GetPosition();
            }

            if (!_isInBooth)
                return;

            // Send Callback "_VketLateUpdate"
            for (int i = 0; i < UdonComponents.Length; i++)
            {
                int hasBit = CallbackMasks[i] & 4;
                if (hasBit != 0)
                {
                    if (UdonComponents[i].gameObject.activeInHierarchy)
                    {
                        UdonBehaviour ub = (UdonBehaviour)UdonComponents[i];
                        ub.SendCustomEvent(VketCallbacks.NAME_OF_VKET_LATE_UPDATE);
                    }
                }
            }
        }

        public override void PostLateUpdate()
        {
            if (!_isInBooth)
                return;

            // Send Callback "_VketPostLateUpdate"
            for (int i = 0; i < UdonComponents.Length; i++)
            {
                int hasBit = CallbackMasks[i] & 8;
                if (hasBit != 0)
                {
                    if (UdonComponents[i].gameObject.activeInHierarchy)
                    {
                        UdonBehaviour ub = (UdonBehaviour)UdonComponents[i];
                        ub.SendCustomEvent(VketCallbacks.NAME_OF_VKET_POST_LATE_UPDATE);
                    }
                }
            }
        }

        public void _ActivateBooth()
        {
            // Send Callback "_VketOnBoothEnter"
            for (int i = 0; i < UdonComponents.Length; i++)
            {
                int hasBit = CallbackMasks[i] & 16;
                if (hasBit != 0)
                {
                    UdonBehaviour ub = (UdonBehaviour)UdonComponents[i];
                    ub.SendCustomEvent(VketCallbacks.NAME_OF_VKET_ON_BOOTH_ENTER);
                }
            }

            _isInBooth = true;
        }

        public void _DeactivateBooth()
        {
            // Send Callback "_VketOnBoothExit"
            for (int i = 0; i < UdonComponents.Length; i++)
            {
                int hasBit = CallbackMasks[i] & 32;
                if (hasBit != 0)
                {
                    UdonBehaviour ub = (UdonBehaviour)UdonComponents[i];
                    ub.SendCustomEvent(VketCallbacks.NAME_OF_VKET_ON_BOOTH_EXIT);
                }
            }
            _storePreviewUI.SendCustomEvent(VketCallbacks.NAME_OF_VKET_ON_BOOTH_EXIT);

            // Exit Processing
            for (int i = 0; i < Pickups.Length; i++)
            {
                Pickups[i].Drop();
                Pickups[i].transform.SetPositionAndRotation(InitPositions[i], InitRotations[i]);
            }
            foreach (var audioSource in AudioSources)
            {
                audioSource.enabled = false;
            }
            foreach (var camera in Cameras)
            {
                camera.enabled = false;
            }
            foreach (var projectorObject in ProjectorObjects)
            {
                projectorObject.SetActive(false);
            }
            foreach (var videoPlayer in VideoPlayers)
            {
                if (videoPlayer.gameObject.activeInHierarchy)
                    videoPlayer.Stop();
            }

            if (Utilities.IsValid(_localPlayer))
            {
                _localPlayer.SetJumpImpulse(_defaultJumpImpulse);
                _localPlayer.SetWalkSpeed(_defaultWalkSpeed);
                _localPlayer.SetRunSpeed(_defaultRunSpeed);
                _localPlayer.SetStrafeSpeed(_defaultStrafeSpeed);
            }

            _isInBooth = false;
        }

        public void _StopAllSoundFade()
        {
            foreach (var component in SoundFadeComponents)
            {
                UdonBehaviour ub = (UdonBehaviour)component;
                ub.SendCustomEvent(NAME_OF_STOP_SOUND_FADE);
            }
        }

        public void _SwitchLanguage()
        {
            _isSwitchedEnglish = !_isSwitchedEnglish;

            foreach (var component in LanguageSwitcherComponents)
            {
                UdonBehaviour ub = (UdonBehaviour)component;
                ub.SendCustomEvent(_isSwitchedEnglish ? NAME_OF_SWITCH_TO_EN : NAME_OF_SWITCH_TO_JP);
            }
        }

        public void _EnableFollowPickupUI()
        {
            _userInterfaces[0].gameObject.SetActive(true);
            _uiRootObject.SetActive(true);
        }

        public void _DisableFollowPickupUI()
        {
            _userInterfaces[0].gameObject.SetActive(false);
            foreach (var ui in _userInterfaces)
            {
                if (ui.gameObject.activeSelf)
                    return;
            }
            _uiRootObject.SetActive(false);
        }
    }
}