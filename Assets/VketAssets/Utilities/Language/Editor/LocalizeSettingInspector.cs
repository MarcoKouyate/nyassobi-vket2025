using System.Linq;
using UnityEditor;
using UnityEngine;
using VketAssets.Utilities.Language.Runtime;

namespace VketAssets.Utilities.Language.Editor
{
    [CustomEditor(typeof(LocalizedSetting))]
    public class LocalizeSettingInspector : UnityEditor.Editor
    {
        private SerializedProperty _script;
        private LocalizedSetting _localizeSetting;
        private int _selectLanguageIndex;
        private SerializedProperty _selectableLanguagesProperty;
        private SerializedProperty _currentLanguage;

        private void OnEnable()
        {
            _script = serializedObject.FindProperty("m_Script");
            _localizeSetting = target as LocalizedSetting;
            _currentLanguage = serializedObject.FindProperty("_currentLanguage");
            _selectableLanguagesProperty = serializedObject.FindProperty("_selectableLanguages");
            for (int i = 0; i < _selectableLanguagesProperty.arraySize; i++)
            {
                if (_selectableLanguagesProperty.GetArrayElementAtIndex(i).intValue == _currentLanguage.intValue)
                {
                    _selectLanguageIndex = i;
                    break;
                }
            }
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_script);
            }

            GUILayout.Space(5f);
            
            EditorGUILayout.PropertyField(_selectableLanguagesProperty);
            
            if (_localizeSetting.SelectableLanguages.Any())
            {
                EditorGUI.BeginChangeCheck();
                _selectLanguageIndex = EditorGUILayout.Popup("CurrentLanguage", _selectLanguageIndex,
                    _localizeSetting.SelectableLanguages.Select(x => x.ToString()).ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    _currentLanguage.intValue = (int)_localizeSetting.SelectableLanguages[_selectLanguageIndex];
                    // 変更を適用してからコールバックを実行
                    serializedObject.ApplyModifiedProperties();
                    _localizeSetting.OnChangeLanguage();
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}