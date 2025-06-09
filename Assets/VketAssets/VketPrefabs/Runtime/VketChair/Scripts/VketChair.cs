
using UdonSharp;
using VRC.SDKBase;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketChair : UdonSharpBehaviour
    {
        public override void Interact()
        {
            Networking.LocalPlayer.UseAttachedStation();
        }
    }
}