using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using Vket.VketPrefabs;
using UnityEngine.UI;
using TMPro;
using VketAssets.VketPrefabs.Editor;

namespace VketAssets.VketPrefabs.VketAttachItem.Editor
{
    [CustomEditor(typeof(Udon.VketAttachItem))]
    public class VketAttachItemEditor : UnityEditor.Editor
    {
        private Vector2 _scrollPosition = Vector2.zero;
        private Udon.VketAttachItem _vketAttachItem;
        private Image _attachImage;
        private Image _settingImage;
        private TextMeshProUGUI[] _attachTexts;
        private TextMeshProUGUI[] _settingTexts;

        private void OnEnable()
        {
            _vketAttachItem = target as Udon.VketAttachItem;
            _attachImage = _vketAttachItem.transform.Find("UI/ActiveUI/Canvas/Image").GetComponent<Image>();
            _settingImage = _vketAttachItem.transform.Find("UI/SettingActiveUI/Canvas/Image").GetComponent<Image>();
            _attachTexts = _attachImage.GetComponentsInChildren<TextMeshProUGUI>(true);
            _settingTexts = _settingImage.GetComponentsInChildren<TextMeshProUGUI>(true);
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target,false,false)) return;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Attach Item", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(152));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketAttachItemEditor"));
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultAttachHumanBodyBone"));
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Extend Setting");
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bonePositionOffset"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("boneRotationOffset"));

            if (_attachImage != null && _settingImage != null) {
                EditorGUI.BeginChangeCheck();
                var imageColor = EditorGUILayout.ColorField("Image Color", _attachImage.color);
                if(EditorGUI.EndChangeCheck())
                {
                    _attachImage.color = imageColor;
                    _settingImage.color = imageColor;
                    EditorUtility.SetDirty(_attachImage);
                    EditorUtility.SetDirty(_settingImage);
                }
            }
            if(_attachTexts != null && _settingTexts != null)
            {
                EditorGUI.BeginChangeCheck();
                var textColor = EditorGUILayout.ColorField("Text Color", _attachTexts[0].color);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach(var attachText in _attachTexts)
                    {
                        attachText.color = textColor;
                        EditorUtility.SetDirty(attachText);
                    }
                    foreach(var settingText in _settingTexts)
                    {
                        settingText.color = textColor;
                        EditorUtility.SetDirty(settingText);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();

            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow
                    .OpenSettingWindow<VketAttachItemSettingWindow>((target as Udon.VketAttachItem).transform);
            }
        }
    }
}