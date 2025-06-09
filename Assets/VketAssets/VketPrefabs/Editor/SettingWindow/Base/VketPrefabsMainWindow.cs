
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using VketAssets.VketPrefabs.Editor;
using VketAssets.VketPrefabs.VketAttachItem.Editor;

namespace Vket.VketPrefabs
{
    /// <summary>
    /// VketPrefabを管理するメインウィンドウ
    /// </summary>
    public class VketPrefabsMainWindow : EditorWindow
    {
        /// <summary>
        /// Project内のVketPrefabを格納
        /// </summary>
        private List<VketPrefabInformation> _vketPrefabInfos = new List<VketPrefabInformation>();
        
        /// <summary>
        /// スクロール位置
        /// </summary>
        private Vector2 _scrollPosition = Vector2.zero;
        
        /// <summary>
        /// 自動折り返し用スタイル
        /// </summary>
        private GUIStyle _wrapStyle;
        
        /// <summary>
        /// タイトル用スタイル
        /// </summary>
        private GUIStyle _titleStyle;
        
        /// <summary>
        /// 説明用スタイル
        /// </summary>
        private GUIStyle _descriptionStyle;

        /// <summary>
        /// 入稿フォルダ
        /// </summary>
        private static string _exhibitorID;
        
        #if !VKET_TOOLS
        [MenuItem("VketPrefabs/MainMenu")]
        #endif
        public static void OpenMainWindow()
        {
            var window = GetWindow<VketPrefabsMainWindow>(true,"VketPrefabsMainMenu");
            window.minSize = new Vector2(300f, 400f);
            _exhibitorID = "0";
            window.Show();
        }
        
        public static void OpenMainWindow(string exhibitorID)
        {
            var window = GetWindow<VketPrefabsMainWindow>(true,"VketPrefabsMainMenu");
            window.minSize = new Vector2(300f, 400f);
            _exhibitorID = exhibitorID;
            window.Show();
        }

        private void OnEnable()
        {
            LoadVketPrefabInformation();
        }

        /// <summary>
        /// プロジェクト内にある全てのVketPrefabInformationを読み込む
        /// </summary>
        private void LoadVketPrefabInformation()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            var prefabs = guids.Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid))).ToArray();

            _vketPrefabInfos.Clear();
            
            foreach (var prefab in prefabs)
            {
                if (!prefab)
                    continue;

                var info = prefab.GetComponent<VketPrefabInformation>();
                if (!info)
                    continue;

                if (!_vketPrefabInfos.Contains(info))
                {
                    _vketPrefabInfos.Add(info);
                }
            }
        }

        /// <summary>
        /// テキストスタイルの初期化
        /// </summary>
        private void InitStyle()
        {
            _wrapStyle ??= new GUIStyle( GUI.skin.label) { wordWrap = true };
            _titleStyle ??= new GUIStyle( GUI.skin.label )
            {
                wordWrap = true, fontSize = 28,
                fontStyle = FontStyle.Bold
            };
            _titleStyle.normal.textColor = GUI.skin.label.normal.textColor;
            
            _descriptionStyle ??= new GUIStyle( GUI.skin.label )
            {
                wordWrap = true, fontSize = 20,
            };
            _descriptionStyle.normal.textColor = GUI.skin.label.normal.textColor;
        }
        
        private void OnGUI()
        {
            InitStyle();
            
            EditorGUILayout.LabelField(/* VketPrefab一覧 */LocalizedUtility.Get("VketPrefabsMainWindow.Title"), _titleStyle);
            GUILayout.Space(5);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField(/* VketPrefabの一覧です。 */LocalizedUtility.Get("VketPrefabsMainWindow.Description"), _descriptionStyle);
            EditorGUI.indentLevel--;
            GUILayout.Space(10);
            
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            foreach (var info in _vketPrefabInfos)
            {
                ShowVketPrefabInfo(info);
                GUILayout.Space(2);
            }
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// VketPrefabInformation表示UI
        /// 1項目分のUIを表示する
        /// </summary>
        /// <param name="info">prefabのVketPrefabInformationを指定</param>
        private void ShowVketPrefabInfo(VketPrefabInformation info)
        {
            EditorGUILayout.BeginVertical( GUI.skin.box );
            {
                EditorGUILayout.LabelField(/* "名前:{0}" */LocalizedUtility.Get("VketPrefabsMainWindow.ItemName", string.IsNullOrEmpty(info.PrefabName) ? info.gameObject.name : info.PrefabName), _wrapStyle);
                EditorGUILayout.LabelField(/* "説明:" */LocalizedUtility.Get("VketPrefabsMainWindow.ItemDescription"), _wrapStyle);
                EditorGUI.indentLevel+=2;
                EditorGUILayout.LabelField(string.IsNullOrEmpty(LocalizedUtility.Get(info.DescriptionKey)) ? /* "説明無し" */LocalizedUtility.Get("VketPrefabsMainWindow.ItemDescription.None") : LocalizedUtility.Get(info.DescriptionKey), _wrapStyle);
                EditorGUI.indentLevel-=2;

                // 補足説明
                if (!string.IsNullOrEmpty(info.RepletionKey))
                {
                    EditorGUILayout.HelpBox(LocalizedUtility.Get(info.RepletionKey), MessageType.Warning);
                }
                
                if (info.SettingWindowWindowTag != VketPrefabSettingWindowTag.None)
                {
                    if( GUILayout.Button( /* "生成と設定" */LocalizedUtility.Get("VketPrefabsMainWindow.OpenSettingWindowButton")))
                    {
                        ShowWindow(info);
                    }
                }
                else
                {
                    if( GUILayout.Button( /* "生成" */LocalizedUtility.Get("VketPrefabsMainWindow.CreateVketPrefabButton") ) )
                    {
                        CreateVketPrefab(info);
                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
        
        /// <summary>
        /// シーン内のルートオブジェクトを取得
        /// </summary>
        /// <param name="exhibitorID">exhibitorID</param>
        /// <param name="targetScene">対象のシーン</param>
        /// <returns></returns>
        private static GameObject GetExhibitRootObject(string exhibitorID, Scene targetScene)
        {
            var exhibitRootObjects = targetScene
                .GetRootGameObjects()
                .Where(obj => obj.name == exhibitorID)
                .ToArray();

            if (exhibitRootObjects.Length == 0)
            {
                Debug.LogWarning(/* "入稿ルールに則ったオブジェクトが存在しません。" */LocalizedUtility.Get("VketPrefabsMainWindow.ExhibitRootObjects.NotFound"));
                return null;
            }
            else if (exhibitRootObjects.Length > 1)
            {
                Debug.LogWarning(/* "入稿ルールに則ったオブジェクトが複数存在しています。" */LocalizedUtility.Get("VketPrefabsMainWindow.ExhibitRootObjects.Multiple"));
                return null;
            }
            else
            {
                return exhibitRootObjects[0];
            }
        }
        
        /// <summary>
        /// VketPrefabの生成
        /// </summary>
        /// <param name="info">prefabのVketPrefabInformationを指定</param>
        public static Transform CreateVketPrefab(VketPrefabInformation info, bool isMarkSceneDirty = true, bool isHighLight = true)
        {
            if(!info)
                return null;
                
            var prefabInfo = PrefabUtility.InstantiatePrefab(info) as VketPrefabInformation;
            Transform prefab;
            if (prefabInfo)
            {
                prefab = prefabInfo.transform;
                // 生成したプレハブのVketPrefabInformationを削除
                DestroyImmediate(prefabInfo);
                prefabInfo = null;
            }
            else
            {
                Debug.LogWarning(/* "生成対象のVketPrefabが存在しません。" */LocalizedUtility.Get("VketPrefabsMainWindow.CreateVketPrefab.NotFound"));
                return null;
            }
                        
            // シーン変更をマーク
            if(isMarkSceneDirty)
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        
            // 生成されたオブジェクトを選択し、ハイライト
            if (isHighLight)
            {
                Selection.activeTransform = prefab;
                EditorGUIUtility.PingObject(prefab.gameObject);
            }

            // Dynamicに移動
            var exhibitRootObject = GetExhibitRootObject(_exhibitorID, EditorSceneManager.GetActiveScene());
                        
            if (!exhibitRootObject)
                return prefab;
                        
            var dynamicRoot = exhibitRootObject.transform.Find("Dynamic");
            if (!dynamicRoot)
                return prefab;

            prefab.parent = dynamicRoot;
            return prefab;
        }

        private void ShowWindow(VketPrefabInformation vketPrefabInfo)
        {
            switch (vketPrefabInfo.SettingWindowWindowTag)
            {
                case VketPrefabSettingWindowTag.VideoPlayer:
                    VketPrefabSettingWindow.OpenSettingWindow<VketVideoPlayerSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.VideoUrlTrigger:
                    VketPrefabSettingWindow.OpenSettingWindow<VketVideoUrlTriggerSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.SoundFade:
                    VketPrefabSettingWindow.OpenSettingWindow<VketSoundFadeSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.AvatarPedestal:
                    VketPrefabSettingWindow.OpenSettingWindow<VketAvatarPedestalSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.Chair:
                    VketPrefabSettingWindow.OpenSettingWindow<VketChairSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.FittingChair:
                    VketPrefabSettingWindow.OpenSettingWindow<VketFittingChairSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.Pickup:
                    VketPrefabSettingWindow.OpenSettingWindow<VketPickupSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.FollowPickup:
                    VketPrefabSettingWindow.OpenSettingWindow<VketFollowPickupSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.WebPageOpener:
                    VketPrefabSettingWindow.OpenSettingWindow<VketWebPageOpenerSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.URLOpenr:
                    VketPrefabSettingWindow.OpenSettingWindow<VketURLOpenerSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.LanguageSwitcher:
                    VketPrefabSettingWindow.OpenSettingWindow<VketLanguageSwitcherSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.GroupPageOpener:
                    VketPrefabSettingWindow.OpenSettingWindow<VketGroupPageOpenerSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.StorePreviewOpener:
                    VketPrefabSettingWindow.OpenSettingWindow<VketStorePreviewOpenerSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.AttachItem:
                    VketPrefabSettingWindow.OpenSettingWindow<VketAttachItemSettingWindow>(vketPrefabInfo);
                    break;
                case VketPrefabSettingWindowTag.VketBGMFader:
                    VketPrefabSettingWindow.OpenSettingWindow<VketBGMFaderSettingWindow>(vketPrefabInfo);
                    break;
                default:
                    break;
            }
        }
    }
}
