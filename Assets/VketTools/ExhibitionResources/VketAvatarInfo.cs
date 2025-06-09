using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace VketTools.ExhibitionResources
{
    public class VketAvatarInfo : ScriptableObject
    {
        [Serializable]
        public class AvatarData
        {
            public string ShopName;
            public string AvatarName;
            public string BlueprintId;
            public string Price;
            public string Url;

#if UNITY_EDITOR
            private static readonly string[] AllowOpenUrls = {
                "https://booth.pm/",
                "https://gumroad.com/",
                "https://jinxxy.com/",
                "https://payhip.com/",
            };
            private static readonly string[] AllowOpenUriRegexs =
            {
                "https://.*.booth.pm/items/[0-9]*",
                "https://.*.gumroad.com/*",
            };
                
            public static bool ValidateUrl(string url)
            {
                if(string.IsNullOrEmpty(url))
                    return false;
                return AllowOpenUrls.Any(url.StartsWith) || AllowOpenUriRegexs.Any(r => Regex.IsMatch(url, r));
            }
            
            [CustomPropertyDrawer(typeof(AvatarData))]
            public class AvatarDataDrawer : PropertyDrawer
            {
                // Draw the property inside the given rect
                public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
                {
                    EditorGUI.BeginProperty(position, label, property);

                    position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
                    var indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;

                    var posY = position.y + EditorGUIUtility.standardVerticalSpacing;
                    var shopNameRect = new Rect(position.x, posY, position.width, EditorGUIUtility.singleLineHeight);
                    posY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    var avatarNameRect = new Rect(position.x, posY, position.width, EditorGUIUtility.singleLineHeight);
                    posY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    var idRect = new Rect(position.x, posY, position.width, EditorGUIUtility.singleLineHeight);
                    posY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    var priceRect = new Rect(position.x, posY, position.width, EditorGUIUtility.singleLineHeight);
                    posY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    var urlRect = new Rect(position.x, posY, position.width, EditorGUIUtility.singleLineHeight);
                    EditorGUIUtility.labelWidth = 80;
                    var urlProp = property.FindPropertyRelative(nameof(Url));
#if VKET_TOOLS
                    EditorGUI.PropertyField(shopNameRect, property.FindPropertyRelative(nameof(ShopName)), new GUIContent( /* "ショップ名" */  Utilities.AssetUtility.GetMain("VketAvatarInfo.ShopName")));
                    EditorGUI.PropertyField(avatarNameRect, property.FindPropertyRelative(nameof(AvatarName)), new GUIContent( /* "アバター名" */  Utilities.AssetUtility.GetMain("VketAvatarInfo.AvatarName")));
                    EditorGUI.PropertyField(idRect, property.FindPropertyRelative(nameof(BlueprintId)));
                    EditorGUI.PropertyField(priceRect, property.FindPropertyRelative(nameof(Price)), new GUIContent( /* "値段" */  Utilities.AssetUtility.GetMain("VketAvatarInfo.Price")));
                    EditorGUI.PropertyField(urlRect, urlProp, new GUIContent( /* "Url" */  Utilities.AssetUtility.GetMain("VketAvatarInfo.Url")));
#else
                    EditorGUI.PropertyField(shopNameRect, property.FindPropertyRelative(nameof(AvatarName)));
                    EditorGUI.PropertyField(avatarNameRect, property.FindPropertyRelative(nameof(AvatarName)));
                    EditorGUI.PropertyField(idRect, property.FindPropertyRelative(nameof(BlueprintId)));
                    EditorGUI.PropertyField(priceRect, property.FindPropertyRelative(nameof(Price)));
                    EditorGUI.PropertyField(urlRect, urlProp);
#endif
                    EditorGUI.indentLevel = indent;

                    var url = urlProp.stringValue;
                    if (!string.IsNullOrEmpty(url))
                    {
                        posY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        var buttonRect = new Rect(position.x, posY, position.width, EditorGUIUtility.singleLineHeight);
                        if(GUI.Button(buttonRect, "OpenURL"))
                        {
                            Application.OpenURL(url);
                        }

                        if (!ValidateUrl(url))
                        {
                            posY += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                            var helpBoxRect = new Rect(position.x, posY, position.width, GetHelpBoxHeight(HelpBoxLabel, MessageType.Error, margineWidth: 80f));
                            EditorGUI.HelpBox(helpBoxRect, HelpBoxLabel, MessageType.Error);
                        }
                    }
                    
                    EditorGUI.EndProperty();
                }
                
                private string HelpBoxLabel =>
#if VKET_TOOLS
                    $"{Utilities.AssetUtility.GetMain("VketAvatarInfo.VaridateUrl.HelpBox")}\nhttps://booth.pm/\nhttps://gumroad.com/\nhttps://jinxxy.com/\nhttps://payhip.com/";
#else
                    "販売WebページのURLとして設定できるドメインは下記のみです。\nhttps://booth.pm/\nhttps://gumroad.com/\nhttps://jinxxy.com/\nhttps://payhip.com/";
#endif

                public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
                {
                    var height = EditorGUIUtility.singleLineHeight * 5 + EditorGUIUtility.standardVerticalSpacing * 5;
                    var urlProp = property.FindPropertyRelative(nameof(Url));
                    var url = urlProp.stringValue;
                    if (!string.IsNullOrEmpty(url))
                    {
                        height += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                        if (!ValidateUrl(url))
                        {
                            height += GetHelpBoxHeight(HelpBoxLabel, MessageType.Error, margineWidth: 80f) + EditorGUIUtility.standardVerticalSpacing;
                        }
                    }
                    return height;
                }
            }
            
            static float GetHelpBoxHeight(string message, MessageType type = MessageType.Info, float margineWidth = 0)
            {
                var style   = new GUIStyle( "HelpBox" );
                var content = new GUIContent( message );
                return Mathf.Max( style.CalcHeight( content, Screen.width - (type != MessageType.None ? 53 : 21) - margineWidth), 40);
            }
#endif // UNITY_EDITOR
        }

        public bool UsePedestal;
        public List<AvatarData> AvatarDataList;
#if UNITY_EDITOR
        [CustomEditor(typeof(VketAvatarInfo))]
        public class VketAvatarInfoInspector : Editor
        {
            private const int MAX_LIST_SIZE = 3;
            private ReorderableList _reorderableList;

            private void OnEnable()
            {
                var listProp = serializedObject.FindProperty(nameof(AvatarDataList));
                _reorderableList ??= new ReorderableList(serializedObject, listProp);
                _reorderableList.draggable = false;
                _reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, nameof(AvatarDataList));
                _reorderableList.onCanAddCallback = list => list.count < MAX_LIST_SIZE;
                _reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    var elementProperty = listProp.GetArrayElementAtIndex(index);
#if VKET_TOOLS
                    EditorGUI.PropertyField(rect, elementProperty, new GUIContent(/* "アバター {0}" */  Utilities.AssetUtility.GetMain("VketAvatarInfo.Header", index)));
#else
                    EditorGUI.PropertyField(rect, elementProperty);
#endif
                };
                _reorderableList.elementHeightCallback = index => EditorGUI.GetPropertyHeight(listProp.GetArrayElementAtIndex(index));
            }

            public override void OnInspectorGUI()
            {
                serializedObject.Update();
                EditorGUIUtility.labelWidth = 60;
                _reorderableList.DoLayoutList();
                serializedObject.ApplyModifiedProperties();
            }
        }
#endif
    }
}