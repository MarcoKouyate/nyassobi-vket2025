
using UdonSharp;
using UnityEngine;
using Vket.EssentialResources.Attribute;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketWebPageOpener : UdonSharpBehaviour
    {
        [SerializeField]
        private CapsuleCollider capsuleCollider;
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private Transform popupTransform;
        [SerializeField]
        private bool autoAdjustPosition = true;
        [SerializeField]
        private int pageId;
        [SerializeField]
        private int type; // 0:Circle2D, 1:Circle3D
        [SerializeField]
        private UdonBehaviour webPageOpener;

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
                if (webPageOpener != null)
                {
                    webPageOpener.SetProgramVariable("pageId", pageId);
                    if (type <= 1)
                        webPageOpener.SendCustomEvent("_OpenCirclePage");
                    else
                        webPageOpener.SendCustomEvent("_OpenItemPage");
                }

                _CloseWindow();
            }
        }

        public void _CloseWindow()
        {
            animator.SetBool("Open", false);
            hasOpen = false;
        }

        public void _OnWindowClosed()
        {
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