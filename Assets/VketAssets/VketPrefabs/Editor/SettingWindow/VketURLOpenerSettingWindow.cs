using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    public class VketURLOpenerSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        private VketURLOpener _urlOpener;
        private CapsuleCollider _collider;
        private VketURLOpenerProxy _vrcUrlLauncherProxy;
        private Transform _popupTransform;
        private Text _reqText, _openOKText;
        private int _type;
        
        // 2Dの設定画像
        private Image _image;
        
        private Transform _interactObjectsRoot;
        private GameObject _interactObject;
        
        private Sprite[] _templateSprites;
        
        #endregion
        
        #region const定義
        private const string InteractObjectsRootName = "Visual";
        private static readonly string[] PrefabNames = {
            "VketURLOpener_2D", "VketURLOpener_3D"
        };
        
        private static readonly string[] SpriteGuids =
        {
            "1b35bdd9d00a5f94798265e3cc8d5387", // BUY
            "ff1ddfaa8351a7d408e7f09ffd79aeb1", // FREE
            "44109250f81e7f14ab6f566218996ffe"  // CATALOG
        };
        #endregion
        
        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 500f);

            if (_vketPrefabInstance)
            {
                _urlOpener = _vketPrefabInstance.GetComponent<VketURLOpener>();
                _collider = _vketPrefabInstance.GetComponent<CapsuleCollider>();
                _popupTransform = _vketPrefabInstance.Find("PopupTransform");
                _reqText = _vketPrefabInstance.GetComponentInChildren<Text>();
                _openOKText = _vketPrefabInstance.GetComponentInChildren<Text>();
                _vrcUrlLauncherProxy = _vketPrefabInstance.GetComponent<VketURLOpenerProxy>();
                _interactObjectsRoot = _vketPrefabInstance.Find(InteractObjectsRootName);
            }

            if (_urlOpener)
            {
                _type = (int)_urlOpener.GetProgramVariable("_type");
            }
            if (_interactObjectsRoot && _interactObjectsRoot.childCount != 0)
            {
                _interactObject = _interactObjectsRoot.GetChild(0).gameObject;
            }
            // VketURLOpene_2D, VketURLOpene_2D
            if (_type == 0 || _type == 2)
            {
                var imageTransform = _vketPrefabInstance?.transform.Find("Canvas")?.Find("Image");
                
                if(imageTransform)
                    _image = imageTransform.GetComponent<Image>();
            }
            
            _templateSprites = new Sprite[SpriteGuids.Length];
            for(int i=0; i < _templateSprites.Length; i++)
            {
                _templateSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(SpriteGuids[i]));
            }
            
        }

        private void OnGUI()
        {
            InitStyle();
            if(!BaseHeader(PrefabNames[_type]))
                return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            
            GUILayout.Space(3);

            // VketURLPageOpener_2D, VketURLOpener_2D
            if (_type == 0 || _type == 2)
            {
                /* "1.サイズの調整" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketWebPageOpenerSettingWindow.SizeSetting"), _settingItemStyle);
                EditorGUI.BeginChangeCheck();
                var scale = EditorGUILayout.Vector2Field("Scale", _vketPrefabInstance.localScale);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_vketPrefabInstance, "Change Scale");
                    _vketPrefabInstance.localScale = new Vector3(scale.x, scale.y, 1f);
                }

                GUILayout.Space(3);
                
                /* "2.画像設定" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketWebPageOpenerSettingWindow.TextureSetting"), _settingItemStyle);
                
                if (_image == null)
                {
                    EditorGUILayout.HelpBox("Not Found \"Canvas/Image\"", MessageType.Error);
                }
                
                if (_image != null)
                {
                    int spriteIdx;
                    for(spriteIdx = 0; spriteIdx < _templateSprites.Length; spriteIdx++)
                    {
                        if (_image.sprite == _templateSprites[spriteIdx])
                            break;
                    }
                    
                    int popup = EditorGUILayout.Popup("Template Image", spriteIdx, new[] { "BUY", "FREE", "CATALOG", "Custom" });
                    if (popup != spriteIdx)
                    {
                        _image.sprite = popup < _templateSprites.Length ? _templateSprites[popup] : null;
                        EditorUtility.SetDirty(_image);
                    }

                    EditorGUI.BeginChangeCheck();
                    var sprite = SpriteField(_image.sprite);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_image, "Modify image sprite");
                        _image.sprite = sprite;
                        _image.enabled = false;
                        _image.enabled = true;
                    }

                    EditorGUILayout.Space();
                }
                
                GUILayout.Space(3);
            }
            // 3D
            else
            {
                /* "1.見た目にする3Dモデルの設定" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketWebPageOpenerSettingWindow.VisualSetting"), _settingItemStyle);
                EditorGUI.BeginChangeCheck();
                var interactObject = EditorGUILayout.ObjectField(_interactObject, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                {
                    if (_interactObject)
                        _interactObject.transform.parent = null;

                    _interactObject = interactObject;
                    
                    if (_interactObject && _vketPrefabInstance)
                    {
                        // シーン上に存在しない場合は複製
                        if (_interactObject.scene.name == null)
                        {
                            var copy = Instantiate(_interactObject);
                            copy.name = _interactObject.name;
                            _interactObject = copy;
                            Undo.RegisterCreatedObjectUndo(_interactObject, "Create Prefab");
                        }

                        // Rootの子として設定
                        _interactObject.transform.parent = _interactObjectsRoot;
                        _interactObject.transform.localPosition = Vector3.zero;
                        _interactObject.transform.localRotation = Quaternion.identity;
                    }
                }

                /* "見た目となる3Dオブジェクトを設定します。" */
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketWebPageOpenerSettingWindow.VisualSetting.Help"), MessageType.Info);

                GUILayout.Space(3);

                /* "2.コライダーのサイズ調整" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketWebPageOpenerSettingWindow.ColliderScaleSetting"), _settingItemStyle);
                if (GUILayout.Button("Setup Collider"))
                {
                    AdjustCapsuleCollider();
                }

                /* "見た目となる3Dオブジェクトの大きさに自動で設定します。" */
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketWebPageOpenerSettingWindow.ColliderScaleSetting.Help"), MessageType.Info);

                GUILayout.Space(3);

                /* "3.見た目となる3DオブジェクトからColliderを削除" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketWebPageOpenerSettingWindow.VisualColliderDeleteButton"), _settingItemStyle);
                if (GUILayout.Button("Delete All Colliders at [Visual]"))
                {
                    DeleteAllColliders();
                }

                /* "正しく動作させるためには見た目となる3Dオブジェクトにコライダーが含まれないようにする必要があります。" */
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketWebPageOpenerSettingWindow.VisualColliderDeleteButton.Help"), MessageType.Warning);

                GUILayout.Space(3);
            }
            /*Setup URL*/
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketURLOpenerSettingWindow.UrlSetting", (_type == 0 || _type == 2 ? 3 : 4).ToString()), _settingItemStyle);
            if (!_urlOpener.CheckURL(_vrcUrlLauncherProxy.Url.ToString()))
            {
                string allowedURLs = string.Join("\n", _urlOpener.AllowUri);
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketURLOpenerSettingWindow.UrlIsNotAllowList") +"\n" + allowedURLs, MessageType.Error);
            }
                
            SerializedObject proxySO = new SerializedObject(_vrcUrlLauncherProxy);
            proxySO.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(proxySO.FindProperty("_url").FindPropertyRelative("url"));
            if (EditorGUI.EndChangeCheck()) {
                proxySO.ApplyModifiedProperties();
                    
                SerializedObject serializedObject = new SerializedObject(_urlOpener);
                serializedObject.FindProperty("_inputURL").stringValue= _vrcUrlLauncherProxy.Url.ToString();
                serializedObject.ApplyModifiedProperties();
            }

            EditorGUILayout.EndScrollView();
            
            BaseFooter(PrefabNames[_type], _interactObject);
        }

        private void AdjustCapsuleCollider()
        {
            if (_collider == null)
            {
                Debug.LogWarning("CapsuleCollider not found.");
                return;
            }

            var renderers = _vketPrefabInstance.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.Log("Renderer not found.");
                return;
            }

            Bounds totalBounds = CalculateBounds(renderers);

            Vector3 pos = _urlOpener.transform.position;
            Vector3 scale = _urlOpener.transform.lossyScale;

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

            Undo.RecordObject(_collider, "AdjustCollider");
            _collider.center = localCenter;
            _collider.height = localSize.y;
            _collider.radius = localSize.x <= localSize.y ? localSize.x * 0.5f : localSize.y * 0.5f;

            Debug.Log("Setup Completed.");
        }
        private void DeleteAllColliders()
        {
            if (!_interactObjectsRoot)
            {
                Debug.Log($"[{InteractObjectsRootName}] Object not found.");
                return;
            }
            
            var colliders = _interactObjectsRoot.GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                DestroyImmediate(colliders[i]);
            }
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