
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.SDK3.Image;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketImageDownloader : UdonSharpBehaviour
    {
        [SerializeField] private VRCUrl url;
        [SerializeField] private TextureInfo textureInfo;
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private int materialIndex;

        private VRCImageDownloader imageDownloader;
        private IVRCImageDownload progress;
        private UdonBehaviour udonBehaviour;
        private Material targetMaterial;
        private Texture unloadTexture;
        private bool hasInitialized;

        private void OnDestroy()
        {
            if (progress != null && progress.State == VRCImageDownloadState.Complete)
                imageDownloader.Dispose();
        }

        private void Initialize()
        {
            udonBehaviour = GetComponent<UdonBehaviour>();
            targetMaterial = targetRenderer.materials[materialIndex];
            unloadTexture = targetMaterial.GetTexture(textureInfo.MaterialProperty);
            hasInitialized = true;
        }

        public void _Dispose()
        {
            imageDownloader.Dispose();
            targetMaterial.SetTexture(textureInfo.MaterialProperty, unloadTexture);
        }

        public void _VketOnBoothEnter()
        {
            if (progress == null || progress.State == VRCImageDownloadState.Unloaded)
            {
                if (!hasInitialized)
                    Initialize();

                imageDownloader = new VRCImageDownloader();
                progress = imageDownloader.DownloadImage(url, targetMaterial, udonBehaviour, textureInfo);
            }
        }

        public override void OnImageLoadError(IVRCImageDownload result)
        {
            Debug.Log(result.ErrorMessage);
        }
    }
}