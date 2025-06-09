using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
	[CustomEditor(typeof(VketBGMFader))]
	public class VketBGMFaderEditor : Editor
	{
		private VketBGMFader _vketBGMFader;
		private Vector2 scrollPosition = Vector2.zero;

		private void OnEnable()
		{
			if (!_vketBGMFader) _vketBGMFader = target as VketBGMFader;
		}

		public override void OnInspectorGUI()
		{
			if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

			// Draw Title and Summary
			var style = new GUIStyle();
			EditorGUILayout.BeginVertical(GUI.skin.box);
			EditorGUILayout.LabelField("  Vket BGM Fader", new GUIStyle(EditorStyles.boldLabel));
			EditorGUILayout.EndVertical();
			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(95));
			EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketBGMFaderEditor"));
			EditorGUILayout.EndScrollView();

			EditorGUILayout.Space();

			serializedObject.Update();

			// Draw Field of On Booth Fading
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_fadeInTime"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_fadeOutTime"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_fadeInVolumeRatio"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_fadeOutVolumeRatio"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("_onBoothFading"));

			serializedObject.ApplyModifiedProperties();

			EditorGUI.BeginDisabledGroup(_vketBGMFader.gameObject.scene.name == null);
			if (GUILayout.Button("Open Setting Window"))
			{
				VketPrefabSettingWindow.OpenSettingWindow<VketBGMFaderSettingWindow>(_vketBGMFader.transform);
			}
			EditorGUI.EndDisabledGroup();
		}
	}
}