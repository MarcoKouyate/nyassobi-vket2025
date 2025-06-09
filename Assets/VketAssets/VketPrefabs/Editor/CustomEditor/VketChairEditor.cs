
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;


namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketChair))]
    public class VketChairEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private VketChair _vketChair;

        private void OnEnable()
        {
            _vketChair = target as VketChair;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Chair", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(94));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketChairEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(_vketChair.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketChairSettingWindow>(_vketChair.transform);
            }

            EditorGUI.EndDisabledGroup();
        }
    }
}