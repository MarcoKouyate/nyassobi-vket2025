
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketPrefabInformation))]
    public class VketPrefabInformationEditor : Editor
    {
        private SerializedProperty _prefabName;
        private SerializedProperty _descriptionKey;
        private SerializedProperty _repletionKey;
        private SerializedProperty _settingWindowWindowTag;

        /// <summary>
        /// 自動折り返し用スタイル
        /// </summary>
        private GUIStyle _wrapStyle;

        private void OnEnable()
        {
            _prefabName = serializedObject.FindProperty("_prefabName");
            _descriptionKey = serializedObject.FindProperty("_descriptionKey");
            _repletionKey　= serializedObject.FindProperty("_repletionKey");
            _settingWindowWindowTag = serializedObject.FindProperty("_settingWindowWindowTag");
        }

        private void InitStyle()
        {
            if (_wrapStyle != null)
                return;

            _wrapStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
        }

        public override void OnInspectorGUI()
        {
            InitStyle();

            serializedObject.Update();

            EditorGUILayout.PropertyField(_prefabName);

            EditorGUILayout.PropertyField(_descriptionKey);

            EditorGUILayout.LabelField("Description", LocalizedUtility.Get(_descriptionKey.stringValue), _wrapStyle);

            EditorGUILayout.PropertyField(_repletionKey);
            
            EditorGUILayout.LabelField("Repletion", LocalizedUtility.Get(_repletionKey.stringValue), _wrapStyle);
            
            EditorGUILayout.PropertyField(_settingWindowWindowTag);
            serializedObject.ApplyModifiedProperties();
        }
    }
}