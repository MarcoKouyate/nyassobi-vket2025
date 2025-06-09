
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketFittingChair))]
    public class VketFittingChairEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private VketFittingChair _vketFittingChair;

        private void OnEnable()
        {
            _vketFittingChair = target as VketFittingChair;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Fitting Chair", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(108));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketFittingChairEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();
            
            EditorGUI.BeginDisabledGroup(_vketFittingChair.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketFittingChairSettingWindow>(_vketFittingChair.transform);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}