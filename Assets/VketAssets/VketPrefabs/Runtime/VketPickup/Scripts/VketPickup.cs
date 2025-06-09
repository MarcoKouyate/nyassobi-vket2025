
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class VketPickup : UdonSharpBehaviour
    {
        [SerializeField]
        private AnimatorOverrideController overrideController;

        [SerializeField]
        private Animator animator;
        [SerializeField]
        private AnimatorOverrideController overrideControllerBase;
        [SerializeField]
        private VRCPickup vrcPickup;
        [SerializeField]
        private VRCObjectSync objectSync;

        private readonly float resetTime = 30.0f;

        private bool isPickup;
        private float remainWaitTime = 30.0f;

        private void Update()
        {
            if (!vrcPickup.IsHeld && remainWaitTime > 0)
            {
                remainWaitTime -= Time.deltaTime;
                if (remainWaitTime <= 0)
                    objectSync.Respawn();
            }
        }

        public void SendOnDrop()
        {
            animator.SetBool("Pickup", false);
            animator.SetBool("Use", false);
        }
        
        public void SendOnPickup()
        {
            remainWaitTime = resetTime;

            if (overrideController != null)
                animator.SetBool("Pickup", true);
        }

        public void SendOnPickupUseDown()
        {
            animator.SetBool("Use", true);
        }

        public void SendOnPickupUseUp()
        {
            animator.SetBool("Use", false);
        }

        public override void OnDrop()
        {
            if (overrideController != null)
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendOnDrop));
        }
        public override void OnPickup()
        {
            SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendOnPickup));
        }
        public override void OnPickupUseDown()
        {
            if (overrideController != null)
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendOnPickupUseDown));
        }
        public override void OnPickupUseUp()
        {
            if (overrideController != null)
                SendCustomNetworkEvent(VRC.Udon.Common.Interfaces.NetworkEventTarget.All, nameof(SendOnPickupUseUp));
        }
    }
}