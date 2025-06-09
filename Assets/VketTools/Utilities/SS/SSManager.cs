#if UNITY_EDITOR
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace VketTools.Utilities.SS
{
    public class SSManager : MonoBehaviour
    {
        public Vector2 SSSize;
        public Camera capCamera;
        public Toggle previewPlaceHopeToggle;
        public Toggle mainSubmissionToggle;
        public Toggle dcThumbnailChangeToggle;
        public Text screenshotMessageText;

        [SerializeField]
        private Camera camvasCamera;
        [SerializeField]
        private GameObject SSItemTexObj;
        [SerializeField]
        private Button okButton;
        [SerializeField]
        private Image okButtonBackground;
        [SerializeField]
        private Text okButtonText;

        private Texture2D texture;

        public void CaptureButton_Click()
        {
            StartCoroutine(Capture());
        }

        public async void OKButton_Click()
        {
            var editorPlayInfo = AssetUtility.EditorPlayInfoData;
            if (texture != null)
            {
                editorPlayInfo.SsPath = await AssetUtility.SaveSS(texture);
            }

            editorPlayInfo.SsSuccessFlag = true;
            EditorUtility.SetDirty(editorPlayInfo);
            EditorUtility.SetDirty(AssetUtility.LoginInfoData);
            AssetDatabase.SaveAssets();
            EditorApplication.isPlaying = false;
        }

        private IEnumerator Capture()
        {
            yield return new WaitForEndOfFrame();

            Texture2D ssTexture = new Texture2D((int)SSSize.x, (int)SSSize.y, TextureFormat.RGB24, false);
            RenderTexture renderTexture = new RenderTexture((int)SSSize.x, (int)SSSize.y, 24);
            RenderTexture prevTexture = capCamera.targetTexture;

            capCamera.targetTexture = renderTexture;
            capCamera.Render();
            capCamera.targetTexture = prevTexture;
            RenderTexture.active = renderTexture;
            ssTexture.ReadPixels(new Rect(0, 0, SSSize.x, SSSize.y), 0, 0);
            ssTexture.Apply();
            texture = ssTexture;
            SSItemTexObj.GetComponent<RawImage>().texture = ssTexture;
            okButton.interactable = true;
            okButtonBackground.color = Color.white;
            var c = okButtonText.color;
            c.a = 1;
            okButtonText.color = c;
        }
    }
}
#endif