
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketFollowPickup))]
    public class VketFollowPickupEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private bool isFoldoutOpen;
        private VketFollowPickup _vketFollowPickup;

        private void OnEnable()
        {
            _vketFollowPickup = target as VketFollowPickup;
        }
        
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(108));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketFollowPickupEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            serializedObject.Update();

            // Draw Field of Attach Range
            EditorGUILayout.PropertyField(serializedObject.FindProperty("attachRange"));

            // Draw Field of Target Bone
            SerializedProperty targetBoneProperty = serializedObject.FindProperty("targetBone");
            EditorGUI.BeginChangeCheck();
            int bone = EditorGUILayout.Popup("Target Bone", targetBoneProperty.intValue,
                new string[] { "Head", "Neck", "Chest", "Spine", "Hips", "Shoulder", "UpperArm", "LowerArm", "Hand", "UpperLeg", "LowerLeg", "Foot" });
            if (EditorGUI.EndChangeCheck())
            {
                targetBoneProperty.intValue = bone;
            }
            /*
            EditorGUILayout.Space();
            
            // References
            isFoldoutOpen = EditorGUILayout.Foldout(isFoldoutOpen, "References(Do not change)");
            if (isFoldoutOpen)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("pickup"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("udonManager"));
                EditorGUI.indentLevel--;
            }
            */
            serializedObject.ApplyModifiedProperties();

            EditorGUI.BeginDisabledGroup(_vketFollowPickup.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketFollowPickupSettingWindow>(_vketFollowPickup.transform);
            }
            EditorGUI.EndDisabledGroup();
            //base.OnInspectorGUI();
        }
    }
}
