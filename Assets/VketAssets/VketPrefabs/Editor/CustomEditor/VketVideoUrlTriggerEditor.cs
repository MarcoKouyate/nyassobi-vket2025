
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketVideoUrlTrigger))]
    public class VketVideoUrlTriggerEditor : Editor
    {
        private VketVideoUrlTrigger videoUrlTrigger;
        private Vector2 scrollPosition;

        private void OnEnable()
        {
            if (videoUrlTrigger == null)
                videoUrlTrigger = (VketVideoUrlTrigger)target;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Video URL Trigger", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(67));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketVideoUrlTriggerEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            serializedObject.Update();

            bool isInteract = serializedObject.FindProperty("isInteract").boolValue;
            int popupIdx = isInteract ? 1 : 0;
            EditorGUI.BeginChangeCheck();
            popupIdx = EditorGUILayout.Popup("Mode", popupIdx, new string[] { "On Player Enter", "Interact" });
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.FindProperty("isInteract").boolValue = popupIdx == 1 ? true : false;
            }

            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("videoUrl"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("vketVideoPlayer"));

            serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginDisabledGroup(videoUrlTrigger.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketVideoUrlTriggerSettingWindow>(videoUrlTrigger.transform);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
