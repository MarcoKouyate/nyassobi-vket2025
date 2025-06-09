using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VketAssets.VketPrefabs.Editor;
using VRC.SDK3.Components;

namespace Vket.VketPrefabs
{
    public class VketAvatarPedestalSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        
        private VketAvatarPedestal _vketAvatarPedestal;
        
        private int _type;
        private VRCAvatarPedestal _avatarPedestal;
        
        // 2Dの設定画像
        private Image _image;
        
        private Transform _interactObjectsRoot;
        private GameObject _interactObject;
        private CapsuleCollider _collider;

        #endregion

        #region const定義
        private const string InteractObjectsRootName = "Visual";
        #endregion
        
        private readonly string[] _prefabNames = new string[]
        {
            "VketAvatarPedestal_Default", "VketAvatarPedestal_2D",
            "VketAvatarPedestal_3D"
        };

        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 500f);

            if (_vketPrefabInstance)
            {
                _vketAvatarPedestal = _vketPrefabInstance.GetComponent<VketAvatarPedestal>();
                _collider = _vketPrefabInstance.GetComponent<CapsuleCollider>();
                _interactObjectsRoot = _vketPrefabInstance.Find(InteractObjectsRootName);
            }
            
            if (_vketAvatarPedestal)
            {
                _type = (int)_vketAvatarPedestal.GetProgramVariable("type");
                _avatarPedestal = _vketAvatarPedestal.GetProgramVariable("avatarPedestal") as VRCAvatarPedestal;
            }
            
            // 見た目のオブジェクトを取得
            if (_interactObjectsRoot && _interactObjectsRoot.childCount != 0)
            {
                _interactObject = _interactObjectsRoot.GetChild(0).gameObject;
            }
            
            // AvatarPedestal_2D
            if (_type == 1)
            {
                var imageTransform = _vketAvatarPedestal.transform?.Find("Canvas")?.Find("Image");
                
                if(imageTransform)
                    _image = imageTransform.GetComponent<Image>();
            }
        }
        
        private void OnGUI()
        {
            InitStyle();

            if (!BaseHeader(_prefabNames[_type]))
                return;
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            
            /* "1.Blueprint IDを指定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.SelectBlueprintId"), _l3Style);
            EditorGUI.BeginChangeCheck();
            var blueprintId = EditorGUILayout.TextField("Blueprint Id", _avatarPedestal.blueprintId);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(_avatarPedestal, "Modify blueprint id");
                _avatarPedestal.blueprintId = blueprintId;
            }
            /* "ペデスタルにするpublicアバターのIDを設定します。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.SelectBlueprintId.Help"), MessageType.Info);

            GUILayout.Space(3);

            if (_type == 0 || _type == 1)
            {
                /* "2.サイズの調整" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.SizeSetting"), _l3Style);
                EditorGUI.BeginChangeCheck();
                var scale = EditorGUILayout.Vector2Field("Scale", _vketPrefabInstance.localScale);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_vketPrefabInstance, "Change Scale");
                    _vketPrefabInstance.localScale = new Vector3(scale.x, scale.y, 1f);
                }

                GUILayout.Space(3);
            }

            // VketAvatarPedestal_2D
            if (_type == 1)
            {
                /* "3.画像設定" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.TextureSetting"), _l3Style);
                
                if (_image == null)
                {
                    EditorGUILayout.HelpBox("Not Found \"VketAvatarPedestal2D/Canvas/Image\"", MessageType.Error);
                }
                else
                {
                    EditorGUI.BeginChangeCheck();
                    var imageSprite = SpriteField(_image.sprite);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(_image, "Modify source image");
                        _image.sprite = imageSprite;
                        _image.enabled = false;
                        _image.enabled = true;
                    }
                }
                
                GUILayout.Space(3);
            }

            // VketAvatarPedestal_3D
            if (_type == 2)
            {
                /* 2.見た目にする3Dモデルの設定 */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.VisualSetting"), _l3Style);
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
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.VisualSetting.Help"), MessageType.Info);

                GUILayout.Space(3);

                /* "3.コライダーのサイズ調整" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.ColliderScaleSetting"), _l3Style);
                if (GUILayout.Button("Setup Collider"))
                {
                    AdjustCapsuleCollider();
                }

                /* "見た目となる3Dオブジェクトの大きさに自動で設定します。" */
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.ColliderScaleSetting.Help"), MessageType.Info);

                GUILayout.Space(3);

                /* "4.見た目となる3DオブジェクトからColliderを削除" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.VisualColliderDeleteButton"), _l3Style);
                if (GUILayout.Button("Delete All Colliders at \"Visual\""))
                {
                    DeleteAllColliders();
                }

                /* "正しく動作させるためには見た目となる3Dオブジェクトにコライダーが含まれないようにする必要があります。" */
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketAvatarPedestalSettingWindow.VisualColliderDeleteButton.Help"), MessageType.Warning);

                GUILayout.Space(3);
            }

            EditorGUILayout.EndScrollView();
            
            BaseFooter(_prefabNames[_type], _interactObject);
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

        private void AdjustCapsuleCollider()
        {
            if (_collider == null)
            {
                Debug.LogWarning("CapsuleCollider not found.");
                return;
            }

            if (!_interactObjectsRoot)
            {
                Debug.Log($"[{InteractObjectsRootName}] Object not found.");
                return;
            }
            
            var renderers = _interactObjectsRoot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.Log("Renderer not found.");
                return;
            }

            Bounds totalBounds = CalculateBounds(renderers);

            Vector3 pos = _vketAvatarPedestal.transform.position;
            Vector3 scale = _vketAvatarPedestal.transform.lossyScale;

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