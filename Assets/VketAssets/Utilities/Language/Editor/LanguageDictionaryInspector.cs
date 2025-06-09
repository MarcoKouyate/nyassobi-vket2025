using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using VketAssets.Utilities.Language.Runtime;

namespace VketAssets.Utilities.Language.Editor
{
    [CustomEditor(typeof(LanguageDictionary))]
    public class LanguageDictionaryInspector : UnityEditor.Editor
    {
        private SerializedProperty _script;
        [SerializeField]
        private TreeViewState _treeViewState;

        private LanguageDictionary _dictionary;

        private LanguageDictionaryTreeView _treeView;
        private SearchField _searchField;

        private GUIStyle _wordWrapTextAreaStyle;

        private void OnEnable()
        {
            _dictionary = target as LanguageDictionary;
            _script = serializedObject.FindProperty("m_Script");
            _treeView = LanguageDictionaryTreeView.CreateInstance(
                _dictionary,
                ref _treeViewState);

            _searchField = new SearchField();
            _searchField.downOrUpArrowKeyPressed += _treeView.SetFocusAndEnsureSelectedItem;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(_script);
            }
            
            _treeView.searchString = _searchField.OnGUI(_treeView.searchString);

            var rect = EditorGUILayout.GetControlRect(false, 200);
            _treeView.OnGUI(rect);

            GUILayout.Space(10);

            SelectedMessageView();
            serializedObject.ApplyModifiedProperties();
        }

        private void SelectedMessageView()
        {
            if (_wordWrapTextAreaStyle == null)
            {
                _wordWrapTextAreaStyle = new GUIStyle(EditorStyles.textArea);
                _wordWrapTextAreaStyle.wordWrap = true;
            }

            EditorGUI.BeginChangeCheck();
            var selections = _treeView.GetSelection();
            var translations = _dictionary.Language;
            foreach (var selection in selections)
            {
                GUILayout.Space(10);
                var selected = translations[selection];
                GUILayout.Label(selected.Key, EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                translations[selection].Value = EditorGUILayout.TextArea(selected.Value, _wordWrapTextAreaStyle);
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_dictionary);
            }
        }
    }
}