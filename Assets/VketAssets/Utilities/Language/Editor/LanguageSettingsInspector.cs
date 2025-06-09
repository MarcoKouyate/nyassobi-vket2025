using UnityEditor;
using VketAssets.Utilities.Language.Runtime;

namespace VketAssets.Utilities.Language.Editor
{
    [CustomEditor(typeof(LanguageSettings))]
    public class LanguageSettingsInspector : UnityEditor.Editor
    {
        private LanguageSettings _languageSettings;
        private SerializedProperty _script;
        private SerializedProperty _systemProperty;
        private SerializedProperty _tagsProperty;
        private SerializedProperty _languageProperty;
        private SerializedProperty _languageDictionaryListProperty;
        
        private void OnEnable()
        {
            _languageSettings = target as LanguageSettings;
            _script = serializedObject.FindProperty("m_Script");
            _systemProperty = serializedObject.FindProperty("_systemName");
            _tagsProperty = serializedObject.FindProperty("_tags");
            _languageDictionaryListProperty = serializedObject.FindProperty("_languageDictionaryList");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_script);
            }
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_systemProperty);
            EditorGUILayout.PropertyField(_tagsProperty);
            EditorGUILayout.PropertyField(_languageDictionaryListProperty);
            if (EditorGUI.EndChangeCheck())
            {
                _languageSettings.SetUpDictionary();
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}