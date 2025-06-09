
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketSoundFade))]
    public class VketSoundFadeEditor : Editor
    {
        private VketSoundFade _vketSoundFade;
        private Vector2 scrollPosition = Vector2.zero;

        private void OnEnable()
        {
            if (!_vketSoundFade)
                _vketSoundFade = target as VketSoundFade;
        }
        
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Sound Fade", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(95));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketSoundFadeEditor"));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            serializedObject.Update();

            // Draw Field of On Booth Fading
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fadeInTime"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("onBoothFading"));

            serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginDisabledGroup(_vketSoundFade.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketSoundFadeSettingWindow>(_vketSoundFade.transform);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
