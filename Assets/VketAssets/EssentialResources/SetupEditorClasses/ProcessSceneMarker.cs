using UnityEngine;
using UnityEngine.Serialization;

namespace Vket.EssentialResources
{
    [IgnoreBuild]
    public class ProcessSceneMarker : MonoBehaviour
    {
        [FormerlySerializedAs("MarkerTag")] [SerializeField]
        protected string _tag = "";

        public string Tag => _tag;
    }
}