
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    public class VketLanguageSwitcherSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        
        private VketLanguageSwitcher _vketLanguageSwitcher;
        private SerializedObject _vketLanguageSwitcherSerializedObject;
        
        private SerializedProperty _jpSwitchObjects;
        private SerializedProperty _enSwitchObjects;
        private Sprite _switchToEnglishSprite;
        private Sprite _switchToJapaneseSprite;

        private SpriteRenderer _spriteRenderer;
        
        #endregion
        
        #region const定義
        private const string InteractObjectsRootName = "Visual";
        #endregion
        
        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 500f);

            Transform _interactObjectsRoot = null;
            if (_vketPrefabInstance)
            {
                _vketLanguageSwitcher = _vketPrefabInstance.GetComponent<VketLanguageSwitcher>();
                _interactObjectsRoot = _vketPrefabInstance.Find(InteractObjectsRootName);
            }
            
            if (_vketLanguageSwitcher)
            {
                _vketLanguageSwitcherSerializedObject = new SerializedObject(_vketLanguageSwitcher);
                _jpSwitchObjects = _vketLanguageSwitcherSerializedObject.FindProperty("jpSwitchObjects");
                _enSwitchObjects = _vketLanguageSwitcherSerializedObject.FindProperty("enSwitchObjects");
                _switchToEnglishSprite = _vketLanguageSwitcher.GetProgramVariable("switchToEnglishSprite") as Sprite;
                _switchToJapaneseSprite = _vketLanguageSwitcher.GetProgramVariable("switchToJapaneseSprite") as Sprite;
            }

            if (_interactObjectsRoot)
            {
                _spriteRenderer = _interactObjectsRoot.GetComponent<SpriteRenderer>();
            }
        }
        
        private void OnGUI()
        {
            InitStyle();
            
            if(!BaseHeader("VketLanguageSwitcher"))
                return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            
            GUILayout.Space(3);

            /* "1.サイズの調整" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketLanguageSwitcherSettingWindow.SizeSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            var scale = EditorGUILayout.Vector2Field("Scale", _vketPrefabInstance.localScale);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(_vketPrefabInstance, "Change Scale");
                _vketPrefabInstance.localScale = new Vector3(scale.x, scale.y, 1f);
            }

            GUILayout.Space(3);
                
            /* "2.英語への切り替え画像の設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketLanguageSwitcherSettingWindow.SwitchToEnglishSpriteSetting"), _l3Style);
                
            if (_spriteRenderer == null)
            {
                EditorGUILayout.HelpBox("Not Found \"Visual\" Sprite Renderer", MessageType.Error);
            }
            else
            {
                EditorGUI.BeginChangeCheck();
                var aswitchToEnglishSprite = SpriteField(_switchToEnglishSprite);
                if (EditorGUI.EndChangeCheck())
                {
                    _spriteRenderer.sprite = aswitchToEnglishSprite;
                    _vketLanguageSwitcher.SetProgramVariable("switchToEnglishSprite", aswitchToEnglishSprite);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(_spriteRenderer);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(_vketLanguageSwitcher);
                }
            }
                
            GUILayout.Space(3);
            
            /* "3.日本語への切り替え画像の設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketLanguageSwitcherSettingWindow.SwitchToJapaneseSpriteSetting"), _l3Style);
                
            EditorGUI.BeginChangeCheck();
            var switchToJapaneseSprite = SpriteField(_switchToJapaneseSprite);
            if (EditorGUI.EndChangeCheck())
            {
                _vketLanguageSwitcher.SetProgramVariable("switchToJapaneseSprite", switchToJapaneseSprite);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketLanguageSwitcher);
            }
                
            GUILayout.Space(3);

            /* "4.日本語オブジェクトの設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketLanguageSwitcherSettingWindow.JpObject"), _l3Style);
                
            _vketLanguageSwitcherSerializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_jpSwitchObjects);
            
            /* "日本語設定時にアクティブ化、英語設定時に非アクティブ化されます。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketLanguageSwitcherSettingWindow.JpObject.Help"), MessageType.Info);
            
            GUILayout.Space(3);
            
            /* "5.英語オブジェクトの設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketLanguageSwitcherSettingWindow.EnObject"), _l3Style);
            
            EditorGUILayout.PropertyField(_enSwitchObjects);
            if (EditorGUI.EndChangeCheck())
            {
                _vketLanguageSwitcherSerializedObject.ApplyModifiedProperties();
            }
            
            /* "英語設定時にアクティブ化、日本語設定時に非アクティブ化されます。最初は非アクティブに設定しておく必要があります。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketLanguageSwitcherSettingWindow.EnObject.Help"), MessageType.Info);
                
            GUILayout.Space(3);

            EditorGUILayout.EndScrollView();
            
            BaseFooter("VketLanguageSwitcher");
        }
    }
}
