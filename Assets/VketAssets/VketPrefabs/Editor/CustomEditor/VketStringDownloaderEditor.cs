
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketStringDownloader))]
    public class VketStringDownloaderEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket String Downloader", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(64));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketStringDownloaderEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("url"));

            var textProp = serializedObject.FindProperty("targetText");
            EditorGUI.BeginChangeCheck();
            var component = (Component)EditorGUILayout.ObjectField("Target Text", textProp.objectReferenceValue, typeof(Component), true);
            if (EditorGUI.EndChangeCheck())
            {
                if (component != null)
                {
                    var type = component.GetType();
                    if (type == typeof(Text) || type == typeof(TextMeshPro) || type == typeof(TextMeshProUGUI))
                    {
                        textProp.objectReferenceValue = component;
                    }
                    else
                    {
                        var textComponent = GetTextComponent(component);
                        if (textComponent != null)
                            textProp.objectReferenceValue = textComponent;
                    }
                }
                else
                {
                    textProp.objectReferenceValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private Component GetTextComponent(Component component)
        {
            Component textComponent;

            textComponent = component.GetComponent<Text>();
            if (textComponent != null)
                return textComponent;

            textComponent = component.GetComponent<TextMeshPro>();
            if (textComponent != null)
                return textComponent;

            textComponent = component.GetComponent<TextMeshProUGUI>();
            return textComponent;
        }
    }
}
