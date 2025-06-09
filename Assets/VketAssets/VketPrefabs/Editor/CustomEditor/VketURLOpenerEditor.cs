
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UdonSharpEditor;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    [CustomEditor(typeof(VketURLOpener))]
    public class VketURLOpenerEditor : Editor
    {
        private VketURLOpener _vketURLOpener;
        private VketURLOpenerProxy _vrcUrlLauncherProxy;
        private Vector2 _scrollPosition = Vector2.zero;
        private Image _image;

        private readonly string[] _urlOpenerType =
        {
            "  Vket Url Opener 2D",
            "  Vket Url Opener 3D"
        };
        
        private string GetSummary(int index)
        {
            return LocalizedUtility.GetInspectorReadme(Is2D(index) ? "VketURLOpenerEditor_2D" : "VketURLOpenerEditor_3D");
        }
        private readonly int[] _summaryHeights = { 136, 168 };

        private void OnEnable()
        {
            _vketURLOpener = (VketURLOpener)target;
            _vrcUrlLauncherProxy = _vketURLOpener.GetComponent<VketURLOpenerProxy>();
        }

        public override void OnInspectorGUI()
        {
            if (UdonSharpGUI.DrawDefaultUdonSharpBehaviourHeader(target)) return;

            serializedObject.Update();

            var type = serializedObject.FindProperty("_type").intValue;

            // Draw Title and Summary
            var style = new GUIStyle();
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField(_urlOpenerType[type], new GUIStyle(EditorStyles.boldLabel));
            EditorGUILayout.EndVertical();
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(_summaryHeights[type]));
            EditorGUILayout.TextArea(GetSummary(type));
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            SerializedObject proxySO = new SerializedObject(_vrcUrlLauncherProxy);
            proxySO.Update();
            EditorGUILayout.PropertyField(proxySO.FindProperty("_url").FindPropertyRelative("url"));
            proxySO.ApplyModifiedProperties();
            serializedObject.FindProperty("_inputURL").stringValue= _vrcUrlLauncherProxy.Url.ToString();
            serializedObject.ApplyModifiedProperties();
            if (!_vketURLOpener.CheckURL(_vrcUrlLauncherProxy.Url.ToString()))
            {
                string allowedURLs = string.Join("\n", _vketURLOpener.AllowUri);
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketURLOpenerSettingWindow.UrlIsNotAllowList") + "\n" + allowedURLs, MessageType.Error);
            }
            if (Is2D(type))
            {
                Transform canvas = _vketURLOpener.transform.Find("Canvas");
                if (canvas != null)
                {
                    if (_image == null)
                        _image = canvas.Find("Image")?.GetComponent<Image>();
                    if (_image != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        var sprite = (Sprite)EditorGUILayout.ObjectField("Sprite", _image.sprite, typeof(Sprite), false);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Undo.RecordObject(_image, "Modify image sprite");
                            _image.sprite = sprite;
                            _image.enabled = false;
                            _image.enabled = true;
                        }

                        EditorGUILayout.Space();
                    }
                    else
                    {
                        Debug.Log("Not Found \"VketURLOpener/Canvas/Image\"");
                    }
                }
            }
            else
            {
                var property = serializedObject.FindProperty("_autoAdjustPosition");
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

            EditorGUI.BeginDisabledGroup(_vketURLOpener.gameObject.scene.name == null);
            if (GUILayout.Button("Open Setting Window"))
            {
                VketPrefabSettingWindow.OpenSettingWindow<VketURLOpenerSettingWindow>(_vketURLOpener.transform);
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

        private void AdjustCapsuleCollider()
        {
            CapsuleCollider capsuleCollider = (CapsuleCollider)serializedObject.FindProperty("_capsuleCollider").objectReferenceValue;
            if (capsuleCollider == null)
            {
                Debug.LogWarning("CapsuleCollider not found.");
                return;
            }

            var renderers = _vketURLOpener.transform.Find("Visual").GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.Log("Renderer not found.");
                return;
            }

            Bounds totalBounds = CalculateBounds(renderers);

            Vector3 pos = _vketURLOpener.transform.position;
            Vector3 scale = _vketURLOpener.transform.lossyScale;

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
