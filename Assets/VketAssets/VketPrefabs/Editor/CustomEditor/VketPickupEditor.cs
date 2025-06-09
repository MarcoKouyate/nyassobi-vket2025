
using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketPickup))]
    public class VketPickupEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private VketPickup _vketPickup;

        private void OnEnable()
        {
            _vketPickup = target as VketPickup;
        }
        
        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Pickup", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(108));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketPickupEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            serializedObject.Update();

            var overrideControllerProperty = serializedObject.FindProperty("overrideController");
            var overrideControllerBaseProperty = serializedObject.FindProperty("overrideControllerBase");
            var animator = (Animator)serializedObject.FindProperty("animator").objectReferenceValue;

            // Draw Field of Override Controller
            EditorGUI.BeginChangeCheck();
            var overrideController = (AnimatorOverrideController)EditorGUILayout.ObjectField("Override Controller", overrideControllerProperty.objectReferenceValue, typeof(AnimatorOverrideController), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (overrideController == serializedObject.FindProperty("overrideControllerBase").objectReferenceValue)
                    return;

                overrideControllerProperty.objectReferenceValue = overrideController;
                Undo.RecordObject(animator, "Modify animator");
                animator.runtimeAnimatorController = overrideController;

                serializedObject.ApplyModifiedProperties();
                return;
            }

            if (overrideController == null)
            {
                // Create New Override Controller
                if (GUILayout.Button("Create new override controller"))
                {
                    var newController = CreateOverrideController((AnimatorOverrideController)overrideControllerBaseProperty.objectReferenceValue);
                    if (newController != null)
                    {
                        overrideControllerProperty.objectReferenceValue = newController;
                        Undo.RecordObject(animator, "Modify animator");
                        animator.runtimeAnimatorController = newController;

                        serializedObject.ApplyModifiedProperties();
                        return;
                    }
                }
            }
            else
            {
                // If Attach Override Controller Base Then Detach
                if (overrideControllerProperty.objectReferenceValue == overrideControllerBaseProperty.objectReferenceValue)
                {
                    overrideControllerProperty.objectReferenceValue = null;
                    animator.runtimeAnimatorController = null;
                    EditorUtility.SetDirty(animator);

                    serializedObject.ApplyModifiedProperties();
                    return;
                }

                // Draw Field of Override Animation Clips
                List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                overrideController.GetOverrides(overrides);

                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                var clips = new AnimationClip[4];
                clips[0] = (AnimationClip)EditorGUILayout.ObjectField(overrides[0].Key.name, overrides[0].Value, typeof(AnimationClip), false);
                clips[1] = (AnimationClip)EditorGUILayout.ObjectField(overrides[1].Key.name, overrides[1].Value, typeof(AnimationClip), false);
                clips[2] = (AnimationClip)EditorGUILayout.ObjectField(overrides[2].Key.name, overrides[2].Value, typeof(AnimationClip), false);
                clips[3] = (AnimationClip)EditorGUILayout.ObjectField(overrides[3].Key.name, overrides[3].Value, typeof(AnimationClip), false);
                if (EditorGUI.EndChangeCheck())
                {
                    for (int i = 0; i < 4; i++)
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clips[i]);

                    Undo.RecordObject(overrideController, "Modify override clip");
                    overrideController.ApplyOverrides(overrides);
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space(10);
            
            EditorGUI.BeginDisabledGroup(_vketPickup.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketPickupSettingWindow>(_vketPickup.transform);
            }
            EditorGUI.EndDisabledGroup();
            /*
            EditorGUILayout.Space();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("OverrideControllerEmpty", serializedObject.FindProperty("overrideControllerBase").objectReferenceValue, typeof(AnimatorOverrideController), false);
            EditorGUI.EndDisabledGroup();
            */
            //base.OnInspectorGUI();
        }

        private AnimatorOverrideController CreateOverrideController(AnimatorOverrideController overrideControllerBase)
        {
            var path = EditorUtility.SaveFilePanelInProject("Save new override controller", "NewOverrideController.overrideController", "overrideController", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (overrideControllerBase == null)
                {
                    Debug.Log("Error! Not found override controller empty");
                    return null;
                }

                var newController = new AnimatorOverrideController(overrideControllerBase);
                AssetDatabase.CreateAsset(newController, path);
                AssetDatabase.Refresh();
                Undo.RegisterCreatedObjectUndo(newController, "Create override controller");

                return newController;
            }

            return null;
        }
    }
}
