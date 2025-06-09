
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    public class VketURLOpenerProxy : MonoBehaviour
    {
        [SerializeField] private VketURLOpener _vketURLOpener;
        [SerializeField] private VRCUrl _url;

        public VRCUrl Url { get => _url; }
        public VketURLOpener VketURLOpener { get => _vketURLOpener; }
    }
}