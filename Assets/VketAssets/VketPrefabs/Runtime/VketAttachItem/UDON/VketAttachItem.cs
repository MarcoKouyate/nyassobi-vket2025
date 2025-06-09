
using System;
using UdonSharp;
using UnityEngine;
using Vket.EssentialResources.Attribute;
using VRC.SDKBase;

namespace VketAssets.VketPrefabs.VketAttachItem.Udon
{
    public enum HumanBoneSimple
    { //
        // 摘要:
        //     This is the Hips bone.
        Hips = 0,
        //
        // 摘要:
        //     This is the Left Upper Leg bone.
        LeftUpperLeg = 1,
        //
        // 摘要:
        //     This is the Right Upper Leg bone.
        RightUpperLeg = 2,
        //
        // 摘要:
        //     This is the Left Knee bone.
        LeftLowerLeg = 3,
        //
        // 摘要:
        //     This is the Right Knee bone.
        RightLowerLeg = 4,
        //
        // 摘要:
        //     This is the Left Ankle bone.
        LeftFoot = 5,
        //
        // 摘要:
        //     This is the Right Ankle bone.
        RightFoot = 6,
        //
        // 摘要:
        //     This is the first Spine bone.
        Spine = 7,
        //
        // 摘要:
        //     This is the Chest bone.
        Chest = 8,
        //
        // 摘要:
        //     This is the Neck bone.
        Neck = 9,
        //
        // 摘要:
        //     This is the Head bone.
        Head = 10,
        //
        // 摘要:
        //     This is the Left Upper Arm bone.
        LeftUpperArm = 13,
        //
        // 摘要:
        //     This is the Right Upper Arm bone.
        RightUpperArm = 14,
        //
        // 摘要:
        //     This is the Left Elbow bone.
        LeftLowerArm = 0xF,
        //
        // 摘要:
        //     This is the Right Elbow bone.
        RightLowerArm = 0x10,
        //
        // 摘要:
        //     This is the Left Wrist bone.
        LeftHand = 17,
        //
        // 摘要:
        //     This is the Right Wrist bone.
        RightHand = 18,

    }
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class VketAttachItem : UdonSharpBehaviour
    {
        [SceneSingleton, HideInInspector] public VketAttachItemUI skinGimmikUI;
        [HideInInspector] public Transform visual;
        [HideInInspector] public Transform uiPoint;
        [HideInInspector] public GameObject useButton;
        [HideInInspector] public GameObject settingButton;
        public HumanBoneSimple defaultAttachHumanBodyBone = HumanBoneSimple.Head;
        public Vector3 bonePositionOffset;
        public Vector3 boneRotationOffset;

        [UdonSynced(UdonSyncMode.None), HideInInspector, NonSerialized]
        public int humanBodyBone;
        [UdonSynced(UdonSyncMode.Linear), HideInInspector, NonSerialized]
        public float Size = 1;
        [UdonSynced(UdonSyncMode.Linear), HideInInspector, NonSerialized]
        public float UpDown = 0;
        [UdonSynced(UdonSyncMode.Linear), HideInInspector, NonSerialized]
        public float FwdBak = 0;
        [UdonSynced(UdonSyncMode.Linear), HideInInspector, NonSerialized]
        public float LR = 0;
        [UdonSynced(UdonSyncMode.Linear), HideInInspector, NonSerialized]
        public float XRoll = 0;
        [UdonSynced(UdonSyncMode.Linear), HideInInspector, NonSerialized]
        public float YRoll = 0;
        [UdonSynced(UdonSyncMode.Linear), HideInInspector, NonSerialized]
        public float ZRoll = 0;
        [UdonSynced(UdonSyncMode.None), HideInInspector, NonSerialized]
        public bool ItemUsing;
        bool ItemUsingLocal;

        private VRCPlayerApi p;

        private Vector3 oraginPosition, oraginSize, oraginRot;

        private void Start()
        {
            humanBodyBone = (int)defaultAttachHumanBodyBone;
            oraginPosition = visual.transform.position;
            oraginSize = visual.transform.localScale;
            oraginRot = visual.transform.eulerAngles;
            settingButton.SetActive(false);
        }
        public void _SettingButton()
        {
            skinGimmikUI._ActiveSetCurrentSkinItem(this);
        }
        public void _SetUse(bool active)
        {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
            ItemUsing = active;
            if (!active)
            {
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetItemAllState));
                skinGimmikUI._CloseUIOnly();
                settingButton.SetActive(false);
            }
            else
            {
                skinGimmikUI._ActiveSetCurrentSkinItem(this);
            }
        }
        public override void Interact()
        {
            if (!ItemUsing)
            {
                _SetUse(true);
            }
        }
        private void Update()
        {
            if (ItemUsing)
            {
                if (p == null)
                {
                    p = Networking.GetOwner(gameObject);
                }
                useButton.gameObject.SetActive(false);
                if (p.IsValid())
                {
                    Vector3 bonePos = p.GetBonePosition((HumanBodyBones)humanBodyBone);
                    Quaternion boneRot = p.GetBoneRotation((HumanBodyBones)humanBodyBone)*Quaternion.Euler(boneRotationOffset);
                    // 将旋转偏移转换为Quaternion
                    Quaternion rotOffsetQuat = Quaternion.Euler(new Vector3(XRoll, YRoll, ZRoll));
                    // 更新Transform的位置和旋转
                    // 先应用旋转偏移，然后是目标旋转
                    visual.transform.rotation = boneRot * rotOffsetQuat;
                    // 先将位置偏移应用于已旋转的Transform，然后加上目标位置
                    visual.transform.position = boneRot * rotOffsetQuat * (new Vector3(LR, UpDown, FwdBak)+bonePositionOffset)+bonePos;
                }
                visual.transform.localScale = new Vector3(Size, Size, Size);
                ItemUsingLocal = true;
            }
            else
            {
                if (ItemUsingLocal)
                {
                    ItemUsingLocal = false;
                    p = null;
                    visual.transform.position = oraginPosition;
                    visual.transform.localScale = oraginSize;
                    visual.transform.eulerAngles = oraginRot;
                    useButton.gameObject.SetActive(true);
                }
            }
        }
        public override void OnPlayerLeft(VRCPlayerApi player)
        {
            if (player == p)
            {
                if (gameObject.activeSelf && Networking.IsOwner(Networking.LocalPlayer, gameObject))
                {
                    ItemUsing = false;
                    SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetItemAllState));
                    Debug.Log("_RemoteReturnObject");
                }
            }
        }
        public void _VketOnBoothExit()
        {
            if (Networking.IsOwner(Networking.LocalPlayer, gameObject))
            {
                ItemUsing = false;
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(ResetItemAllState));
                skinGimmikUI._CloseUI();
            }
        }
        public void ResetItemAllState()
        {
            p=null;
            humanBodyBone =(int)defaultAttachHumanBodyBone;
            Size = 1;
            UpDown = 0;
            FwdBak = 0;
            LR = 0;
            XRoll = 0;
            YRoll = 0;
            ZRoll = 0;
        }
    }
}