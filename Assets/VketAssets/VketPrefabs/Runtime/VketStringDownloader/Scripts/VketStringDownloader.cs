
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.StringLoading;
using UnityEngine.UI;
using TMPro;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketStringDownloader : UdonSharpBehaviour
    {
        [SerializeField] private VRCUrl url;
        [SerializeField] private Component targetText;

        private bool hasLoaded;

        public void _VketOnBoothEnter()
        {
            if (!hasLoaded)
            {
                VRCStringDownloader.LoadUrl(url, GetComponent<UdonBehaviour>());
                hasLoaded = true;
            }
        }

        public override void OnStringLoadSuccess(IVRCStringDownload result)
        {
            var type = targetText.GetType();
            if (type == typeof(Text))
            {
                var text = (Text)targetText;
                text.text = result.Result;
            }
            else if (type == typeof(TextMeshPro))
            {
                var text = (TextMeshPro)targetText;
                text.text = result.Result;
            }
            else if (type == typeof(TextMeshProUGUI))
            {
                var text = (TextMeshProUGUI)targetText;
                text.text = result.Result;
            }
        }

        public override void OnStringLoadError(IVRCStringDownload result)
        {
            Debug.Log(result.Error);
        }
    }
}