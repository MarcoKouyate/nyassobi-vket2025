using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    public class VketGroupPageOpenerSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        
        private VketGroupPageOpener _vketGroupPageOpener;
        // 2Dの設定画像
        private Image _image;
        
        #endregion

        private readonly string _prefabName = "VketGroupPageOpener";
        private SerializedObject _so;
        private SerializedProperty _groupIdProp;
        private SerializedProperty _openToStorePage;
        
        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 500f);

            if (_vketPrefabInstance)
            {
                _vketGroupPageOpener = _vketPrefabInstance.GetComponent<VketGroupPageOpener>();
                var imageTransform = _vketPrefabInstance.transform.Find("Canvas")?.Find("Image");
                if(imageTransform)
                    _image = imageTransform.GetComponent<Image>();
                
                _so = new SerializedObject(_vketGroupPageOpener);
                _groupIdProp = _so.FindProperty("_groupId");
                _openToStorePage = _so.FindProperty("_openToStorePage");
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
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketGroupPageOpenerSettingWindow.SizeSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            var scale = EditorGUILayout.Vector2Field("Scale", _vketPrefabInstance.localScale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_vketPrefabInstance, "Change Scale");
                _vketPrefabInstance.localScale = new Vector3(scale.x, scale.y, 1f);
            }

            GUILayout.Space(3);
                
            /* "2.画像設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketGroupPageOpenerSettingWindow.TextureSetting"), _l3Style);
                
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
            
            /* "3.Group IDを指定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketGroupPageOpenerSettingWindow.GroupIdSetting.2D"), _l3Style);
            
            _so.Update();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(_groupIdProp);

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_groupIdProp.stringValue)))
            {
                if(GUILayout.Button("Open", GUILayout.Width(50)))
                {
                    Application.OpenURL($"https://vrchat.com/home/group/{_groupIdProp.stringValue}");
                }
            }
            
            EditorGUILayout.EndHorizontal();
            
            /* "GroupIDを設定してください。例:grp_0ce025bd-0491-4248-b6f4-c93ec056dccc" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketGroupPageOpenerSettingWindow.GroupIDSetting.Help"), MessageType.Info);
            
            /* "4.GroupのStorePage設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketGroupPageOpenerSettingWindow.StorePageSetting.2D"), _l3Style);
            
            EditorGUILayout.PropertyField(_openToStorePage);
            _so.ApplyModifiedProperties();
            
            /* "StorePageを開く場合はチェックを入れてください。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketGroupPageOpenerSettingWindow.StorePageSetting.Help"), MessageType.Info);
            
            EditorGUILayout.EndScrollView();
            
            BaseFooter(_prefabName);
        }
    }
}
