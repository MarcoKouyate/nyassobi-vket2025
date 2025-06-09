
using System;
using System.Text.RegularExpressions;
using System.Threading;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketStorePreviewOpener))]
    public class VketStorePreviewOpenerEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private VketStorePreviewOpener vketStorePreviewOpener;
        private Image image;
        private int summaryHeight = 124;
        private CancellationTokenSource currentRequest =new CancellationTokenSource();
        private string processedID;
        private (string itemName, string shopName) loadedData=(null,null);

        private void OnEnable()
        {
            if (vketStorePreviewOpener == null)
                vketStorePreviewOpener = (VketStorePreviewOpener)target;
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("  Vket Store Preview", new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(summaryHeight));
            EditorGUILayout.TextArea(LocalizedUtility.GetInspectorReadme("VketStorePreviewOpenerEditor"));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            var itemPageOpener = vketStorePreviewOpener.GetComponentInChildren<VioRama.ItemPageOpener>(true);
            if (itemPageOpener != null)
            {
                var vrcurlprop = serializedObject.FindProperty("editor_ItemURL").FindPropertyRelative("url");
                EditorGUILayout.LabelField("URLSample:https://store.vket.com/ja/items/1234");
                vrcurlprop.stringValue = EditorGUILayout.TextField("Item URL", vrcurlprop.stringValue);
                {
                    serializedObject.ApplyModifiedProperties();
                    if (Uri.TryCreate(vrcurlprop.stringValue, UriKind.Absolute, out Uri uri))
                    {
                        // 检查URI是否是HTTPS以及Host来源
                        if (uri.Scheme == Uri.UriSchemeHttps && uri.Host.Equals("store.vket.com", StringComparison.OrdinalIgnoreCase))
                        {
                            string IDResult = string.Empty;
                            // 正则表达式用于匹配items后面跟随的数字（即ID）
                            string pattern = @"items/(\d+)";

                            // 使用正则表达式匹配URI
                            Match match = Regex.Match(vrcurlprop.stringValue.Trim(), pattern);

                            // 检查是否找到匹配项
                            if (match.Success)
                            {
                                // 返回匹配到的第一个组（即ID）
                                IDResult = match.Groups[1].Value;

                                // pageId設定
                                serializedObject.FindProperty("pageId").intValue = int.Parse(IDResult);
                                serializedObject.ApplyModifiedProperties();
                            }
                            if (IDResult != string.Empty)
                            {
                                if (GUILayout.Button("Preview Item " + IDResult))
                                {
                                    Application.OpenURL(vrcurlprop.stringValue);
                                }
                            }
                            else
                            {
                                EditorGUILayout.HelpBox($"URL ItemID Not Found!", MessageType.Error);
                            }
                        }
                        else
                        {
                            EditorGUILayout.HelpBox("Url Is Invalid!", MessageType.Error);
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Url Is Invalid!", MessageType.Error);
                    }
                }
            }

            Transform canvas = vketStorePreviewOpener.transform.Find("Canvas");
            if (canvas != null)
            {
                if (image == null)
                    image = canvas.Find("Image")?.GetComponent<Image>();
                if (image != null)
                {
                    EditorGUI.BeginChangeCheck();
                    var sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", image.sprite, typeof(Sprite), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(image, "Modify image sprite");
                        image.sprite = sprite;
                        image.enabled = false;
                        image.enabled = true;
                    }

                    EditorGUILayout.Space();
                }
                else
                {
                    Debug.Log("Not Found \"VketWebPageOpener/Canvas/Image\"");
                }
            }

            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(vketStorePreviewOpener.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketStorePreviewOpenerSettingWindow>(vketStorePreviewOpener.transform);
            }
            EditorGUI.EndDisabledGroup();

            //base.OnInspectorGUI();
        }
    }
}
