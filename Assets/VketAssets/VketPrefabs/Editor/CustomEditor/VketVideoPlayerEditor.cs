
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketVideoPlayer))]
    public class VketVideoPlayerEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private VketVideoPlayer vketVideoPlayer;
        private BaseVRCVideoPlayer videoPlayer;

        private void OnEnable()
        {
            if (vketVideoPlayer == null)
                vketVideoPlayer = (VketVideoPlayer)target;

            videoPlayer = (BaseVRCVideoPlayer)serializedObject.FindProperty("videoPlayer").objectReferenceValue;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Video Player", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(152));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketVideoPlayerEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            serializedObject.Update();

            // Draw Field of Video Url
            var urlProp = serializedObject.FindProperty("videoUrl");
            EditorGUILayout.PropertyField(urlProp);
            if (urlProp.boxedValue is VRCUrl url && !string.IsNullOrEmpty(url.Get()))
            {
                if (GUILayout.Button("Open Video Url"))
                {
                    Application.OpenURL(url.Get());
                }
            }

            // Draw Field of Loop
            if (videoPlayer != null)
            {
                var so = new SerializedObject(videoPlayer);
                so.Update();
                var loopProp = so.FindProperty("loop");
                EditorGUI.BeginChangeCheck();
                bool loop = EditorGUILayout.Toggle("Loop", loopProp.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    loopProp.boolValue = loop;
                    so.ApplyModifiedProperties();
                }
            }

            EditorGUILayout.Space();

            // Draw Field of World Bgm Fade
            EditorGUILayout.PropertyField(serializedObject.FindProperty("worldBgmFade"));

            // Draw Field of On Booth Play
            var onBoothPlay = EditorGUILayout.PropertyField(serializedObject.FindProperty("onBoothPlay"));

            // Draw Fiedl of Disabled Image
            var disabledImageProperty = serializedObject.FindProperty("disabledImage");
            EditorGUI.BeginChangeCheck();
            disabledImageProperty.objectReferenceValue = (Texture2D)EditorGUILayout.ObjectField("Disabled Image", disabledImageProperty.objectReferenceValue, typeof(Texture2D), false);

            // Draw Fiedl of Loading Image
            var loadingImageProperty = serializedObject.FindProperty("loadingImage");
            EditorGUI.BeginChangeCheck();
            loadingImageProperty.objectReferenceValue = (Texture2D)EditorGUILayout.ObjectField("Loading Image", loadingImageProperty.objectReferenceValue, typeof(Texture2D), false);

            serializedObject.ApplyModifiedProperties();
            
            EditorGUI.BeginDisabledGroup(vketVideoPlayer.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketVideoPlayerSettingWindow>(vketVideoPlayer.transform);
            }
            EditorGUI.EndDisabledGroup();
        }
    }
}
