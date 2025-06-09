
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using Vket.EssentialResources.Attribute;
using VRC.SDK3.Data;
using VRC.SDK3.Image;
using VRC.SDK3.StringLoading;
using VRC.SDKBase;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketStorePreviewWindow : UdonSharpBehaviour
    {
        [SerializeField] private Animator canvasAnimator, previewImageBox, requeestYesNoBox;
        [SerializeField] private Text titleText, titleText2, categoryText, shopNameText, shopDescriptionText, tagsText, eventsText;
        [SerializeField] private RawImage webDisplayer, shopIcon, slide0, slide1, slide2, slide3;
        [SerializeField] private GameObject imageLoadingObject, dataLoadingObject, dataLoadingFailedObject,requestWindowFavText,requestWindowCartText; 
        [SerializeField] private ScrollRect webViewScrollRect, variationScrollRect;
        [SerializeField] private RectTransform variationScrollRectContent;
        [SerializeField] private MeshRenderer variationItemClickReference;
        [SerializeField] private TextMeshProUGUI dataLoadingFailedText;

        [SerializeField] private TextureInfo textureInfo;
        [SerializeField] private Material dummyMat;

        private VRCImageDownloader imageDownloader;
        private IVRCImageDownload progress;
        [SerializeField, SelfComponent] private UdonBehaviour udonBehaviour;
        private UdonBehaviour currentCallBack;
        private ButtonClickWaitState buttonClickWaitState;
        private VRCUrl currentVRCURL;
        private void OnEnable()
        {
            imageDownloader = new VRCImageDownloader();
        }
        /// <summary>
        /// 激活显示.
        /// </summary>
        /// <param name="active">激活状态</param>
        /// <param name="targetTransform">目标坐标.如果为null则移动到玩家前方</param>
        private void SetActive(bool active, Transform targetTransform = null)
        {
            if (active)
            {
                //清除上一次new出来的Texture2D防止内存泄露
                Destroy(webDisplayer.texture);
                Destroy(shopIcon.texture);
                Destroy(slide0.texture);
                Destroy(slide1.texture);
                Destroy(slide2.texture);
                Destroy(slide3.texture);
                //Set UI To Init State
                imageLoadingObject.gameObject.SetActive(true);
                dataLoadingObject.gameObject.SetActive(true);
                dataLoadingFailedObject.gameObject.SetActive(false);
                titleText.text = "Loading...";
                titleText2.text = "N/A";
                categoryText.text = "N/A";
                shopNameText.text = "N/A";
                shopDescriptionText.text = "N/A";
                eventsText.text = "N/A";
                tagsText.text = "N/A";
                ClearVariationItemList();
                webViewScrollRect.verticalNormalizedPosition = 1f;
                variationScrollRect.verticalNormalizedPosition = 1f;
                previewImageBox.SetTrigger("RST");

                if (targetTransform)
                {
                    //移动到指定坐标前方
                    var transf = targetTransform;
                    canvasAnimator.transform.position = transf.position;
                    canvasAnimator.transform.rotation = transf.rotation * Quaternion.AngleAxis(180, Vector3.up);
                    canvasAnimator.transform.position += canvasAnimator.transform.forward * -.05f;
                }
                else
                {
                    //移动到玩家前方
                    var head = Networking.LocalPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                    canvasAnimator.transform.position = head.position;
                    canvasAnimator.transform.rotation = head.rotation;
                    canvasAnimator.transform.position += canvasAnimator.transform.forward * .7f;
                }
            }
            else
            {
                requeestYesNoBox.SetTrigger("Close");
                requeestYesNoBox.Update(99);
                requeestYesNoBox.ResetTrigger("Open");
                imageDownloader.Dispose();
                imageLoadingObject.SetActive(false);
                dataLoadingObject.SetActive(false);
                dataLoadingFailedObject.gameObject.SetActive(false);
            }
            canvasAnimator.SetBool("Active", active);
        }
        /// <summary>
        /// 打开窗口
        /// </summary>
        /// <param name="dataURL">数据集URL</param>
        /// <param name="targetTransform">目标坐标.如果为null则移动到玩家前方</param>
        /// <param name="callBackUdon">
        /// _OnFavorites();
        /// _OnAddCart();
        /// _OnOpenInBrowser(); 
        /// 
        /// _OnAddCart()调用前会SetProgramVariable("OnAddCartID",[StringType]);来返回加入购物车的物品ID
        /// Udon CallBack
        /// </param>
        public void _OpenWindow(VRCUrl dataURL, UdonBehaviour callBackUdon, Transform targetTransform)
        {
            //Display Window
            SetActive(true, targetTransform);
            //Loading Data
            currentVRCURL=dataURL;
            VRCStringDownloader.LoadUrl(dataURL, udonBehaviour);
            //SetCallBack
            currentCallBack = callBackUdon;
        }
        #region ButtonEvent
        public void _CloseWindow()
        {
            SetActive(false);
        }
        public void _FavoritesButton()
        {
            requestWindowFavText.gameObject.SetActive(true);
            requestWindowCartText.gameObject.SetActive(false);
            buttonClickWaitState = ButtonClickWaitState.Favorites;
            requeestYesNoBox.SetTrigger("Open");
        }
        public void _AddCartButton()
        {
            requestWindowFavText.gameObject.SetActive(false);
            requestWindowCartText.gameObject.SetActive(true);
            buttonClickWaitState = ButtonClickWaitState.AddCart;
            requeestYesNoBox.SetTrigger("Open");
        }
        public void _OpenInBrowser()
        {
            if (currentCallBack)
            {
                currentCallBack.SendCustomEvent("_OnOpenInBrowser");
            }
        }
        public void _OpenStoreAuthentication()
        {
            if (currentCallBack)
            {
                currentCallBack.SendCustomEvent("_OpenStoreAuthentication");
            }
        }
        public void _FavoritesAndAddCartOKButton()
        {
            if (currentCallBack)
            {
                switch (buttonClickWaitState)
                {
                    case ButtonClickWaitState.None:
                        break;
                    case ButtonClickWaitState.AddCart:
                        currentCallBack.SetProgramVariable("OnAddCartIndex", variationItemClickReference.probeAnchor.GetSiblingIndex() - 1);
                        currentCallBack.SetProgramVariable("OnAddCartID", variationItemClickReference.probeAnchor.name);
                        currentCallBack.SendCustomEvent("_OnAddCart");
                        break;
                    case ButtonClickWaitState.Favorites:
                        currentCallBack.SendCustomEvent("_OnFavorites");
                        break;
                    default:
                        Debug.LogError("Not Support Event:" + buttonClickWaitState);
                        break;
                }
            }
        }
        #endregion
        #region DataLoaderCallBacks
        public override void OnStringLoadError(IVRCStringDownload result)
        {
            if (result.Url != currentVRCURL)
            {
                return;
            }
            dataLoadingObject.gameObject.SetActive(false);
            DisplayError("URL:" + result.Url + "\nError [" + result.ErrorCode + "]:" + result.Error);
        }
        public override void OnStringLoadSuccess(IVRCStringDownload resultData)
        {
            if (resultData.Url != currentVRCURL)
            {
                return;
            }
            var json = resultData.Result;
            if (VRCJson.TryDeserializeFromJson(json, out DataToken result))
            {
                #region MetaDisplay
                //DataGet
                var meta = result.DataDictionary["meta"].DataDictionary;
                //ItemName&&locale&&category
                string locale = meta["locale"].String;
                string itemName = meta["name"].String;
                string category = meta["category"].String;

                //variation
                var variations = meta["variations"].DataList;
                string[] variationIDs = new string[variations.Count];
                string[] variationNames = new string[variations.Count];
                string[] variationPrices = new string[variations.Count];
                for (int i = 0; i < variations.Count; i++)
                {
                    variationIDs[i] = variations[i].DataDictionary["id"].Double.ToString();
                    variationNames[i] = variations[i].DataDictionary["name"].String;
                    double priceDouble = variations[i].DataDictionary["price"].Double;
                    variationPrices[i] = priceDouble == 0 ? "Free" : priceDouble.ToString();
                }
                //Shop
                var shop = meta["shop"].DataDictionary;
                string shopName = shop["name"].String;
                string shopDescription = shop["description"].String;
                //event
                var eventsData = meta["events"].DataList;
                string[] events = new string[eventsData.Count];
                string eventsResultText = string.Empty;
                for (int i = 0; i < eventsData.Count; i++)
                {
                    events[i] = eventsData[i].String;
                    eventsResultText += events[i] + "    ";
                }
                //tag
                var tagsData = meta["tags"].DataList;
                string tagsResultText = string.Empty;
                string[] tags = new string[tagsData.Count];
                for (int i = 0; i < tagsData.Count; i++)
                {
                    tags[i] = tagsData[i].String;
                    tagsResultText += tags[i] + "   ";
                }
                //ApplyMetaDisplayValue 
                titleText.text = itemName;
                titleText2.text = itemName;
                categoryText.text = category;
                shopNameText.text = shopName;
                shopDescriptionText.text = shopDescription;
                eventsText.text = eventsResultText;
                tagsText.text = tagsResultText;
                InstanceVariationItem(variationIDs, variationNames, variationPrices);



                dataLoadingObject.gameObject.SetActive(false);
                #endregion

                #region ImageDisplay
                var textures = result.DataDictionary["textures"].DataDictionary;
                var keys = textures.GetKeys();
                for (int i = 0; i < keys.Count; i++)
                {
                    DataToken key = keys[i].String;//"Page" "shopIcon" "slide.0~3"
                    var textureSize = textures[key].DataDictionary["textureSize"].DataList;
                    var originalSize = textures[key].DataDictionary["originalSize"].DataList;
                    var data = textures[key].DataDictionary["data"].String;
                    var w = (int)textureSize[0].Double;
                    var h = (int)textureSize[1].Double;
#if UNITY_ANDROID
                    var tex = new Texture2D(w, h, TextureFormat.ETC_RGB4Crunched, false);
#else
                    var tex = new Texture2D(w, h, TextureFormat.DXT1Crunched, false);
#endif
                    byte[] data_ = System.Convert.FromBase64String(data);
                    // Debug.Log($"len: {data_.Length}, w: {w}, h: {h}");
                    tex.LoadRawTextureData(data_);
                    tex.Apply(updateMipmaps: false);
                    //SetTexture
                    switch (key.ToString())
                    {
                        case "page":
                            webDisplayer.texture = tex;
                            var aspectRatio = (float)(originalSize[0].Double / originalSize[1].Double);
                            var rect = webDisplayer.rectTransform;
                            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rect.rect.width / aspectRatio);
                            break;
                        case "shopIcon":
                            shopIcon.texture = tex;
                            break;
                        case "slide.0":
                            slide0.texture = tex;
                            break;
                        case "slide.1":
                            slide1.texture = tex;
                            break;
                        case "slide.2":
                            slide2.texture = tex;
                            break;
                        case "slide.3":
                            slide3.texture = tex;
                            break;
                        default:
                            break;
                    }
                    Debug.Log($"load-image done: {key}");
                }

                imageLoadingObject.gameObject.SetActive(false);
                #endregion
            }
            else
            {
                // Deserialization failed. Let's see what the error was.
                Debug.Log($"Failed to Deserialize json - {result.ToString()}");
                DisplayError("URL:" + resultData.Url + $"\nError:Failed to Deserialize json\n{result.ToString()}");
            }
        }
        #endregion
        #region UI 操作
        private void DisplayError(string msg)
        {
            dataLoadingFailedObject.gameObject.SetActive(true);
            dataLoadingFailedText.text = msg;
        }
        /// <summary>
        /// 清除变体列表
        /// </summary>
        private void ClearVariationItemList()
        {
            for (int i = 1; i < variationScrollRectContent.childCount; i++)
            {
                Destroy(variationScrollRectContent.GetChild(i).gameObject);
            }
        }
        /// <summary>
        /// 实例化变体对象
        /// 3个Array.Length需要一样的长度
        /// </summary>
        /// <param name="variationID">变体ID.按下按钮后回调会带着此ID回调回去</param>
        /// <param name="variationNames">变体名称</param>
        /// <param name="prices"><变体价格/param>
        private void InstanceVariationItem(string[] variationID, string[] variationNames, string[] prices)
        {
            Debug.Assert(variationID.Length == prices.Length && prices.Length == variationNames.Length);
            for (int i = 0; i < variationNames.Length; i++)
            {
                GameObject inst = Instantiate(variationScrollRectContent.GetChild(0).gameObject, variationScrollRectContent);
                inst.name = variationID[i];
                inst.transform.Find("Content/Name").GetComponent<Text>().text = variationNames[i];
                inst.transform.Find("Content/Price").GetComponent<Text>().text = prices[i];
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one;
                inst.gameObject.SetActive(true);
            }
        }
        #endregion
    }

    public enum ButtonClickWaitState
    {
        None,
        AddCart,
        Favorites
    }
}