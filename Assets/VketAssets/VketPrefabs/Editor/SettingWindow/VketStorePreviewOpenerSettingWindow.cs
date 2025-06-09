using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    public class VketStorePreviewOpenerSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        
        private SerializedObject _vketStorePreviewOpenerSo;
        
        // 2Dの設定画像
        private Image _image;
        
        private Transform _interactObjectsRoot;
        private GameObject _interactObject;
        
        #endregion
        
        #region const定義
        private const string InteractObjectsRootName = "Visual";
        #endregion

        private readonly string _prefabName = "VketStorePreviewOpener";
        
        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 500f);

            if (_vketPrefabInstance)
            {
                _vketStorePreviewOpenerSo =
                    new SerializedObject(_vketPrefabInstance.GetComponent<VketStorePreviewOpener>());
                _interactObjectsRoot = _vketPrefabInstance.Find(InteractObjectsRootName);
                
                var imageTransform = _vketPrefabInstance.transform.Find("Canvas")?.Find("Image");
                if(imageTransform)
                    _image = imageTransform.GetComponent<Image>();
            }
            
            // 見た目のオブジェクトを取得
            if (_interactObjectsRoot && _interactObjectsRoot.childCount != 0)
            {
                _interactObject = _interactObjectsRoot.GetChild(0).gameObject;
            }
        }
        
        private void OnGUI()
        {
            InitStyle();
            
            if(!BaseHeader(_prefabName))
                return;
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            
            GUILayout.Space(3);

            /* "1.サイズの調整" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketStorePreviewOpenerSettingWindow.SizeSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            var scale = EditorGUILayout.Vector2Field("Scale", _vketPrefabInstance.localScale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_vketPrefabInstance, "Change Scale");
                _vketPrefabInstance.localScale = new Vector3(scale.x, scale.y, 1f);
            }

            GUILayout.Space(3);
                
            /* "2.画像設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketStorePreviewOpenerSettingWindow.TextureSetting"), _l3Style);
                
            if (_image == null)
            {
                EditorGUILayout.HelpBox("Not Found \"Canvas/Image\"", MessageType.Error);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var imageSprite = SpriteField(_image.sprite);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_image, "Modify source image");
                    _image.sprite = imageSprite;
                    _image.enabled = false;
                    _image.enabled = true;
                }
            }
                
            GUILayout.Space(3);
            
            /* "3.Item IDを指定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketStorePreviewOpenerSettingWindow.ItemIdSetting"), _l3Style);
            
            _vketStorePreviewOpenerSo.Update();
            
            var vrcurlprop = _vketStorePreviewOpenerSo.FindProperty("editor_ItemURL").FindPropertyRelative("url");
            EditorGUILayout.LabelField("Item URL", vrcurlprop.stringValue);
            
            if (int.TryParse(vrcurlprop.stringValue.Replace("https://store.vket.com/ja/items/", ""), out var id))
            {
                EditorGUI.BeginChangeCheck();
                var itemId = EditorGUILayout.IntField("Item Id", id);
                if (EditorGUI.EndChangeCheck())
                {
                    vrcurlprop.stringValue = $"https://store.vket.com/ja/items/{itemId}";
                    _vketStorePreviewOpenerSo.FindProperty("pageId").intValue = itemId;
                }
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var itemId = EditorGUILayout.IntField("Item Id", 0);
                if (EditorGUI.EndChangeCheck())
                {
                    vrcurlprop.stringValue = $"https://store.vket.com/ja/items/{itemId}";
                }
            }
            
            /* "商品ページのItemIDを設定してください。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketStorePreviewOpenerSettingWindow.ItemIdSetting.Help"), MessageType.Info);
            
            if (Uri.TryCreate(vrcurlprop.stringValue, UriKind.Absolute, out Uri uri))
            {
                // 检查URI是否是HTTPS以及Host来源
                if (uri.Scheme == Uri.UriSchemeHttps &&
                    uri.Host.Equals("store.vket.com", StringComparison.OrdinalIgnoreCase))
                {
                    string IDResult = string.Empty;
                    // 正则表达式用于匹配items后面跟随的数字（即ID）
                    string pattern = @"items/(\d+)";

                    // 使用正则表达式匹配URI
                    Match match = Regex.Match(vrcurlprop.stringValue.Trim(), pattern);

                    // 检查是否找到匹配项
                    if (match.Success)
                    {
                        // 返回匹配到的第一个组（即ID）
                        IDResult = match.Groups[1].Value;
                    }

                    if (IDResult != string.Empty)
                    {
                        if (GUILayout.Button("Preview Item " + IDResult))
                        {
                            Application.OpenURL(vrcurlprop.stringValue);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"URL ItemID Not Found!", MessageType.Error);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("Url Is Invalid!", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Url Is Invalid!", MessageType.Error);
            }

            /* "4.プレビュー設定*/
            EditorGUILayout.LabelField(/* "4.プレビュー設定" */ LocalizedUtility.Get("VketStorePreviewOpenerSettingWindow.PreviewSetting"), _l3Style);
            if (GUILayout.Button(/* "DisplayPoint Transformを選択" */ LocalizedUtility.Get("VketStorePreviewOpenerSettingWindow.PreviewSetting.SelectDisplayPointTransform")))
            {
                Selection.activeObject = _vketStorePreviewOpenerSo.FindProperty("overrideDisplayTransform").objectReferenceValue;
            }
            EditorGUILayout.HelpBox(/* "DisplayPointオブジェクトのTransformを調整してプレビューの表示位置を変更できます。" */ LocalizedUtility.Get("VketStorePreviewOpenerSettingWindow.PreviewSetting.Help"), MessageType.Info);
            GUILayout.Space(3);

            _vketStorePreviewOpenerSo.ApplyModifiedProperties();
            
            EditorGUILayout.EndScrollView();
            
            BaseFooter(_prefabName, _interactObject);
        }
    }
}
