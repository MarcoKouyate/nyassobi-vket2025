
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    public class VketFittingChairSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        
        private Transform _interactObjectsRoot;
        private GameObject _interactObject;

        private Transform _enterLocation;
        #endregion

        #region const定義
        private const string InteractObjectsRootName = "Visual";
        private const string EnterLocationRelativePath = "EnterLocation";
        #endregion
        
        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 500f);

            if (_vketPrefabInstance)
            {
                _interactObjectsRoot = _vketPrefabInstance.Find(InteractObjectsRootName);
                _enterLocation = _vketPrefabInstance.Find(EnterLocationRelativePath);
            }

            // 見た目のオブジェクトを取得
            if (_interactObjectsRoot && _interactObjectsRoot.childCount != 0)
            {
                _interactObject = _interactObjectsRoot.GetChild(0).gameObject;
            }
        }
        
        private void OnGUI()
        {
            InitStyle();
            
            if(!BaseHeader("VketFittingChair"))
                return;
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);

            /* "1.見た目にする3Dモデルの設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketFittingChairSettingWindow.VisualSetting"), _l3Style);
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
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketFittingChairSettingWindow.VisualSetting.Help"), MessageType.Info);

            GUILayout.Space(3);

            /* "2.見た目となる3DオブジェクトからColliderを削除" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketFittingChairSettingWindow.VisualColliderDeleteButton"), _l3Style);
            if (GUILayout.Button("Delete All Colliders at [Visual]"))
            {
                DeleteAllColliders();
            }

            /* "正しく動作させるためには見た目となる3Dオブジェクトにコライダーが含まれないようにする必要があります。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketFittingChairSettingWindow.VisualColliderDeleteButton.Help"), MessageType.Warning);

            GUILayout.Space(3);

            /* "3.座る位置の調整" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketFittingChairSettingWindow.EnterLocationSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            var pos = EditorGUILayout.Vector3Field("position", _enterLocation.localPosition);
            if (EditorGUI.EndChangeCheck()) 
            {
                Undo.RecordObject(_enterLocation, "Move EnterLocation Position");
                _enterLocation.localPosition = pos;
            }
            
            /* "\"Enter Location\" オブジェクトを選択" */
            if (GUILayout.Button(LocalizedUtility.Get("VketFittingChairSettingWindow.SelectEnterLocationButton")))
            {
                Selection.activeTransform = _enterLocation;
                EditorGUIUtility.PingObject(_enterLocation.gameObject);
            }
            
            /* "\"EnterLocation\"オブジェクトを動かして座る位置を調整してください。
             \"EnterLocation\"オブジェクトの高さにHipが来るよう調整されます。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketFittingChairSettingWindow.SelectEnterLocationButton.Help"), MessageType.Info);

            GUILayout.Space(3);
            
            EditorGUILayout.EndScrollView();
            
            BaseFooter("VketFittingChair", _interactObject);
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
    }
}
