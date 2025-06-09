
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketFittingChair : UdonSharpBehaviour
    {
        [SerializeField] private float fitSpeed = 6.0f;
        [SerializeField] private VRCStation station;

        private VRCPlayerApi seatPlayer;
        private Transform enterLocation;
        private Vector3 hipTarget;
        private Vector3 localEnterPosition;

        private void Start()
        {
            enterLocation = station.stationEnterPlayerLocation;
            hipTarget = enterLocation.position;
            localEnterPosition = enterLocation.localPosition;
        }

        public override void Interact()
        {
            Networking.LocalPlayer.UseAttachedStation();
        }

        public override void PostLateUpdate()
        {
            if (seatPlayer == null)
                return;

            if (!seatPlayer.IsValid())
            {
                seatPlayer = null;
                enterLocation.position = hipTarget;
                enterLocation.localPosition = localEnterPosition;
                return;
            }

            float hipHeight = seatPlayer.GetBonePosition(HumanBodyBones.Hips).y;
            float height = hipTarget.y;
            float h = Slerp(hipHeight, height, fitSpeed * Time.deltaTime);
            float dist = h - hipHeight;
            enterLocation.position += dist * Vector3.up;
        }

        public override void OnStationEntered(VRCPlayerApi player)
        {
            seatPlayer = player;

            if (seatPlayer.GetBonePosition(HumanBodyBones.Hips) == Vector3.zero)
                seatPlayer = null;
        }

        public override void OnStationExited(VRCPlayerApi player)
        {
            seatPlayer = null;
            enterLocation.localPosition = localEnterPosition;
        }

        private float Slerp(float a, float b, float t)
        {
            float vt = t * t * (3.0f - t * 2.0f);
            return a * (1 - vt) + b * vt;
        }
    }
}