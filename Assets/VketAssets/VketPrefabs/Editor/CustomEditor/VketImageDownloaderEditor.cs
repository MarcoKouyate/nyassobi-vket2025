
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketImageDownloader))]
    public class VketImageDownloaderEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Image Downloader", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(94));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketImageDownloaderEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("url"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetRenderer"));
            var renderer = (Renderer)serializedObject.FindProperty("targetRenderer").objectReferenceValue;
            if (renderer != null)
            {
                var indexProp = serializedObject.FindProperty("materialIndex");
                EditorGUI.BeginChangeCheck();
                var index = EditorGUILayout.Popup("Target Material", indexProp.intValue, GetMaterialNames(renderer));
                if (EditorGUI.EndChangeCheck())
                {
                    indexProp.intValue = index;
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Popup("Target Material", 0, new string[] { "" });
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.PropertyField(serializedObject.FindProperty("textureInfo"));

            serializedObject.ApplyModifiedProperties();
        }

        private string[] GetMaterialNames(Renderer renderer)
        {
            var materials = renderer.sharedMaterials;

            var names = new string[materials.Length];
            for(int i=0; i < materials.Length; i++)
                names[i] = materials[i].name;

            return names;
        }
    }
}
