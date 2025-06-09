
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketMirror))]
    public class VketMirrorEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private readonly string[] _modeText = { "Auto", "Manual" };
        
        /*private readonly string manualInfoJa =
            "Manualモードにてミラーの表示非表示を切り替えるにはsendCustomEventにて \"_SetEnableMirror()\" か \"_SetDisableMirror()\" を送信してください\nColliderは実行時にdisableになります";
        private readonly string manualInfoEn =
            "To change display state of mirror in Manual mode, sendCustomEvent \"_SetEnableMirror()\" and \"_SetDisableMirror()\".\ncollider will be disabled at runtime.";*/
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var isManual = serializedObject.FindProperty("_isManualMode");
            int index = isManual.boolValue ? 1 : 0;
            var isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Mirror", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(158));
            var summaryText = LocalizedUtility.GetInspector(isManual.boolValue ? "VketMirrorEditor_Manual" : "VketMirrorEditor_Auto");
            EditorGUILayout.TextArea($"{LocalizedUtility.GetInspectorReadme("VketMirrorEditor")}\n{summaryText}");
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(isPlaying);
            EditorGUI.BeginChangeCheck();
            index = EditorGUILayout.Popup("Mode: ", index, _modeText);
            if (EditorGUI.EndChangeCheck())
            {
                isManual.boolValue = index == 1;
                var areaCollider = serializedObject.FindProperty("_areaCollider");
                if(areaCollider.objectReferenceValue != null)
                {
                    var collider = (Collider)areaCollider.objectReferenceValue;
                    collider.enabled = !isManual.boolValue;
                }
            }
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();
            if (!isPlaying && isManual.boolValue)
            {
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketMirror.InspectorManualModeInfo"), MessageType.Info);
            }
        }
    }
}