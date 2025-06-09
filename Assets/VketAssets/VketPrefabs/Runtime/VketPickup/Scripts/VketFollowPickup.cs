
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
#if !COMPILER_UDONSHARP && UNITY_EDITOR && !UDONSHARP && !PLATFORM_ANDROID
using UdonSharpEditor;
#endif

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class VketFollowPickup : UdonSharpBehaviour
    {
        [UdonSynced] private Vector3 RelativePos;
        [UdonSynced] private Quaternion RelativeRot;
        [UdonSynced] private Vector3 Pos;
        [UdonSynced] private Quaternion Rot;
        [UdonSynced] private sbyte FollowBone = -1;

        [SerializeField] private float attachRange = 0.3f;
        [SerializeField] private int targetBone;
        [SerializeField] private VRC_Pickup pickup;
        [SerializeField] private UdonBehaviour udonManager;

        private readonly float SyncInterval = 0.5f;
        private readonly float MaxDistanceErr = 0.05f;
        private readonly float MaxRotationErr = 5.0f;
        private readonly float ResetTime = 30.0f;
        private readonly float PositionOffsetSpeed = 0.7f;
        private readonly float RotationOffsetSpeed = 45.0f;
        private readonly float lerpSpeed = 5.0f;

        private VRCPlayerApi localPlayer;
        private Transform transformCache;
        private Vector3 relativePosBuff;
        private Quaternion relativeRotBuff;
        private Vector3 lerpRelativePos;
        private Quaternion lerpRelativeRot;
        private Vector3 initPosition;
        private Quaternion initRotation;
        private float sinceLastRequest;
        private float tmr;
        private float remainWaitTime = 30.0f;
        private bool hasAttached;
        private bool isVR;

        void Start()
        {
            transformCache = transform;
            if (Networking.LocalPlayer != null)
            {
                localPlayer = Networking.LocalPlayer;
                isVR = localPlayer.IsUserInVR();

                OnDrop_Dlayed();

                initPosition = transformCache.position;
                initRotation = transformCache.rotation;
            }
        }

        public void ResetPosition()
        {
            transformCache.SetPositionAndRotation(initPosition, initRotation);
            if (Networking.IsOwner(gameObject))
            {
                if (pickup.IsHeld)
                {
                    pickup.Drop();
                }
                else
                {
                    SendCustomEventDelayedSeconds(nameof(OnDrop_Dlayed), 0.1f);
                }
            }

        }

        public override void OnPickupUseDown()
        {
            if (AttachBone(targetBone, isVR))
            {
                hasAttached = true;
                pickup.Drop();

                Vector3 bonePos = localPlayer.GetBonePosition((HumanBodyBones)FollowBone);
                Quaternion boneRot = localPlayer.GetBoneRotation((HumanBodyBones)FollowBone);
                if (!isVR)
                {
                    transformCache.SetPositionAndRotation(bonePos, boneRot);
                    udonManager.SendCustomEvent("_EnableFollowPickupUI");
                }

                RelativePos = Quaternion.Inverse(boneRot) * (transformCache.position - bonePos);
                RelativeRot = Quaternion.Inverse(boneRot) * transformCache.rotation;
                RequestSerialization();

                relativePosBuff = RelativePos;
                relativeRotBuff = RelativeRot;
            }
        }

        public override void OnDrop()
        {
            SendCustomEventDelayedSeconds(nameof(OnDrop_Dlayed), 0.1f);
            OnDrop_Dlayed();
        }

        public void OnDrop_Dlayed()
        {
            if (hasAttached)
                return;

            Pos = transformCache.position;
            Rot = transformCache.rotation;
            FollowBone = (sbyte)-1;

            RequestSerialization();
        }

        public override void OnPickup()
        {
            Networking.SetOwner(localPlayer, this.gameObject);
            hasAttached = false;

            if (isVR)
            {
                if (pickup.currentHand == VRC_Pickup.PickupHand.Right)
                    FollowBone = (sbyte)HumanBodyBones.RightHand;
                else
                    FollowBone = (sbyte)HumanBodyBones.LeftHand;
            }

            SendCustomEventDelayedSeconds(nameof(OnPickup_Delayed), 0.5f);
            OnPickup_Dlayed2();

            remainWaitTime = ResetTime;
        }

        public void OnPickup_Delayed()
        {
            OnPickup_Dlayed2();
        }

        public void OnPickup_Dlayed2()
        {
            if (pickup.currentHand == VRC_Pickup.PickupHand.Right)
                FollowBone = (sbyte)HumanBodyBones.RightHand;
            else
                FollowBone = (sbyte)HumanBodyBones.LeftHand;

            Vector3 handPos = localPlayer.GetBonePosition((HumanBodyBones)FollowBone);
            Quaternion handRot = localPlayer.GetBoneRotation((HumanBodyBones)FollowBone);
            RelativePos = Quaternion.Inverse(handRot) * (transformCache.position - handPos);
            RelativeRot = Quaternion.Inverse(handRot) * transformCache.rotation;
            RequestSerialization();
        }

        void Update()
        {
            tmr = tmr + Time.deltaTime;
            sinceLastRequest = sinceLastRequest + Time.deltaTime;
            if (localPlayer != null)
            {
                if (Networking.IsOwner(gameObject))
                {
                    if (FollowBone != -1)
                    {
                        if (hasAttached || pickup.IsHeld)
                        {
                            Vector3 bonePos = localPlayer.GetBonePosition((HumanBodyBones)FollowBone);
                            Quaternion boneRot = localPlayer.GetBoneRotation((HumanBodyBones)FollowBone);

                            if (hasAttached)
                            {
                                transformCache.position = bonePos + boneRot * relativePosBuff;
                                transformCache.rotation = boneRot * relativeRotBuff;

                                if (!isVR)
                                {
                                    if (DesktopInputs())
                                    {
                                        relativePosBuff = Quaternion.Inverse(boneRot) * (transformCache.position - bonePos);
                                        relativeRotBuff = Quaternion.Inverse(boneRot) * transformCache.rotation;
                                        return;
                                    }
                                }
                            }

                            relativePosBuff = Quaternion.Inverse(boneRot) * (transformCache.position - bonePos);
                            relativeRotBuff = Quaternion.Inverse(boneRot) * transformCache.rotation;
                            if (((Vector3.Distance(relativePosBuff, RelativePos) >= MaxDistanceErr)
                                    || (Quaternion.Angle(relativeRotBuff, RelativeRot) >= MaxRotationErr))
                                && (sinceLastRequest >= SyncInterval))
                            {
                                sinceLastRequest = 0.0f;
                                RelativePos = relativePosBuff;
                                RelativeRot = relativeRotBuff;
                                RequestSerialization();
                            }
                        }
                        else
                        {
                            OnDrop_Dlayed();
                        }
                    }
                    else
                    {
                        // Reset Timer
                        if (remainWaitTime > 0)
                        {
                            remainWaitTime -= Time.deltaTime;
                            if (remainWaitTime <= 0)
                            {
                                ResetPosition();
                            }
                        }
                    }
                }
                else
                {
                    hasAttached = false;
                    if (FollowBone != -1)
                    {

                        Vector3 bonePos = Networking.GetOwner(gameObject).GetBonePosition((HumanBodyBones)FollowBone);
                        Quaternion boneRot = Networking.GetOwner(gameObject).GetBoneRotation((HumanBodyBones)FollowBone);

                        lerpRelativePos = Vector3.Lerp(lerpRelativePos, RelativePos, Time.deltaTime * lerpSpeed);
                        lerpRelativeRot = Quaternion.Lerp(lerpRelativeRot, RelativeRot, Time.deltaTime * lerpSpeed);
                        transformCache.SetPositionAndRotation(bonePos + (boneRot * lerpRelativePos), boneRot * lerpRelativeRot);
                    }
                    else
                    {
                        if (tmr >= 0.2f)
                        {
                            tmr = 0.0f;
                            transformCache.SetPositionAndRotation(Pos, Rot);
                        }
                    }
                }
            }
        }

        private bool DesktopInputs()
        {
            if (Input.GetKeyDown("tab"))
            {
                udonManager.SendCustomEvent("_DisableFollowPickupUI");

                hasAttached = false;
                OnDrop_Dlayed();

                return false;
            }

            Quaternion boneRot = localPlayer.GetBoneRotation((HumanBodyBones)FollowBone);
            bool shift = Input.GetKey("left shift") || Input.GetKey("right shift");
            if (shift)
            {
                if (Input.GetKey("j"))
                {
                    transformCache.position += boneRot * -Vector3.right * Time.deltaTime * PositionOffsetSpeed;
                    return true;
                }
                if (Input.GetKey("l"))
                {
                    transformCache.position += boneRot * Vector3.right * Time.deltaTime * PositionOffsetSpeed;
                    return true;
                }
                if (Input.GetKey("k"))
                {
                    transformCache.position += boneRot * -Vector3.up * Time.deltaTime * PositionOffsetSpeed;
                    return true;
                }
                if (Input.GetKey("i"))
                {
                    transformCache.position += boneRot * Vector3.up * Time.deltaTime * PositionOffsetSpeed;
                    return true;
                }
                if (Input.GetKey("u"))
                {
                    transformCache.position += boneRot * -Vector3.forward * Time.deltaTime * PositionOffsetSpeed;
                    return true;
                }
                if (Input.GetKey("o"))
                {
                    transformCache.position += boneRot * Vector3.forward * Time.deltaTime * PositionOffsetSpeed;
                    return true;
                }
            }
            else
            {
                if (Input.GetKey("j"))
                {
                    transformCache.rotation *= Quaternion.Euler(boneRot * -Vector3.up * Time.deltaTime * RotationOffsetSpeed);
                    return true;
                }
                if (Input.GetKey("l"))
                {
                    transformCache.rotation *= Quaternion.Euler(boneRot * Vector3.up * Time.deltaTime * RotationOffsetSpeed);
                    return true;
                }
                if (Input.GetKey("k"))
                {
                    transformCache.rotation *= Quaternion.Euler(boneRot * -Vector3.right * Time.deltaTime * RotationOffsetSpeed);
                    return true;
                }
                if (Input.GetKey("i"))
                {
                    transformCache.rotation *= Quaternion.Euler(boneRot * Vector3.right * Time.deltaTime * RotationOffsetSpeed);
                    return true;
                }
                if (Input.GetKey("u"))
                {
                    transformCache.rotation *= Quaternion.Euler(boneRot * Vector3.forward * Time.deltaTime * RotationOffsetSpeed);
                    return true;
                }
                if (Input.GetKey("o"))
                {
                    transformCache.rotation *= Quaternion.Euler(boneRot * -Vector3.forward * Time.deltaTime * RotationOffsetSpeed);
                    return true;
                }
            }

            return false;
        }

        private bool AttachBone(int targetBone, bool isVR)
        {
            HumanBodyBones bone;
            switch (targetBone)
            {
                case 0:
                    bone = HumanBodyBones.Head;
                    break;
                case 1:
                    bone = HumanBodyBones.Neck;
                    break;
                case 2:
                    bone = HumanBodyBones.Chest;
                    break;
                case 3:
                    bone = HumanBodyBones.Spine;
                    break;
                case 4:
                    bone = HumanBodyBones.Hips;
                    break;
                case 5:
                    if (pickup == localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right))
                        bone = HumanBodyBones.LeftShoulder;
                    else
                        bone = HumanBodyBones.RightShoulder;
                    break;
                case 6:
                    if (pickup == localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right))
                        bone = HumanBodyBones.LeftUpperArm;
                    else
                        bone = HumanBodyBones.RightUpperArm;
                    break;
                case 7:
                    if (pickup == localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right))
                        bone = HumanBodyBones.LeftLowerArm;
                    else
                        bone = HumanBodyBones.RightLowerArm;
                    break;
                case 8:
                    if (pickup == localPlayer.GetPickupInHand(VRC_Pickup.PickupHand.Right))
                        bone = HumanBodyBones.LeftHand;
                    else
                        bone = HumanBodyBones.RightHand;
                    break;
                case 9:
                    if (IsAttachRightBone(localPlayer.GetBonePosition(HumanBodyBones.LeftUpperLeg), localPlayer.GetBonePosition(HumanBodyBones.RightUpperLeg)))
                        bone = HumanBodyBones.RightUpperLeg;
                    else
                        bone = HumanBodyBones.LeftUpperLeg;
                    break;
                case 10:
                    if (IsAttachRightBone(localPlayer.GetBonePosition(HumanBodyBones.LeftLowerLeg), localPlayer.GetBonePosition(HumanBodyBones.RightLowerLeg)))
                        bone = HumanBodyBones.RightLowerLeg;
                    else
                        bone = HumanBodyBones.LeftLowerLeg;
                    break;
                case 11:
                    if (IsAttachRightBone(localPlayer.GetBonePosition(HumanBodyBones.LeftFoot), localPlayer.GetBonePosition(HumanBodyBones.RightFoot)))
                        bone = HumanBodyBones.RightFoot;
                    else
                        bone = HumanBodyBones.LeftFoot;
                    break;
                default:
                    bone = HumanBodyBones.Head;
                    break;
            }

            Vector3 bonePos = localPlayer.GetBonePosition(bone);
            if (bonePos == Vector3.zero)
                return false;

            if (Vector3.Distance(transform.position, bonePos) > attachRange && isVR)
            {
                return false;
            }
            else
            {
                FollowBone = (sbyte)bone;
                return true;
            }
        }

        private bool IsAttachRightBone(Vector3 leftBonePos, Vector3 rightBonePos)
        {
            float leftDist = Vector3.SqrMagnitude(leftBonePos - transform.position);
            float rightDist = Vector3.SqrMagnitude(rightBonePos - transform.position);
            return rightDist < leftDist;
        }
#if !COMPILER_UDONSHARP && UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
#if !UDONSHARP && !PLATFORM_ANDROID
            this.UpdateProxy();
#endif
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, attachRange);
        }
#endif
        }
}