using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace VketAssets.Utilities.Language.Runtime
{
    public class LocalizedSetting : ScriptableObject
    {
        public delegate void LanguageChangingCallback(int languageId);
        public static event LanguageChangingCallback LanguageChanging = delegate {  };
        
        public const SystemLanguage DefaultLanguage = SystemLanguage.English;

        [SerializeField] private SystemLanguage _currentLanguage;

        [SerializeField] private List<SystemLanguage> _selectableLanguages = new List<SystemLanguage>()
        {
            SystemLanguage.Unknown,
            SystemLanguage.Japanese,
            SystemLanguage.English,
        };

        public List<SystemLanguage> SelectableLanguages => _selectableLanguages;

        public SystemLanguage Language =>
            _currentLanguage == SystemLanguage.Unknown ? Application.systemLanguage : _currentLanguage;
        
        private const string SettingGuid = "df7846fd00bae6841b83ad035bd2d02c"; // Assets/VketAssets/Utilities/Language/LocalizedSetting.asset
        private static LocalizedSetting _instance;

        public static LocalizedSetting Instance
        {
            get
            {
#if UNITY_EDITOR
                if (_instance)
                    return _instance;

                var assetPath = AssetDatabase.GUIDToAssetPath(SettingGuid);
                _instance = AssetDatabase.LoadAssetAtPath<LocalizedSetting>(assetPath);
#endif
                return _instance;
            }
        }
        
#if UNITY_EDITOR
        
        public void SetLanguage(SystemLanguage language)
        {
            _currentLanguage = language;
            OnChangeLanguage();
        }

        /// <summary>
        /// EditorWindowの言語変更とコールバックに登録されている言語変更を呼ぶ
        /// </summary>
        public void OnChangeLanguage()
        {
            Debug.Log($"LocalizeSetting:OnChangeLanguage {_currentLanguage}");
            LanguageLoader.OnChangeLanguage();

            // ILocalizeを実装しているEditorWindowのOnChangeLanguageを実行
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window.GetType().GetInterfaces().Contains(typeof(ILocalized)))
                {
                    var method = window.GetType().GetMethod(nameof(ILocalized.OnChangeLanguage));
                    if (method != null)
                    {
                        Debug.Log($"Invoke: {window.GetType()}.OnChangeLanguage");
                        method.Invoke(window, new object[] {});
                        // 適用したEditorWindowの描画更新
                        window.Repaint();
                    }
                }
            }
            
            // ILocalizeを実装しているEditorのOnChangeLanguageを実行
            foreach (var editor in Resources.FindObjectsOfTypeAll<Editor>())
            {
                // ILocalizeを実装している場合はOnChangeLanguageを実行
                if (editor.GetType().GetInterfaces().Contains(typeof(ILocalized)))
                {
                    var method = editor.GetType().GetMethod(nameof(ILocalized.OnChangeLanguage));
                    if (method != null)
                    {
                        Debug.Log($"Invoke: {editor.GetType()}.OnChangeLanguage");
                        method.Invoke(editor, new object[] {});
                        // 適用したEditorWindowの描画更新
                        editor.Repaint();
                    }
                }
            }
            
            LanguageChanging((int)_currentLanguage);
            
            // Inspectorの描画更新
            RepaintInspector();
            // シーンの描画更新
            EditorApplication.QueuePlayerLoopUpdate();
        }

        /// <summary>
        /// Inspector描画更新
        /// </summary>
        private static void RepaintInspector()
        {
            var assembly  = Assembly.Load( "UnityEditor" );
            var type      = assembly.GetType( "UnityEditor.InspectorWindow" );
            var inspector = EditorWindow.GetWindow( type );
            
            inspector.Repaint();
        }
#endif
    }
}
