
using UdonSharp;
using UnityEngine;
using Vket.EssentialResources.Attribute;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketStorePreviewOpener : UdonSharpBehaviour
    {
        [SerializeField, SceneSingleton]
        VketStorePreviewWindow vketStorePreviewWindow;
        [SerializeField]
        private int pageId;
        [SerializeField]
        private UdonBehaviour webPageOpener;
        [SerializeField, SelfComponent]
        private UdonBehaviour thisUdon;
#if UNITY_EDITOR
        [SerializeField]
        private VRCUrl editor_ItemURL;
#endif
        public VRCUrl dataURL_EN,dataURL_JP;
        public VRCUrl dataURL_android_EN, dataURL_android_JP;
        public int variationCount;
        [SerializeField]
        private Transform languageManager;

        [SerializeField]
        Transform overrideDisplayTransform;

        public override void Interact()
        {
            Transform displayTransform=overrideDisplayTransform;
            if (displayTransform==null)
            {
                displayTransform = this.transform;
            }
#if UNITY_ANDROID
            vketStorePreviewWindow._OpenWindow(GetIsEnglish()?dataURL_android_EN:dataURL_android_JP, thisUdon, displayTransform);
#else
            vketStorePreviewWindow._OpenWindow(GetIsEnglish() ? dataURL_EN : dataURL_JP, thisUdon, displayTransform);
#endif
        }

        public void _CloseWindow()
        {
            vketStorePreviewWindow._CloseWindow();
        }

        private bool GetIsEnglish()
        {
            if (languageManager==null)
            {
                return false;
            }
            return languageManager.Find("isEnglishStorage").gameObject.activeSelf;
        }

#region CallBacks
        public void _OnFavorites()
        {
            if (webPageOpener)
            {
                webPageOpener.SetProgramVariable("pageId", pageId);
                webPageOpener.SendCustomEvent("_FavoriteStoreItem");
            }
        }
        [HideInInspector] public string OnAddCartID;
        [HideInInspector] public int OnAddCartIndex;
        public void _OnAddCart()
        {
            if (webPageOpener)
            {
                webPageOpener.SetProgramVariable("pageId", pageId);
                if (OnAddCartIndex < variationCount)
                {
                    webPageOpener.SetProgramVariable("addCartIndex", OnAddCartIndex);
                    webPageOpener.SendCustomEvent("_AddCartStoreItem");
                }
                else
                {
                    webPageOpener.SendCustomEvent("_OpenItemPage");
                }
            }
        }
        public void _OnOpenInBrowser()
        {
            if (webPageOpener)
            {
                webPageOpener.SetProgramVariable("pageId", pageId);
                webPageOpener.SendCustomEvent("_OpenItemPage");
            }
        }
        public void _OpenStoreAuthentication()
        {
            if (webPageOpener)
            {
                webPageOpener.SendCustomEvent("_OpenStoreAuthentication");
            }
        }
#endregion

        private void OnDisable()
        {
            _CloseWindow();
        }
    }
}