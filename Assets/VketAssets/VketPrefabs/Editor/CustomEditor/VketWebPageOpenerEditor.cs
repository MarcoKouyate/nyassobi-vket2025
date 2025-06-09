
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
    [CustomEditor(typeof(VketWebPageOpener))]
    public class VketWebPageOpenerEditor : Editor
    {
        private Vector2 scrollPosition = Vector2.zero;
        private VketWebPageOpener vketWebPageOpener;
        private Image image;
        private Sprite[] templateSprites;
        private string[] webPageOpenerType =
        {
            "  Vket Circle Page Opener 2D",
            "  Vket Circle Page Opener 3D",
            "  Vket Item Page Opener 2D",
            "  Vket Item Page Opener 3D"
        };
        
        private string GetSummary(int index)
        {
            return LocalizedUtility.GetInspectorReadme(Is2D(index) ? "VketWebPageOpenerEditor_2D" : "VketWebPageOpenerEditor_3D");
        }
        
        private int[] summaryHeights = { 108, 140, 124, 152 };
        private CancellationTokenSource currentRequest =new CancellationTokenSource();
        private string processedID;
        private (string itemName, string shopName) loadedData=(null,null);

        private readonly string[] SpriteGuids =
        {
            "1b35bdd9d00a5f94798265e3cc8d5387", // BUY
            "ff1ddfaa8351a7d408e7f09ffd79aeb1", // FREE
            "44109250f81e7f14ab6f566218996ffe"  // CATALOG
        };

        private void OnEnable()
        {
            if (vketWebPageOpener == null)
                vketWebPageOpener = (VketWebPageOpener)target;

            templateSprites = new Sprite[SpriteGuids.Length];
            for(int i=0; i < templateSprites.Length; i++)
            {
                templateSprites[i] = LoadSpriteFromGuid(SpriteGuids[i]);
            }
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var type = serializedObject.FindProperty("type").intValue;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(webPageOpenerType[type], new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(summaryHeights[type]));
            EditorGUILayout.TextArea(GetSummary(type));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            if (!IsCircle(type))
            {
                var itemPageOpener = vketWebPageOpener.GetComponentInChildren<VioRama.ItemPageOpener>(true);
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
                                }
                                if (IDResult != string.Empty)
                                {
                                    if (GUILayout.Button("Preview Item "+IDResult))
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
            }

            if (Is2D(type))
            {
                Transform canvas = vketWebPageOpener.transform.Find("Canvas");
                if (canvas != null)
                {
                    if (image == null)
                        image = canvas.Find("Image")?.GetComponent<Image>();
                    if (image != null)
                    {
                        int spriteIdx;
                        for(spriteIdx = 0; spriteIdx < templateSprites.Length; spriteIdx++)
                        {
                            if (image.sprite == templateSprites[spriteIdx])
                                break;
                        }
                        int popup = EditorGUILayout.Popup("Template Image", spriteIdx, new string[] { "BUY", "FREE", "CATALOG", "Custom" });
                        if (popup != spriteIdx)
                        {
                            if (popup < templateSprites.Length)
                            {
                                image.sprite = templateSprites[popup];
                            }
                            else
                            {
                                image.sprite = null;
                            }
                            EditorUtility.SetDirty(image);
                        }

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
                }
            }
            else
            {
                var property = serializedObject.FindProperty("autoAdjustPosition");
                EditorGUI.BeginChangeCheck();
                bool autoAdjust = EditorGUILayout.Toggle(new GUIContent("Auto-adjust", "Auto-adjust position of the pop-up dialog (Recommended)"), property.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    property.boolValue = autoAdjust;
                    serializedObject.ApplyModifiedProperties();
                }
                EditorGUILayout.Space();
                if (GUILayout.Button("Setup Collider"))
                {
                    AdjustCapsuleCollider();
                }
            }

            EditorGUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(vketWebPageOpener.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketWebPageOpenerSettingWindow>(vketWebPageOpener.transform);
            }
            EditorGUI.EndDisabledGroup();

            //base.OnInspectorGUI();
        }

        private bool Is2D(int type)
        {
            return type == 0 || type == 2;
        }

        private bool IsCircle(int type)
        {
            return type == 0 || type == 1;
        }

        private Sprite LoadSpriteFromGuid(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                return (Sprite)AssetDatabase.LoadAssetAtPath(path, typeof(Sprite));
            }

            return null;
        }

        private void AdjustCapsuleCollider()
        {
            CapsuleCollider capsuleCollider = vketWebPageOpener.GetComponent<CapsuleCollider>();
            if (capsuleCollider == null)
            {
                Debug.LogWarning("CapsuleCollider not found.");
                return;
            }

            var renderers = vketWebPageOpener.transform.Find("Visual").GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.Log("Renderer not found.");
                return;
            }

            Bounds totalBounds = CalculateBounds(renderers);

            Vector3 pos = vketWebPageOpener.transform.position;
            Vector3 scale = vketWebPageOpener.transform.lossyScale;

            Vector3 localCenter = new Vector3(
                (totalBounds.center.x - pos.x) / scale.x,
                (totalBounds.center.y - pos.y) / scale.y,
                (totalBounds.center.z - pos.z) / scale.z
            );
            Vector3 localSize = new Vector3(
                totalBounds.size.x / scale.x,
                totalBounds.size.y / scale.y,
                totalBounds.size.z / scale.z
            );

            Undo.RecordObject(capsuleCollider, "AdjustCollider");
            capsuleCollider.center = localCenter;
            capsuleCollider.height = localSize.y;
            capsuleCollider.radius = localSize.x <= localSize.y ? localSize.x * 0.5f : localSize.y * 0.5f;

            Debug.Log("Setup Completed.");
        }

        private Bounds CalculateBounds(Renderer[] renderers)
        {
            Bounds bounds = new Bounds();

            foreach (var renderer in renderers)
            {
                Vector3 min = renderer.bounds.center - (renderer.bounds.size * 0.5f);
                Vector3 max = renderer.bounds.center + (renderer.bounds.size * 0.5f);

                if (bounds.size == Vector3.zero)
                    bounds = new Bounds(renderer.bounds.center, Vector3.zero);

                bounds.Encapsulate(min);
                bounds.Encapsulate(max);
            }

            return bounds;
        }
    }
}
