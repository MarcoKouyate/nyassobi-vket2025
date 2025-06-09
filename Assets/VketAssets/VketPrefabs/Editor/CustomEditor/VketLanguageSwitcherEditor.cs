
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketLanguageSwitcher))]
    public class VketLanguageSwitcherEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private VketLanguageSwitcher _vketLanguageSwitcher;

        private void OnEnable()
        {
            _vketLanguageSwitcher = target as VketLanguageSwitcher;
        }
        
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Language Switcher", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(94));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketLanguageSwitcherEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("jpSwitchObjects"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("enSwitchObjects"));

            // Draw Field of Switch To English Sprite
            var enSpriteProperty = serializedObject.FindProperty("switchToEnglishSprite");
            EditorGUI.BeginChangeCheck();
            var toEnSprite = (Sprite)EditorGUILayout.ObjectField("Switch To English Sprite", enSpriteProperty.objectReferenceValue, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
            {
                enSpriteProperty.objectReferenceValue = toEnSprite;
                var spriteRenderer = (SpriteRenderer)serializedObject.FindProperty("spriteRenderer").objectReferenceValue;
                Undo.RecordObject(spriteRenderer, "Modify switch to english sprite");
                spriteRenderer.sprite = toEnSprite;
            }

            // Draw Field of Switch To Japanese Sprite
            var jpSpriteProperty = serializedObject.FindProperty("switchToJapaneseSprite");
            EditorGUI.BeginChangeCheck();
            var toJpSprite = (Sprite)EditorGUILayout.ObjectField("Switch To Japanese Sprite", jpSpriteProperty.objectReferenceValue, typeof(Sprite), false);
            if (EditorGUI.EndChangeCheck())
                jpSpriteProperty.objectReferenceValue = toJpSprite;

            serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginDisabledGroup(_vketLanguageSwitcher.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketLanguageSwitcherSettingWindow>(_vketLanguageSwitcher.transform);
            }
            EditorGUI.EndDisabledGroup();
            
            //base.OnInspectorGUI();
        }
    }
}