
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Components;
#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using UdonSharpEditor;
#endif

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketAvatarPedestal : UdonSharpBehaviour
    {
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private VRCAvatarPedestal avatarPedestal;
        [SerializeField]
        private CapsuleCollider capsuleCollider;
        [SerializeField]
        private Transform popupTransform;
        [SerializeField]
        private bool autoAdjustPosition = true;
        [SerializeField]
        private int type; // 0:Default, 1:2D, 2:3D

        private bool hasOpen;

        public override void Interact()
        {
            animator.enabled = true;

            if (!hasOpen)
            {
                animator.SetBool("Open", true);
                hasOpen = true;

                if (capsuleCollider != null)
                {

                    if (autoAdjustPosition)
                    {
                        float headHeight = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position.y;
                        Vector3 worldScale = transform.lossyScale;
                        Vector3 centerPos = transform.position + Vector3.Scale(capsuleCollider.center, worldScale);
                        Vector3 popupCenter = capsuleCollider.ClosestPoint(new Vector3(centerPos.x, headHeight, centerPos.z));
                        popupTransform.position = popupCenter;
                    }
                }
            }
            else
            {
                animator.SetTrigger("Trigger");
                if (type != 0)
                    avatarPedestal.gameObject.SetActive(true);

                SendCustomEventDelayedFrames(nameof(_DelayAvatarUse), 1, VRC.Udon.Common.Enums.EventTiming.Update);

                _CloseWindow();
            }
        }

        public void _DelayAvatarUse()
        {
            avatarPedestal.SetAvatarUse(Networking.LocalPlayer);
        }

        public void _CloseWindow()
        {
            animator.SetBool("Open", false);
            hasOpen = false;
        }

        public void _OnWindowClosed()
        {

            if (type != 0)
                avatarPedestal.gameObject.SetActive(false);

            animator.Rebind();
            animator.enabled = false;
        }
        private void OnDisable()
        {
            hasOpen = false;
            _OnWindowClosed();
        }
    }
}