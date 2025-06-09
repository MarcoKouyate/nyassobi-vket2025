
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Vket.VketPrefabs;
using VketAssets.VketPrefabs.Editor;

namespace VketAssets.VketPrefabs.VketAttachItem.Editor
{
    public class VketAttachItemSettingWindow : VketPrefabSettingWindow
    {
        readonly string prefabName = "VketAttachItem";
        Udon.VketAttachItem _vketAttachItem;

        private SerializedObject _vketAttachItemSO;
        private Image _attachImage;
        private Image _settingImage;
        private TextMeshProUGUI[] _attachTexts;
        private TextMeshProUGUI[] _settingTexts;
        
        protected override void InitWindow()
        {
            if (_vketPrefabInstance)
            {
                _vketAttachItem = _vketPrefabInstance.GetComponent<Udon.VketAttachItem>();
                _vketAttachItemSO = new SerializedObject(_vketAttachItem);
                
                _attachImage = _vketPrefabInstance.Find("UI/ActiveUI/Canvas/Image").GetComponent<Image>();
                _settingImage = _vketPrefabInstance.Find("UI/SettingActiveUI/Canvas/Image").GetComponent<Image>();
                _attachTexts = _attachImage.GetComponentsInChildren<TextMeshProUGUI>(true);
                _settingTexts = _settingImage.GetComponentsInChildren<TextMeshProUGUI>(true);
            }
        }

        private void OnGUI()
        {
            InitStyle();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            {
                _vketAttachItemSO.Update();
                
                /* "1.追従ボーン指定" */
                EditorGUILayout.LabelField(/* "1.追従ボーンの指定" */ LocalizedUtility.Get("VketAttachItemSettingWindow.BoneSetting"), _l3Style);
                EditorGUILayout.PropertyField(_vketAttachItemSO.FindProperty("defaultAttachHumanBodyBone"));
                GUILayout.Space(3);
                
                /* "2.表示オブジェクトの設定*/
                EditorGUILayout.LabelField(/* "2.表示オブジェクトの設定" */ LocalizedUtility.Get("VketAttachItemSettingWindow.ViewObject"), _l3Style);
                if (GUILayout.Button(/* "Visualオブジェクトを選択" */ LocalizedUtility.Get("VketAttachItemSettingWindow.ViewObject.SelectVisualObject")))
                {
                    Selection.activeObject = _vketAttachItem.visual.gameObject;
                }
                EditorGUILayout.HelpBox(/* "追従するオブジェクトをVisualオブジェクト以下に配置してください。" */ LocalizedUtility.Get("VketAttachItemSettingWindow.ViewObject.Help"), MessageType.Info);
                GUILayout.Space(3);
                
                /* "3.表示オブジェクトの初期位置設定*/
                EditorGUILayout.LabelField(/* "3.表示オブジェクトの初期位置設定" */ LocalizedUtility.Get("VketAttachItemSettingWindow.ViewObjectTransform"), _l3Style);
                EditorGUILayout.PropertyField(_vketAttachItemSO.FindProperty("bonePositionOffset"));
                EditorGUILayout.PropertyField(_vketAttachItemSO.FindProperty("boneRotationOffset"));

                /* "4.UI設定*/
                EditorGUILayout.LabelField(/* "4.UI設定" */ LocalizedUtility.Get("VketAttachItemSettingWindow.UISetting"), _l3Style);
                if (GUILayout.Button(/* "UIDisplayPoint Transformを選択" */ LocalizedUtility.Get("VketAttachItemSettingWindow.UISetting.SelectUITransform")))
                {
                    Selection.activeObject = _vketAttachItem.uiPoint.gameObject;
                }
                EditorGUILayout.HelpBox(/* "UIDisplayPointオブジェクトのTransformを調整してボタンやスライダーパネルの配置を変更できます。" */ LocalizedUtility.Get("VketAttachItemSettingWindow.UISetting.Help"), MessageType.Info);
                GUILayout.Space(3);
                
                /* "5.ボタンの色を設定" */
                EditorGUILayout.LabelField(/* "5.ボタンの色を設定" */ LocalizedUtility.Get("VketAttachItemSettingWindow.ButtonColorSetting"), _l3Style);
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
                _vketAttachItemSO.ApplyModifiedProperties();
            }
            EditorGUILayout.EndScrollView();

            BaseFooter(prefabName, _vketAttachItem.gameObject);
        }
    }
}