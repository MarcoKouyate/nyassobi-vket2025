
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    public class VketPickupSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        private VketPickup _vketPickup;
        private Transform _interactObjectsRoot;
        private GameObject _interactObject;

        private AnimatorOverrideController _overrideControllerBase;
        private AnimatorOverrideController _overrideController;
        private Animator _animator;
        
        private CapsuleCollider _collider;
        #endregion
        
        #region const定義
        private const string InteractObjectsRootName = "ModeController";
        #endregion

        #region UI設定用
        GUIStyle _advancedOptionStyle;
        private bool _foldout;
        #endregion
        
        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 500f);

            if (_vketPrefabInstance)
            {
                _vketPickup = _vketPrefabInstance.GetComponent<VketPickup>();
                _collider = _vketPrefabInstance.GetComponent<CapsuleCollider>();
                _interactObjectsRoot = _vketPrefabInstance.Find(InteractObjectsRootName);
            }
            
            if (_vketPickup)
            {
                _overrideControllerBase = _vketPickup.GetProgramVariable("overrideControllerBase") as AnimatorOverrideController;
                _overrideController = _vketPickup.GetProgramVariable("overrideController") as AnimatorOverrideController;
                _animator = _vketPickup.GetProgramVariable("animator") as Animator;
            }
            
            // 見た目のオブジェクトを取得
            if (_interactObjectsRoot && _interactObjectsRoot.childCount != 0)
            {
                _interactObject = _interactObjectsRoot.GetChild(0).gameObject;
            }
        }

        protected override void InitStyle()
        {
            base.InitStyle();
            
            if (_advancedOptionStyle != null)
                return;
            
            _advancedOptionStyle = new GUIStyle(EditorStyles.foldout)
            {
                fontSize = 14
            };
            _advancedOptionStyle.normal.textColor = Color.yellow;
        }
        
        private void OnGUI()
        {
            InitStyle();

            if(!BaseHeader("VketPickup"))
                return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);

            /* "1.見た目にする3Dモデルの設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketPickupSettingWindow.VisualSetting"), _l3Style);
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
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketPickupSettingWindow.VisualSetting.Help"), MessageType.Info);

            GUILayout.Space(3);

            /* "2.コライダーのサイズ調整" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketPickupSettingWindow.ColliderScaleSetting"), _l3Style);
            if (GUILayout.Button("Setup Collider"))
            {
                AdjustCapsuleCollider();
            }

            /* "見た目となる3Dオブジェクトの大きさに自動で設定します。
             少し小さめに設定されるため、細かい調整はInspectorのCapcelColliderのサイズ変更を手動で行ってください。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketPickupSettingWindow.ColliderScaleSetting.Help"), MessageType.Info);

            GUILayout.Space(3);
            
            /* "高度な設定" */
            _foldout = EditorGUILayout.Foldout(_foldout,LocalizedUtility.Get("VketPickupSettingWindow.AdvancedSetting"), _advancedOptionStyle);
            if (_foldout)
            {
                EditorGUI.indentLevel+=2;
                GUILayout.Space(3);
                /* "アニメーションの設定" */
                EditorGUILayout.LabelField(LocalizedUtility.Get("VketPickupSettingWindow.AnimationSetting"), _l3Style);
                EditorGUI.BeginChangeCheck();
                var overrideController =
                    EditorGUILayout.ObjectField("Override Controller", _overrideController,
                        typeof(AnimatorOverrideController), false) as AnimatorOverrideController;
                if (EditorGUI.EndChangeCheck())
                {
                    if (overrideController == _overrideControllerBase)
                        return;

                    _overrideController = overrideController;
                    _vketPickup.SetProgramVariable("overrideController", _overrideController);

                    Undo.RecordObject(_animator, "Modify animator");
                    _animator.runtimeAnimatorController = _overrideController;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(_animator);
                    return;
                }

                if (_overrideController == null)
                {
                    if (GUILayout.Button("Create new override controller"))
                    {
                        var newController = CreateOverrideController(_overrideControllerBase);
                        if (newController != null)
                        {
                            _overrideController = newController;
                            _vketPickup.SetProgramVariable("overrideController", _overrideController);
                            PrefabUtility.RecordPrefabInstancePropertyModifications(_vketPickup);
                            Undo.RecordObject(_animator, "Modify animator");
                            _animator.runtimeAnimatorController = newController;
                            PrefabUtility.RecordPrefabInstancePropertyModifications(_animator);
                            return;
                        }
                    }
                }
                else
                {
                    // If Attach Override Controller Base Then Detach
                    // Baseと同じものが設定されている場合は削除
                    if (_overrideController == _overrideControllerBase)
                    {
                        _overrideController = null;
                        _vketPickup.SetProgramVariable("overrideController", null);
                        _animator.runtimeAnimatorController = null;
                        EditorUtility.SetDirty(_animator);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(_vketPickup);
                        PrefabUtility.RecordPrefabInstancePropertyModifications(_animator);
                        return;
                    }

                    // Draw Field of Override Animation Clips
                    List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
                        new List<KeyValuePair<AnimationClip, AnimationClip>>();
                    overrideController.GetOverrides(overrides);

                    EditorGUI.indentLevel++;
                    EditorGUI.BeginChangeCheck();
                    var clips = new AnimationClip[4];
                    clips[0] = (AnimationClip)EditorGUILayout.ObjectField(overrides[0].Key.name, overrides[0].Value,
                        typeof(AnimationClip), false);
                    clips[1] = (AnimationClip)EditorGUILayout.ObjectField(overrides[1].Key.name, overrides[1].Value,
                        typeof(AnimationClip), false);
                    clips[2] = (AnimationClip)EditorGUILayout.ObjectField(overrides[2].Key.name, overrides[2].Value,
                        typeof(AnimationClip), false);
                    clips[3] = (AnimationClip)EditorGUILayout.ObjectField(overrides[3].Key.name, overrides[3].Value,
                        typeof(AnimationClip), false);
                    if (EditorGUI.EndChangeCheck())
                    {
                        for (int i = 0; i < 4; i++)
                            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clips[i]);

                        Undo.RecordObject(overrideController, "Modify override clip");
                        overrideController.ApplyOverrides(overrides);
                    }

                    EditorGUI.indentLevel--;
                }

                /* "Animationさせたい場合は[Create new override controller]ボタンを押して、新しいOverrideControllerを保存してからAnimationClipを登録してください。" */
                EditorGUILayout.HelpBox(LocalizedUtility.Get("VketPickupSettingWindow.AnimationSetting.Help"), MessageType.Info);

                GUILayout.Space(3);
                EditorGUI.indentLevel-=2;
            }

            EditorGUILayout.EndScrollView();
            
            BaseFooter("VketPickup", _interactObject);
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

            Vector3 pos = _vketPickup.transform.position;
            Vector3 scale = _vketPickup.transform.lossyScale;

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
