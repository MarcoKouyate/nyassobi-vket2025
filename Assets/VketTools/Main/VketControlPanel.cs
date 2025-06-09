#if VIT_DEECK_CORE && VIT_DEECK_EXHIBITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using Cysharp.Threading.Tasks;
using HarmonyLib;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VitDeck.Main.ValidatedExporter;
using VitDeck.ExhibitorGUI;
using VitDeck.Utilities;
using VitDeck.Validator;
using Vket.VketPrefabs;
using VketTools.Utilities;
using VketTools.Utilities.SS;
using VRC.Core;
using VRC.SDK3.Components;
using VRC.SDK3.Editor;
using VRC.SDKBase;
using VRC.SDKBase.Editor;
using UdonSharpEditor;
using UdonSharp;
using UnityEditor.Build;
using UnityEditor.Callbacks;
using UnityEditor.Compilation;
using VitDeck.TemplateLoader;
using VitDeck.Validator.GUI;
using VketAssets.Utilities.Language.Runtime;
using VketTools.Storage.AWS;
using VketTools.Validator;
using VketTools.ExhibitionResources;
using VketTools.Networking;
using VitDeckValidator = VitDeck.Validator.Validator;
using Assembly = System.Reflection.Assembly;
using AssetUtility = VketTools.Utilities.AssetUtility;
using Status = VketTools.Utilities.Sequence.RunStatus;

namespace VketTools.Main
{
    public class VketControlPanel : EditorWindow
    {
        private static readonly string EventUrl = "https://vket.com/";
        private static readonly string CircleMypageUrl = "https://vket.com/mypage";
        private static readonly string DefaultRuleSet = "Vket2025SummerConceptWorldRuleSet";
        private static readonly string DefaultExportSetting = "VketExportSetting";
        
        private static readonly VketApi.WorldData[] CompanyWorlds =
        {
            new(){world_id = 2, name_ja = "パラリアル東京", name_en = "Parareal Tokyo", concept = new VketApi.WorldData.WorldConcept()},
            new(){world_id = 3, name_ja = "パラリアルハワイ", name_en = "Parareal Hawaii", concept = new VketApi.WorldData.WorldConcept()},
        };
        
        private static readonly VketApi.WorldData[] CommunityWorlds =
        {
            new(){world_id = 1001, name_ja = "コミュニティコラボブースプラン", name_en = "Community Collaboration Booth Plan", concept = new VketApi.WorldData.WorldConcept()},
        };

        private Texture2D _circleThumbnail;

        /// <summary>
        /// ワールドのビルド中はtrueになる
        /// </summary>
        private static bool _isBuildWorld;

        /// <summary>
        /// タスクキャンセル用
        /// </summary>
        private static CancellationTokenSource _cts;

        /// <summary>
        /// 実行中のタスク
        /// </summary>
        private static UniTask _currentUniTask;

        /// <summary>
        /// 入稿シーケンス
        /// </summary>
        private static DraftSequence _currentSeq = DraftSequence.None;

        /// <summary>
        /// スクロール位置
        /// </summary>
        private static Vector2 _selectWindowScrollPos = Vector2.zero;

        private static Vector2 _controlPanelWindowScrollPos = Vector2.zero;

        /// <summary>
        /// パッケージタイプ
        /// </summary>
        private static VersionInfo.PackageType _packageType;

        /// <summary>
        /// デバッグモード状態のキャッシュ
        /// </summary>
        private bool _debugMode;

        #region VRCSDKが開かれた場合にウィンドウを開き、ウィンドウを閉じた場合に相互に閉じる処理

        /// <summary>
        /// VRCSDKコントロールパネルを開く 
        /// </summary>
        [MenuItem("VketTools/Show Control Panel", false, 600)]
        private static void OpenSDKWindow()
        {
            if (HasOpenInstances<VRCSdkControlPanel>() && !HasOpenInstances<VketControlPanel>())
            {
                OpenWindowIfClose();
                return;
            }
            
            var sdkControlPanelType = Type.GetType("VRCSdkControlPanel,VRC.SDKBase.Editor");
            if (sdkControlPanelType == null)
            {
                Debug.LogError("Exception:Not Found VRCSdkControlPanelType");
                return;
            }

            MethodInfo info = null;
            try
            {
                info = sdkControlPanelType.GetMethod("ShowControlPanel", BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
            }
            catch (Exception)
            {
                Debug.LogError("Exception:Not Found VRCSdkControlPanel.ShowControlPanel Method");
            }

            if (info != null)
            {
                info.Invoke(null, null);
            }
            else
            {
                Debug.LogError("Not Found VRCSdkControlPanel.ShowControlPanel Method");
            }
        }

        /// <summary>
        /// PlayMode時に非表示
        /// </summary>
        /// <returns>PlayMode時に非表示</returns>
        [MenuItem("VketTools/Show Control Panel", true)]
        private static bool ValidateVketToolsControlPanel() => !EditorApplication.isPlaying;

        [MenuItem("VRChat SDK/ Show Control Panel", true)]
        private static bool ValidateVRChatSDKControlPanel() => !EditorApplication.isPlaying;

        private bool isClosing;
        public bool SdkWindowClosing;

        private void SafeClose()
        {
            if (!isClosing)
            {
                isClosing = true;
                Close();
            }
        }
        
        private static void OnVrcSdkPanelEnable(object sender, EventArgs e)
        {
            OpenWindowIfClose();
        }

        private static void OnVrcSdkPanelDisable(object sender, EventArgs e)
        {
            CloseWindowIfOpen(true);
        }

        private void OnEnable()
        {
            OpenSDKWindow();
            _debugMode = Utilities.Hiding.HidingUtil.DebugMode;
        }
        
        private void OnDisable()
        {
            isClosing = true;
            
            if (!SdkWindowClosing)
            {
                if (HasOpenInstances<VRCSdkControlPanel>())
                {
                    var vrcsdkPanel = GetWindow<VRCSdkControlPanel>();
                    if (vrcsdkPanel)
                    {
                        vrcsdkPanel.Close();
                    }
                }
            }
            
            // 入稿ボタンエリアの色テクスチャ解放
            if (_draftBoxStyle != null)
            {
                if (_draftBoxStyle.normal.background)
                {
                    DestroyImmediate(_draftBoxStyle.normal.background);
                    _draftBoxStyle.normal.background = null;
                }
                _draftBoxStyle = null;
            }
        }

        /// <summary>
        /// SDKパネルが開いた際に呼ぶ想定
        /// </summary>
        private static void OpenWindowIfClose()
        {
            bool isOpen = HasOpenInstances<VketControlPanel>();
            var panel = GetWindow<VketControlPanel>(true, "VketTools");
            panel.Show();
            if (!isOpen)
            {
                panel.minSize = new Vector2(300f, 210f + GetFooterHeight(null) + GetToolButtonsHeight());
            }
        }

        /// <summary>
        /// SDKパネルが閉じた際に呼ぶ想定
        /// </summary>
        private static void CloseWindowIfOpen(bool sdkWindowClosing = false)
        {
            var objectsOfTypeAll = Resources.FindObjectsOfTypeAll(typeof(VketControlPanel));
            var window = (objectsOfTypeAll.FirstOrDefault() as VketControlPanel);
            if (window)
            {
                window.SdkWindowClosing = sdkWindowClosing;
                window.SafeClose();
            }
        }

        #endregion

#if VKET_TOOLS
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            // デバッグモードでない場合、Unity起動時にシンボルにVITDECK_HIDE_MENUITEMが定義されていない場合は定義を追加する。
            // デバッグモードでない場合はメニューにVitDeckを表示しなくなる
            // インポート時、デバッグモードでない場合VITDECK_HIDE_MENUITEMシンボルの追加
            PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Standalone, out string[] defines);
            var symbols = defines.ToList();

            if (!Utilities.Hiding.HidingUtil.DebugMode)
            {
                if (!symbols.Contains("VITDECK_HIDE_MENUITEM"))
                {
                    symbols.Add("VITDECK_HIDE_MENUITEM");
                }
            }

            PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Standalone, symbols.ToArray());
            
            var info = AssetUtility.LoginInfoData;
            if (info)
            {
                // 既にログイン中の場合、ValidatorRuleの初期化
                if (info.IsAvailable)
                {
                    var userSettings = SettingUtility.GetSettings<UserSettings>();
                    if (string.IsNullOrEmpty(userSettings.validatorRuleSetType))
                    {
                        switch (AssetUtility.VersionInfoData.Type)
                        {
                            default:
                                // "Assets/id"
                                if(info.SelectedCircle != null)
                                    userSettings.validatorFolderPath = "Assets/" + info.SelectedCircle.circle_id;
                                break;
                        }

                        userSettings.validatorRuleSetType = GetValidatorRule();
                    }
                }

                // Initialize時にリフレッシュ
                if (!EditorApplication.isPlaying && info.OauthEndFlg)
                {
                    RefreshAuth().Forget();
                }
            }

            // InfoScriptableObject群をエディタ上で編集不可に変更
            EditorApplication.delayCall += AssetUtility.SetHideFlags;
            
            // 現在のシーンで境界線の描画
            EditorApplication.delayCall += () => DrawBoundsLimitGizmos(SceneManager.GetActiveScene());

            // プレイモード変更時に実行する関数登録
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // シーンを開いたときに実行する関数を登録
            EditorSceneManager.sceneOpened += OnSceneOpened;

            // SDKパネルのOpen,Close時の処理を追加
            VRCSdkControlPanel.OnSdkPanelEnable += OnVrcSdkPanelEnable;
            VRCSdkControlPanel.OnSdkPanelDisable += OnVrcSdkPanelDisable;
            
            VitDeck.Language.LocalizedMessage.AddDictionary(SystemLanguage.Japanese, AssetDatabase.LoadAssetAtPath<VitDeck.Language.LanguageDictionary>("Assets/VitDeck/Config/LanguageDictionaries/Japanese.asset")); // 日本語用外部辞書の追加登録
            VitDeck.Language.LocalizedMessage.AddDictionary(SystemLanguage.English, AssetDatabase.LoadAssetAtPath<VitDeck.Language.LanguageDictionary>("Assets/VitDeck/Config/LanguageDictionaries/English.asset")); // 英語用外部辞書の追加登録
        }
#endif

        /// <summary>
        /// ログイン処理
        /// </summary>
        private static async UniTask RefreshAuth()
        {
            var info = AssetUtility.LoginInfoData;
            if (!info)
                return;
            
            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.stable:
                    await info.RefreshLogin();
                    break;
                case VersionInfo.PackageType.company:
                case VersionInfo.PackageType.community:
                    await info.GuestLogin(info.SpecialExhibitedId);
                    break;
                case VersionInfo.PackageType.develop:
                    await info.GuestLogin(info.SpecialExhibitedId, true);
                    break;
            }
        }

        /// <summary>
        /// シーンが開かれた場合に呼ばれる
        /// </summary>
        /// <param name="scene">開いたシーン</param>
        /// <param name="mode">OpenSceneMode</param>
        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            DrawBoundsLimitGizmos(scene);
        }

        /// <summary>
        /// ブースの範囲境界線の描画
        /// </summary>
        /// <param name="scene">チェックするシーン</param>
        private static void DrawBoundsLimitGizmos(Scene scene)
        {
            var info = AssetUtility.LoginInfoData;
            var userSettings = SettingUtility.GetSettings<UserSettings>();
            if (string.IsNullOrEmpty(scene.name) || string.IsNullOrEmpty(scene.path) || !info || !info.IsSelectedWorld ||
                SceneManager.GetActiveScene() != scene || LoginInfo.CurrentWorldDefinition == null ||
                Path.GetDirectoryName(scene.path)!.Replace("/", @"\") !=
                userSettings.validatorFolderPath.Replace("/", @"\"))
            {
                return;
            }

            if (scene.name != GetExhibitorID())
                return;

            var ruleSets = VitDeckValidator.GetRuleSets().Where(a => a.GetType().Name == GetValidatorRule()).ToArray();
            if (ruleSets.Any())
            {
                var selectedRuleSet = ruleSets.First();
                var baseFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(userSettings.validatorFolderPath);
                var target = selectedRuleSet.TargetFinder.Find(AssetDatabase.GetAssetPath(baseFolder), false);
                var type = typeof(VRCBoothBoundsRule);
                foreach (var rule in selectedRuleSet.GetRules())
                {
                    if (rule.GetType() == type)
                    {
                        // BoothBoundsRuleのLogicを呼び出し、BoothRangeIndicatorをシーンに配置することでGizmoを描画する
                        ReflectionUtility.InvokeMethod(type, "Logic", rule, new object[] { target });
                    }
                }
            }
        }

        /// <summary>
        /// BoothBoundsRuleのComponentsをシーンから削除する
        /// </summary>
        /// <param name="scene"></param>
        private static void ResetBoundsComponents(Scene scene)
        {
            var info = AssetUtility.LoginInfoData;
            var userSettings = SettingUtility.GetSettings<UserSettings>();
            if (string.IsNullOrEmpty(scene.name) || string.IsNullOrEmpty(scene.path) || !info || !info.IsSelectedWorld ||
                SceneManager.GetActiveScene() != scene || LoginInfo.CurrentWorldDefinition == null ||
                Path.GetDirectoryName(scene.path)!.Replace("/", @"\") !=
                userSettings.validatorFolderPath.Replace("/", @"\"))
            {
                return;
            }

            if (scene.name != GetExhibitorID())
                return;

            var ruleSets = VitDeckValidator.GetRuleSets().Where(a => a.GetType().Name == GetValidatorRule()).ToArray();
            if (ruleSets.Any())
            {
                var selectedRuleSet = ruleSets.First();
                var type = typeof(VRCBoothBoundsRule);
                foreach (var rule in selectedRuleSet.GetRules())
                {
                    if (rule.GetType() == type)
                    {
                        ReflectionUtility.InvokeMethod(type, "Reset", rule, Array.Empty<object>());
                    }
                }
            }
        }

        private void OnGUI()
        {
            DrawLanguageSettings();
            
            // 入稿処理中表示
            if (_currentSeq != DraftSequence.None)
            {
                DraftSequenceWindow();
                Repaint();
                return;
            }

            // パスに日本語が含まれている場合は作り直し
            if (NonAsciiPathChecker.IsErrorDataPath())
            {
                EditorGUILayout.LabelField( /* "現在のプロジェクトフォルダ「{0}」は使用できません。\n日本語等のマルチバイト文字が含まれないようにしてください。" */ AssetUtility.GetMain("Vket_ControlPanel.OnGUI.ErrorDataPathMessage", NonAsciiPathChecker.GetProjectDirectory()), EditorStyles.wordWrappedLabel);
                Repaint();
                return;
            }

            // 設定パネルの操作ができない特殊な状態の表示
            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.develop:
                case VersionInfo.PackageType.stable:
                {
                    // VRCSDKにログインしているか確認
                    if (!APIUser.IsLoggedIn)
                    {
                        EditorGUILayout.LabelField( /* "VRChat SDKにログインしてください。" */ AssetUtility.GetMain("Vket_ControlPanel.OnGUI.SDKLoginMessage"));
                        Repaint();
                        return;
                    }

                    // VRChat.exeのパスが合っているか確認
                    if (!ExistVRCClient())
                    {
                        EditorGUILayout.LabelField( /* "VRChatの起動パスが指定されていません。" */ AssetUtility.GetMain("Vket_ControlPanel.OnGUI.AppPathError.Title"));
                        EditorGUILayout.HelpBox( /* "VRChat SDKのSettingタブを開き、Installed Client PathにVRChat.exeを指定してください。" */ AssetUtility.GetMain("Vket_ControlPanel.OnGUI.AppPathError.Message"), MessageType.Info);
                        Repaint();
                        return;
                    }

                    // VRChat SDKが非表示
                    if (!VRCSdkControlPanel.window)
                    {
                        EditorGUILayout.LabelField( /* "VRChat SDKが表示されていません。「VRChat SDK/Show Control Panel」から表示してください。" */ AssetUtility.GetMain("Vket_ControlPanel.OnGUI.NotOpenSDKPanel.Title"));
                        Repaint();
                        return;
                    }

                    // VRChat SDK のBuilderタブが開いていない
                    if (VRCSettings.ActiveWindowPanel != 1)
                    {
                        EditorGUILayout.LabelField( /* "VRChat SDKのBuilderタブが開いていません。" */ AssetUtility.GetMain("Vket_ControlPanel.OnGUI.NotBuilderTab.Title"));
                        EditorGUILayout.HelpBox( /* "VRChat SDKのBuilderタブを開いてください。" */ AssetUtility.GetMain("Vket_ControlPanel.OnGUI.NotBuilderTab.Message"), MessageType.Info);

                        if (GUILayout.Button( /* "Builderタブを開く" */ AssetUtility.GetMain("Vket_ControlPanel.OnGUI.NotBuilderTab.OpenButton")))
                        {
                            // SDKにログインしていること前提
                            OpenSDKBuilderTab();
                        }

                        Repaint();
                        return;
                    }
                }
                    break;
            }

            // タスク実行中はメニューを触れない
            EditorGUI.BeginDisabledGroup(!_currentUniTask.GetAwaiter().IsCompleted);

            var info = AssetUtility.LoginInfoData;
            if (info.IsLogin)
            {
                switch (AssetUtility.VersionInfoData.Type)
                {
                    case VersionInfo.PackageType.develop:
                    case VersionInfo.PackageType.stable:
                    {
                        if (info.IsSelectedWorld)
                        {
                            ControlPanelWindow();
                        }
                        else
                        {
                            SelectWindow();
                        }

                        break;
                    }
                    default:
                    {
                        if (info.SpecialExhibitedId == 0)
                        {
                            LoginWindow();
                        }
                        else
                        {
                            ControlPanelWindow();
                        }

                        break;
                    }
                }
            }
            // 未ログイン時
            else
            {
                if (LoginInfo.IsWaitAuth)
                {
                    // 認可コード入力ウィンドウ
                    WaitAuthWindow();
                }
                else
                {
                    // ログイン画面
                    LoginWindow();
                }
            }

            EditorGUI.EndDisabledGroup();
        }

        private void Update()
        {
            if(AssetUtility.VersionInfoData.Type != VersionInfo.PackageType.stable)
                return;
            
            LoginInfo info = AssetUtility.LoginInfoData;
            if (!info) return;
            
            if (info.NeedRefresh() && !EditorApplication.isPlaying)
            {
                RefreshAuth().Forget();
            }
        }

        #region ログイン画面

        /// <summary>
        /// ログイン画面の描画
        /// </summary>
        private void LoginWindow()
        {
            GUILayout.Space(5);

            GUIStyle box = new GUIStyle(GUI.skin.box);
            GUIStyle l1 = new GUIStyle(GUI.skin.label);
            GUIStyle l2 = new GUIStyle(GUI.skin.label);

            l1.fontSize = 25;
            l1.fixedHeight = 30;
            l2.fontSize = 15;
            l2.fixedHeight = 23;

            float controlWidth = position.width - position.width / 3;

            LoginInfo info = AssetUtility.LoginInfoData;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUIContent content = new GUIContent( /* ログイン */AssetUtility.GetMain("Vket_ControlPanel.Login"));
                EditorGUILayout.LabelField(content, l1, GUILayout.Width(l1.CalcSize(content).x));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(30);

            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.company:
                {
                    EditorGUILayout.BeginVertical(box);
                    {
                        EditorGUILayout.Space();

                        info.SpecialExhibitedId = EditorGUILayout.IntField("ID", info.SpecialExhibitedId);
                        // 0以外を選択させる
                        bool isSelect = info.SpecialExhibitedId <= 0;
                        if (isSelect)
                        {
                            EditorGUILayout.HelpBox( /* "※IDを入力してください。" */
                                AssetUtility.GetMain("Vket_ControlPanel.LoginWindow.InputCircleID"), MessageType.Error);
                        }

                        EditorGUI.BeginDisabledGroup(isSelect);

                        DrawCompanyBoothContent(info, controlWidth, CompanyWorlds[0]);
                        DrawCompanyBoothContent(info, controlWidth, CompanyWorlds[1]);

                        EditorGUI.EndDisabledGroup();

                        EditorGUILayout.Space();

                        GUILayout.Space(30);

                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndVertical();
                    break;
                }
                case VersionInfo.PackageType.community:
                {
                    EditorGUILayout.BeginVertical(box);
                    {
                        EditorGUILayout.Space();

                        info.SpecialExhibitedId = EditorGUILayout.IntField("サークルID", info.SpecialExhibitedId);
                        // 0以外を選択させる
                        bool isSelect = info.SpecialExhibitedId <= 0;
                        if (isSelect)
                        {
                            EditorGUILayout.HelpBox( /* "※IDを入力してください。" */
                                AssetUtility.GetMain("Vket_ControlPanel.LoginWindow.InputCircleID"), MessageType.Error);
                        }

                        EditorGUI.BeginDisabledGroup(isSelect);
                        DrawCompanyBoothContent(info, controlWidth, CommunityWorlds[0]);

                        EditorGUI.EndDisabledGroup();

                        EditorGUILayout.Space();

                        GUILayout.Space(30);

                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndVertical();
                    break;
                }
                case VersionInfo.PackageType.develop:
                {
                    EditorGUILayout.BeginVertical(box);
                    {
                        EditorGUILayout.Space();

                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            GUIContent content = new GUIContent("テストログイン");
                            EditorGUILayout.LabelField(content, l2, GUILayout.Width(l2.CalcSize(content).x));
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(30);

                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            
                            EditorGUI.BeginChangeCheck();
                            info.SpecialExhibitedId = EditorGUILayout.IntField("入稿ID", info.SpecialExhibitedId);
                            if (EditorGUI.EndChangeCheck())
                            {
                                info.Save();
                            }
                            
                            if (GUILayout.Button( /* ログイン */AssetUtility.GetMain("Vket_ControlPanel.Login"),
                                    GUILayout.Width(controlWidth),
                                    GUILayout.Height(30)))
                            {
                                info.GuestLogin(info.SpecialExhibitedId, true).Forget();
                            }

                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space();

                        GUIStyle l3 = new GUIStyle(GUI.skin.label);
                        l3.fontSize = 11;
                        l3.fixedHeight = 16;

                        EditorGUILayout.Space();

                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            UIUtility.EditorGUILink(EventUrl, l3);
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndVertical();
                    break;
                }
                case VersionInfo.PackageType.stable:
                {
                    EditorGUILayout.BeginVertical(box);
                    {
                        EditorGUILayout.Space();

                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            GUIContent content = new GUIContent( /* VketAccountでログイン */
                                AssetUtility.GetMain("Vket_ControlPanel.LoginVketAccount"));
                            EditorGUILayout.LabelField(content, l2, GUILayout.Width(l2.CalcSize(content).x));
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        GUILayout.Space(30);

                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button( /* ログイン */AssetUtility.GetMain("Vket_ControlPanel.Login"),
                                    GUILayout.Width(controlWidth),
                                    GUILayout.Height(30)))
                            {
                                info.LoginRequest();
                            }

                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.Space();

                        GUIStyle l3 = new GUIStyle(GUI.skin.label);
                        l3.fontSize = 11;
                        l3.fixedHeight = 16;

                        EditorGUILayout.Space();

                        EditorGUILayout.BeginHorizontal();
                        {
                            GUILayout.FlexibleSpace();
                            UIUtility.EditorGUILink(EventUrl, l3);
                            GUILayout.FlexibleSpace();
                        }
                        EditorGUILayout.EndHorizontal();

                        GUILayout.FlexibleSpace();
                    }
                    EditorGUILayout.EndVertical();
                    break;
                }
            }

            GUILayout.Space(5);
        }

        /// <summary>
        /// 企業・コミュニティブース選択コンテントの描画
        /// </summary>
        /// <param name="info"></param>
        /// <param name="controlWidth"></param>
        /// <param name="world"></param>
        /// <param name="buttonHeight"></param>
        private void DrawCompanyBoothContent(LoginInfo info, float controlWidth, 
            VketApi.WorldData world, float buttonHeight = 30)
        {
            GUILayout.Space(30);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                DrawGuiCompanyBoothButton(info, controlWidth, world, buttonHeight);

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 企業ブース選択ボタンの描画
        /// </summary>
        /// <param name="info"></param>
        /// <param name="controlWidth"></param>
        /// <param name="world"></param>
        /// <param name="buttonHeight"></param>
        /// <returns>入稿を開始ボタンが押された場合にtrue</returns>
        private void DrawGuiCompanyBoothButton(LoginInfo info, float controlWidth, VketApi.WorldData world, float buttonHeight = 30)
        {
            /* "入稿を開始する" */
            var content = new GUIContent($"{AssetUtility.GetMain("Vket_ControlPanel.StartSubmissionButton")}（{world.GetWorldName(LocalizedSetting.Instance.Language)}）");
            if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, GUI.skin.button, controlWidth), GUILayout.Width(controlWidth), GUILayout.Height(buttonHeight)))
            {
                info.SelectedWorld = world;
                info.Save();
                info.GuestLogin(info.SpecialExhibitedId);
                LoginInfo.ReloadWorldDefinition();
                var userSettings = SettingUtility.GetSettings<UserSettings>();
                userSettings.validatorFolderPath = $"Assets/{GetExhibitorID()}";
                SettingUtility.SaveSettings(userSettings);
            }
        }

        #endregion //ログイン画面

        #region Auth認証画面

        /// <summary>
        /// 認可コード入力ウィンドウの描画
        /// </summary>
        private void WaitAuthWindow()
        {
            GUILayout.Space(5);

            GUIStyle box = new GUIStyle(GUI.skin.box);
            GUIStyle l1 = new GUIStyle(GUI.skin.label);
            GUIStyle l2 = new GUIStyle(GUI.skin.label);

            l1.fontSize = 25;
            l1.fixedHeight = 30;
            l2.fontSize = 15;
            l2.fixedHeight = 23;

            float controlWidth = position.width - position.width / 3;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUIContent content = new GUIContent( /* ログイン */AssetUtility.GetMain("Vket_ControlPanel.Login"));
                EditorGUILayout.LabelField(content, l1, GUILayout.Width(l1.CalcSize(content).x));
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(30);

            EditorGUILayout.BeginVertical(box);
            {
                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    GUIContent content = new GUIContent( /* 表示された認可コードを入力してください */AssetUtility.GetMain("Vket_ControlPanel.WaitAuthWindow.EnterAuthCode"));
                    EditorGUILayout.LabelField(content, l2, GUILayout.Width(l2.CalcSize(content).x));
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(30);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    LoginInfo.AuthorizationCode = EditorGUILayout.TextField(LoginInfo.AuthorizationCode, GUILayout.Width(controlWidth));
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(30);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button( /* ログイン */AssetUtility.GetMain("Vket_ControlPanel.Login"), GUILayout.Width(controlWidth),
                            GUILayout.Height(30)))
                    {
                        // Play 中なら実行不可
                        if (EditorPlayCheck() || EditorApplication.isPlayingOrWillChangePlaymode) return;
                        AssetUtility.LoginInfoData.Login().Forget();
                    }

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space();

                GUIStyle l3 = new GUIStyle(GUI.skin.label);
                l3.fontSize = 11;
                l3.fixedHeight = 16;

                EditorGUILayout.Space();

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    UIUtility.EditorGUILink(EventUrl, l3);
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(30);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button( /* 戻る */AssetUtility.GetMain("Vket_ControlPanel.WaitAuthWindow.BackButton"), GUILayout.Width(controlWidth * 0.3f),
                            GUILayout.Height(25)))
                    {
                        AssetUtility.LoginInfoData.CancelLogin();
                    }

                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();


                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        #endregion // Auth認証画面

        #region 入稿ワールド選択画面

        /// <summary>
        /// 入稿ワールド選択ウィンドウの描画
        /// </summary>
        private void SelectWindow()
        {
            GUILayout.Space(5);

            LoginInfo info = AssetUtility.LoginInfoData;
            var versionInfo = AssetUtility.VersionInfoData;

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();

            _selectWindowScrollPos = EditorGUILayout.BeginScrollView(_selectWindowScrollPos, GUI.skin.box);
            {
                switch (versionInfo.Type)
                {
                    case VersionInfo.PackageType.stable:
                    {
                        var circles = info.Circles;

                        // サークルが登録されていない場合はサークル登録確認画面の描画
                        // TODO:ownerが0の場合に登録されていない恐れがある
                        if (circles == null || circles.Length <= 0)// || circles[0].owner_id == 0)
                        {
                            GUILayout.Space(30);

                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                GUIContent content =
                                    new GUIContent(AssetUtility.GetMain("Vket_ControlPanel.SelectWindow.NotFoundCircle") /*"サークルが登録されていません。"*/);
                                GUIStyle textStyle = new GUIStyle();
                                textStyle.alignment = TextAnchor.MiddleCenter;
                                EditorGUILayout.LabelField(content, textStyle);
                                GUILayout.FlexibleSpace();
                            }
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                GUIContent content =
                                    new GUIContent(
                                        AssetUtility.GetMain("Vket_ControlPanel.SelectWindow.RequestLoginAgain") /*"マイページからサークルを登録のうえ再度ログインしてください。"*/);
                                GUIStyle textStyle = new GUIStyle();
                                textStyle.alignment = TextAnchor.MiddleCenter;
                                EditorGUILayout.LabelField(content, textStyle);
                                GUILayout.FlexibleSpace();
                            }
                            EditorGUILayout.EndHorizontal();

                            GUILayout.Space(30);

                            GUIStyle l3 = new GUIStyle(GUI.skin.label);
                            l3.fontSize = 11;
                            l3.fixedHeight = 16;

                            EditorGUILayout.BeginHorizontal();
                            {
                                GUILayout.FlexibleSpace();
                                UIUtility.EditorGUILink(CircleMypageUrl, l3);
                                GUILayout.FlexibleSpace();
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        else
                        {
                            // ワールド選択ボタンの描画
                            for (int i = 0; i < circles.Length; i++)
                            {
                                foreach (var entrySpaceWorld in info.GetEntrySpaceWorlds(circles[i]))
                                {
                                    DrawWorldSelectContent(i, entrySpaceWorld, info);
                                }
                                
                                foreach (var entryItemWorld in info.GetEntryItemWorlds(circles[i]))
                                {
                                    DrawWorldSelectContent(i, entryItemWorld, info, true);
                                }
                            }
                        }

                        break;
                    }
                    case VersionInfo.PackageType.develop:
                    {
                        foreach (var spaceWorld in info.GetAllSpaceWorlds())
                        {
                            DrawWorldSelectContent(0, spaceWorld, info);
                        }
                        
                        foreach (var itemWorld in info.GetAllItemWorlds())
                        {
                            DrawWorldSelectContent(0, itemWorld, info, true);
                        }
                        break;
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            // ログアウトボタンの表示
            EditorGUILayout.BeginHorizontal();
            {
                GUIStyle b2 = new GUIStyle(GUI.skin.button);
                b2.fontSize = 12;
                GUIContent content = new GUIContent( /* ログアウト */AssetUtility.GetMain("Vket_ControlPanel.LogOutButton"));
                if (GUILayout.Button(content, GUILayout.Width(position.width - 10.0f), GUILayout.Height(30.0f)))
                {
                    Logout();
                }
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);
        }
        
        /// <summary>
        /// ワールド選択Contentの描画
        /// </summary>
        private void DrawWorldSelectContent(int circleIndex, VketApi.WorldData world, LoginInfo info, bool isItem = false)
        {
            bool isCrossPlatform = world.IsCrossPlatform;
            
            GUIStyle itemStyle = new GUIStyle(GUI.skin.box);
            itemStyle.fixedHeight = 100.0f;
            EditorGUILayout.BeginVertical(itemStyle);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.BeginVertical();
                    {
                        GUIContent contents = new GUIContent();
                        GUIStyle style = new GUIStyle(GUIStyle.none);
                        if (!world.ThumbnailTexture)
                        {
                            world.LoadThumbnailTexture().Forget();
                        }
                        contents.image = world.ThumbnailTexture;
                        if (!contents.image)
                        {
                            contents.image = AssetUtility.GetNoImage();
                            style.fixedHeight = 95.0f;
                            style.fixedWidth = 95.0f;
                        }
                        else
                        {
                            style.fixedHeight = 256.0f;
                            style.fixedWidth = 144.0f;
                        }

                        if (GUILayout.Button(contents, style))
                        {
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.BeginVertical();
                    {
                        bool isAvatar = world.exhibition_category.Contains("avatar");
                        if (_debugMode)
                        {
                            var style = new GUIStyle(GUI.skin.label);
                            if (isItem && isCrossPlatform)
                            {
                                style.normal.textColor = Color.red;
                                /* " (CrossPlatformアイテム入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language), style);
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.CrossPlatformItem"), style);
                            }
                            else if (isItem)
                            {
                                style.normal.textColor = Color.cyan;
                                /* " (PCアイテム入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language), style);
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.PCItem"), style);
                            }
                            else if (isAvatar && isCrossPlatform)
                            {
                                /* " (CrossPlatformアバター入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language), style);
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.CrossPlatformAvatar"), style);
                            }
                            else if(isAvatar)
                            {
                                /* " (PCアバター入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language), style);
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.PCAvatar"), style);
                            }
                            else if (isCrossPlatform)
                            {
                                style.normal.textColor = Color.blue;
                                /* " (CrossPlatform入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language), style);
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.CrossPlatform"), style);
                            }
                            else
                            {
                                /* " (PC入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language), style);
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.PC"), style);
                            }
                        }
                        else
                        {
                            if (isItem && isCrossPlatform)
                            {
                                /* " (CrossPlatformアイテム入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language));
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.CrossPlatformItem"));
                            }
                            else if (isItem)
                            {
                                /* " (PCアイテム入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language));
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.PCItem"));
                            }
                            else if (isAvatar && isCrossPlatform)
                            {
                                /* " (CrossPlatformアバター入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language));
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.CrossPlatformAvatar"));
                            }
                            else if(isAvatar)
                            {
                                /* " (PCアバター入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language));
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.PCAvatar"));
                            }
                            else if (isCrossPlatform)
                            {
                                /* " (CrossPlatform入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language));
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.CrossPlatform"));
                            }
                            else
                            {
                                /* " (PC入稿)" */
                                EditorGUILayout.LabelField(world.GetWorldName(LocalizedSetting.Instance.Language));
                                EditorGUILayout.LabelField(
                                    AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.PC"));
                            }
                        }

                        GUIContent contents = new GUIContent();
                        contents.text = AssetUtility.GetMain("Vket_ControlPanel.DrawWorldSelectContent.SelectWorldButton") /* "このワールドに入稿する" */;

                        if (GUILayout.Button(contents, GUILayout.Width(position.width * 0.5f)))
                        {
                            info.SelectedCircleIndex = circleIndex;
                            info.SelectedWorld = world;
                            info.SelectedType = isItem ? LoginInfo.ExhibitType.Item : LoginInfo.ExhibitType.Space;
                            info.Save();
                            LoginInfo.ReloadWorldDefinition();
                            // サークルサムネイルの読み込み
                            info.SelectedCircle?.LoadThumbnailTexture().Forget();
                            DrawBoundsLimitGizmos(SceneManager.GetActiveScene());
                            Repaint();
                        }
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        #endregion // 入稿ワールド選択画面

        #region コントロールパネルウィンドウ

        // ControlPanelで使用するスタイル
        private GUIStyle _controlPanelLabelStyle1;
        private GUIStyle _controlPanelLabelStyle2;
        private GUIStyle _controlPanelButtonStyle1;
        private GUIStyle _controlPanelMainFiledStyle;

        // ツールボタンの大きさ
        private const float ControlPanelToolButtonHeight = 35f;

        // フッターボタンの大きさ
        private const float ControlPanelFooterButtonHeight = 30f;

        // 最小ウィンドウサイズ
        private const float ControlPanelWindowMinHeight = 620f;

        // 最小サムネイルサイズ
        private const float ControlPanelThumbnailMinSize = 50f;

        // 入稿シーン作成ボタンなどの最低横幅サイズ
        private const float ToolButtonsMinWidth = 160;

        /// <summary>
        /// 標準の空白サイズ
        /// </summary>
        private const float ControlPanelBaseSpace = 10f;

        /// <summary>
        /// コントロールパネル用スタイルの初期化
        /// </summary>
        private void InitControlPanelWindow()
        {
            if (_controlPanelLabelStyle1 != null)
                return;

            PlatformUtility.CheckAndSwitchPlatform();

            _controlPanelLabelStyle1 = new GUIStyle(GUI.skin.label);
            _controlPanelLabelStyle2 = new GUIStyle(GUI.skin.label);
            _controlPanelButtonStyle1 = new GUIStyle(GUI.skin.button);

            _controlPanelLabelStyle1.alignment = TextAnchor.MiddleLeft;
            _controlPanelLabelStyle1.fontSize = 15;
            _controlPanelLabelStyle1.fixedHeight = 25;
            _controlPanelLabelStyle1.padding = new RectOffset(0, 0, 0, 0);
            _controlPanelLabelStyle2.alignment = TextAnchor.MiddleCenter;
            _controlPanelLabelStyle2.fontSize = 11;
            _controlPanelLabelStyle2.fixedHeight = 16;
            _controlPanelLabelStyle2.padding = new RectOffset(0, 0, 0, 0);

            _controlPanelButtonStyle1.fontSize = 12;

            _controlPanelMainFiledStyle = new GUIStyle(GUI.skin.box)
            {
                margin = new RectOffset((int)EditorGUIUtility.standardVerticalSpacing, (int)EditorGUIUtility.standardVerticalSpacing, 0, 0)
            };
        }
        
        /// <summary>
        /// コントロールパネルの描画
        /// </summary>
        private void ControlPanelWindow()
        {
            InitControlPanelWindow();
            LoginInfo info = AssetUtility.LoginInfoData;
            
            // _worldDefinitionが無ければ読み込みと初期化
            if (!LoginInfo.CurrentWorldDefinition)
            {
                LoginInfo.ReloadWorldDefinition();
            }
            bool isItem = info.IsItem;
            bool isAvatar = LoginInfo.CurrentWorldDefinition && LoginInfo.CurrentWorldDefinition.IsAvatarSubmission;
            
            // 外枠更新
            if (!isItem && !isAvatar)
                UpdateOuterFrame(info);
            
            var headerHeight = GetHeaderHeight();
            var footerHeight = GetFooterHeight(info);
            var mainHeight = GetMainHeight(info);
            // ウィンドウ最小サイズの更新
            float windowHeight = headerHeight + footerHeight + mainHeight;
            bool isScrollVertical = windowHeight > ControlPanelWindowMinHeight;
            minSize = new Vector2(minSize.x, isScrollVertical ? ControlPanelWindowMinHeight : windowHeight);
            // ヘッダーの描画
            DrawHeader(info);
            
            if (isScrollVertical)
            {
                _controlPanelWindowScrollPos = EditorGUILayout.BeginScrollView(_controlPanelWindowScrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
            }

            float maxItemHeight = isItem ? 382f : 0f;

            // アイコンや情報表示部分のRect
            Rect mainFiledRect = new Rect(0f, headerHeight, position.width - EditorGUIUtility.standardVerticalSpacing * 2,
                position.height - headerHeight - footerHeight - maxItemHeight);
            
            // 幅 or 高さ-(はみ出し防止分の値->サムネイルサイズは横幅計算するため、ちょうどいい値を探す必要あり) の 短いほうを取得
            float windowMinSize = Mathf.Min(mainFiledRect.width, mainFiledRect.height - GetLoginInformationHeight() + 150f + maxItemHeight);
            float thumbnailLeftFieldSize = ToolButtonsMinWidth + ControlPanelBaseSpace * 2;

            // Windowサイズの幅の短いほうを基準にサムネイルの大きさを計算
            float thumbnailSize = Mathf.Max(windowMinSize - thumbnailLeftFieldSize, ControlPanelThumbnailMinSize);

            // 入稿シーン作成ボタンなどの幅
            // サムネと横並びになると仮定して計算
            // mainWidth - (空白 + サムネイル + 空白)
            float toolButtonWidth = mainFiledRect.width - thumbnailSize - ControlPanelBaseSpace * 2;

            // 縦長レイアウトになる場合true
            // メインフィールド全体の高さからGetToolButtonsHeight分の空白ができる場合にtrue
            float portraitLayoutThumbnailSize = mainFiledRect.width - ControlPanelBaseSpace * 2;
            bool portraitLayoutFlag = mainFiledRect.height > portraitLayoutThumbnailSize + ControlPanelBaseSpace * 2 +
                GetLoginInformationHeight() + GetToolButtonsHeight() + ControlPanelBaseSpace + EditorGUIUtility.standardVerticalSpacing * 2 + 1;

            // 縦長の場合サムネイルの横幅を最大に
            if (portraitLayoutFlag)
            {
                thumbnailSize = portraitLayoutThumbnailSize;
            }

            // メイン部分の描画(範囲デバッグ表示)
            if (_debugMode)
            {
                var debugRect = mainFiledRect;
                if (isScrollVertical)
                {
                    debugRect.y = 0;
                    debugRect.height = position.height - (headerHeight + footerHeight);
                    EditorGUI.DrawRect(debugRect, Color.blue);
                }
                else
                {
                    EditorGUI.DrawRect(debugRect, Color.blue);
                }
            }

            if (isScrollVertical)
                EditorGUILayout.BeginHorizontal();
            
            // メイン部分の描画
            EditorGUILayout.BeginVertical(_controlPanelMainFiledStyle);
            {
                // サークルアイコンの描画
                Texture2D targetTex;
                if (info.SelectedCircle != null)
                {
                    if (!_circleThumbnail)
                    {
                        _circleThumbnail = AssetDatabase.LoadAssetAtPath<Texture2D>(info.SelectedCircle.CircleThumbnailFilePath);
                    }
                    
                    targetTex = _circleThumbnail ? _circleThumbnail : AssetUtility.GetNoImage();
                }
                else
                {
                    targetTex = AssetUtility.GetNoImage();
                }
                
                float circleIconPosY = isScrollVertical ? ControlPanelBaseSpace : headerHeight + ControlPanelBaseSpace;
                EditorGUI.DrawPreviewTexture(
                    new Rect(EditorGUIUtility.standardVerticalSpacing + ControlPanelBaseSpace, circleIconPosY, thumbnailSize,
                        thumbnailSize), targetTex);
                
                // 縦配置
                if (portraitLayoutFlag)
                {
                    // サムネイル分の空白を追加
                    GUILayout.Space(thumbnailSize + ControlPanelBaseSpace * 2);
                }
                // 横配置
                else
                {
                    GUILayout.Space(EditorGUIUtility.standardVerticalSpacing + ControlPanelBaseSpace);

                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Space(thumbnailSize + ControlPanelBaseSpace);

                        // サムネの横にボタンを表示
                        DrawToolButtons(toolButtonWidth);
                    }
                    EditorGUILayout.EndHorizontal();

                    // ボタンの高さがサムネイルのサイズより小さい場合はその分だけスペースを追加
                    float addSpace = thumbnailSize + ControlPanelBaseSpace - GetToolButtonsHeight();
                    if (0 < addSpace)
                    {
                        GUILayout.Space(addSpace);
                    }
                }

                DrawLoginInformation(info, mainFiledRect.width);

                if (portraitLayoutFlag)
                {
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(ControlPanelBaseSpace / 2f);
                    DrawToolButtons(mainFiledRect.width - ControlPanelBaseSpace);
                    GUILayout.Space(ControlPanelBaseSpace / 2f);
                    EditorGUILayout.EndHorizontal();
                    // 一番下に空白
                    GUILayout.Space(ControlPanelBaseSpace);
                }
                
                if (!isItem && !isAvatar)
                {
                    DrawTemplateOuterFrame(info);
                }
                
                if (isItem)
                {
                    DrawItemSetting();
                }

                if (isAvatar)
                {
                    DrawAvatarSetting();
                }
            }
            EditorGUILayout.EndVertical();

            if (isScrollVertical)
            {
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();
            }

            // フッターは下側に配置
            GUILayout.FlexibleSpace();

            // フッターの描画
            DrawFooter(info, isItem, isAvatar);
        }

        /// <summary>
        /// Headerの高さを取得
        /// </summary>
        /// <returns>Headerの高さ</returns>
        private float GetHeaderHeight()
        {
            float retVal = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            retVal += 5f + _controlPanelLabelStyle1.fixedHeight + EditorGUIUtility.standardVerticalSpacing;
            return retVal;
        }

        /// <summary>
        /// Headerの表示
        /// サークル名とマイページボタンの描画
        /// </summary>
        private void DrawHeader(LoginInfo info)
        {
            if (_debugMode)
            {
                var headerRect = Rect.zero;
                headerRect.width = position.width;
                headerRect.height = GetHeaderHeight();
                EditorGUI.DrawRect(headerRect, Color.red);
            }

            GUILayout.Space(5);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUIContent content1 = new GUIContent($"{ /* サークル名 */AssetUtility.GetMain("Vket_ControlPanel.CircleName")}:");
                GUIContent content2;
                switch (AssetUtility.VersionInfoData.Type)
                {
                    case VersionInfo.PackageType.stable:
                    {
                        var circles = info.Circles;
                        content2 = new GUIContent(circles[info.SelectedCircleIndex].GetCircleName(LocalizedSetting.Instance.Language), circles[info.SelectedCircleIndex].GetCircleName(LocalizedSetting.Instance.Language));
                        break;
                    }
                    case VersionInfo.PackageType.company:
                    case VersionInfo.PackageType.community:
                    case VersionInfo.PackageType.develop:
                    default:
                        content2 = new GUIContent("", "");
                        break;
                }

                var content3 = new GUIContent(AssetUtility.GetMain("Vket_ControlPanel.DrawHeader.MyPage") /*"マイページ"*/);

                // サークル名が表示可能な横幅を計算
                const float space = 20;
                var width = position.width - space - _controlPanelLabelStyle1.CalcSize(content1).x -
                            _controlPanelButtonStyle1.CalcSize(content3).x;
                var style = UIUtility.GetContentSizeFitStyle(content2, _controlPanelLabelStyle1, width);

                // サークル名ラベル描画
                EditorGUILayout.LabelField(content1, _controlPanelLabelStyle1,
                    GUILayout.Width(_controlPanelLabelStyle1.CalcSize(content1).x),
                    GUILayout.Height(_controlPanelLabelStyle1.CalcSize(content1).y));

                // サークル名の描画
                EditorGUILayout.LabelField(content2, style, GUILayout.Width(style.CalcSize(content2).x),
                    GUILayout.Height(style.CalcSize(content2).y));
                GUILayout.FlexibleSpace();

                // マイページボタンの描画
                if (GUILayout.Button(content3, _controlPanelButtonStyle1,
                        GUILayout.Width(_controlPanelButtonStyle1.CalcSize(content3).x),
                        GUILayout.Height(_controlPanelButtonStyle1.CalcSize(content3).y)))
                {
                    switch (AssetUtility.VersionInfoData.Type)
                    {
                        case VersionInfo.PackageType.stable:
                            Application.OpenURL(CircleMypageUrl);
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// メイン部分の高さを取得(横配置)
        /// </summary>
        /// <param name="info">LoginInfo</param>
        /// <returns>横配置想定のメイン部分の高さを取得</returns>
        private float GetMainHeight(LoginInfo info)
        {
            return ControlPanelBaseSpace + EditorGUIUtility.standardVerticalSpacing
                                         + GetToolButtonsHeight()
                                         + GetLoginInformationHeight()
                                         + GetTemplateOuterFrameHeight(info)
                                         + GetItemSettingHeight(info)
                                         + GetAvatarSettingHeight() + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <summary>
        /// フッターの高さを取得
        /// </summary>
        /// <param name="info">LoginInfo</param>
        /// <returns>フッターの高さ</returns>
        private static float GetFooterHeight(LoginInfo info)
        {
            bool isItem = false;
            bool isAvatar = LoginInfo.CurrentWorldDefinition && LoginInfo.CurrentWorldDefinition.IsAvatarSubmission;
            float diff = 0;
            bool isOwner = true;
            if (info)
            {
                isItem = info.IsItem;
                if (isItem && !IsExistItemInfo())
                {
                    // "アイテム入稿に必要な設定ファイル[Assets/{0}/Config/VketItemInfo.asset]が見つかりません。\n入稿フォルダを削除し、入稿用シーン作成ボタンを押してシーンを再生成してください。"
                    diff = UIUtility.GetHelpBoxHeight(AssetUtility.GetMain("Vket_ControlPanel.DrawFooter.NotFoundItemInfo",
                        GetExhibitorID()));
                }
                if (AssetUtility.VersionInfoData.Type == VersionInfo.PackageType.stable)
                {
                    isOwner = info.IsOwner;
                }
            }
            
            int footerLineCount = 3;
            if (isItem || isAvatar) footerLineCount--;
            if (isOwner)
            {
                // 入稿ボタン分のサイズ加算
                footerLineCount+=3;
                diff += 20;
            }
            return (ControlPanelFooterButtonHeight + EditorGUIUtility.standardVerticalSpacing) * footerLineCount + EditorGUIUtility.standardVerticalSpacing + diff;
        }

        /// <summary>
        /// 規約同意トグル
        /// </summary>
        private bool _agreeToggle;
        private GUIStyle _draftBoxStyle;
        private GUIStyle _toggleStyle;
        
        /// <summary>
        /// Footerの表示
        /// </summary>
        /// <param name="info">LoginInfo</param>
        private void DrawFooter(LoginInfo info, bool isItem, bool isAvatar)
        {
            if (_debugMode)
            {
                var footerRect = Rect.zero;

                footerRect.y = position.height - GetFooterHeight(info);
                footerRect.width = position.width;
                footerRect.height = GetFooterHeight(info);

                EditorGUI.DrawRect(footerRect, Color.green);
            }
            
            using (new EditorGUILayout.VerticalScope())
            {
                GUIContent content;

                // ボタンを横配置
                using (new EditorGUILayout.HorizontalScope())
                {
                    int horizontalButtonCount = 3;
                    float buttonWidth = position.width / horizontalButtonCount - 4f;

                    switch (AssetUtility.VersionInfoData.Type)
                    {
                        case VersionInfo.PackageType.stable:
                        case VersionInfo.PackageType.develop:
                        {
                            content = new GUIContent(AssetUtility.GetMain("Vket_ControlPanel.DrawFooter.BackWorldSelectButton") /* "ワールド選択に戻る"*/);
                            if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth), GUILayout.Width(buttonWidth), GUILayout.Height(ControlPanelFooterButtonHeight)))
                            {
                                BackWorldSelectWindow(info);
                                _circleThumbnail = null;
                            }

                            break;
                        }
                        default:
                        {
                            horizontalButtonCount = 2;
                            buttonWidth = position.width / horizontalButtonCount - 4f;
                            break;
                        }
                    }

                    // ログアウトボタンの表示
                    content = new GUIContent( /* ログアウト */AssetUtility.GetMain("Vket_ControlPanel.LogOutButton"));
                    if (GUILayout.Button(content,
                            UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth),
                            GUILayout.Width(buttonWidth), GUILayout.Height(ControlPanelFooterButtonHeight)))
                    {
                        Logout();
                    }

                    // パッケージボタンの表示
                    content = new GUIContent("Packages");
                    if (GUILayout.Button(content,
                            UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth),
                            GUILayout.Width(buttonWidth), GUILayout.Height(ControlPanelFooterButtonHeight)))
                    {
                        PopupWindow.Show(new Rect(Event.current.mousePosition, Vector2.one),
                            new VketPackagePopup());
                    }
                }

                if (!isItem && !isAvatar)
                {
                    content = new GUIContent(AssetUtility.GetMain("Vket_ControlPanel.VketPrefabsButton"));
                    if (GUILayout.Button(content,
                            UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, position.width - 6f),
                            GUILayout.Width(position.width - 6f), GUILayout.Height(ControlPanelFooterButtonHeight)))
                    {
                        VketPrefabsMainWindow.OpenMainWindow(GetExhibitorID());
                    }
                }

                switch (AssetUtility.VersionInfoData.Type)
                {
                    case VersionInfo.PackageType.company:
                    case VersionInfo.PackageType.community:
                    {
                        EditorGUI.BeginDisabledGroup(CircleNullOrEmptyCheck());
                        {
                            if (GUILayout.Button( /* 入稿ファイルの書き出し */AssetUtility.GetMain("Vket_ControlPanel.DrawFooter.ExportButton"),
                                    _controlPanelButtonStyle1,
                                    GUILayout.Height(ControlPanelFooterButtonHeight)))
                            {
                                ExportBoothPackage($"Assets/{GetExhibitorID()}", $"Vket_{AssetUtility.VersionInfoData.Type.ToString()}_BoothData_{GetExhibitorID()}.unitypackage", true);
                            }
                        }
                        EditorGUI.EndDisabledGroup();
                        break;
                    }
                    // 入稿ボタンの表示
                    default:
                    {
                        bool disabledDraftButton = CircleNullOrEmptyCheck();
                        bool itemCheck = false;
                        if (info.IsItem && !_itemInfo)
                        {
                            disabledDraftButton = true;
                            itemCheck = true;
                        }
                        
                        if(!disabledDraftButton)
                        {
                            EditorGUI.BeginDisabledGroup(disabledDraftButton);
                            {
                                bool isOwner = true;
                                if (AssetUtility.VersionInfoData.Type == VersionInfo.PackageType.stable)
                                {
                                    isOwner = info.IsOwner;   
                                }
                                
                                // オーナーの場合は入稿ボタンの表示
                                if (isOwner)
                                {
                                    EditorGUILayout.Space(5);
                                    DrawDraftButton();
                                    EditorGUILayout.Space(5);
                                }
                                
                                if (GUILayout.Button( /* 入稿ファイルの書き出し */AssetUtility.GetMain("Vket_ControlPanel.DrawFooter.ExportButton"),
                                        _controlPanelButtonStyle1,
                                        GUILayout.Height(ControlPanelFooterButtonHeight)))
                                {
                                    ExportBoothPackage($"Assets/{GetExhibitorID()}", $"Vket_{AssetUtility.VersionInfoData.Type.ToString()}_BoothData_{GetExhibitorID()}.unitypackage", true);
                                }
                            }
                            EditorGUI.EndDisabledGroup();
                        }
                        else
                        {
                            EditorGUILayout.Space(EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight * 2);
                        }

                        if (itemCheck)
                        {
                            // "アイテム入稿に必要な設定ファイル[Assets/{0}/Config/VketItemInfo.asset]が見つかりません。\n入稿フォルダを削除し、入稿用シーン作成ボタンを押してシーンを再生成してください。"
                            EditorGUILayout.HelpBox(AssetUtility.GetMain("Vket_ControlPanel.DrawFooter.NotFoundItemInfo", GetExhibitorID()), MessageType.Error);
                        }

                        break;
                    }
                }
            }
            
            void DrawDraftButton()
            {
                if (_toggleStyle == null)
                {
                    _toggleStyle = new GUIStyle(GUI.skin.toggle)
                    {
                        fontStyle = FontStyle.Bold,
                        padding = { left = 20 },
                        margin = { left = 10 },
                        wordWrap = true
                    };
                    _toggleStyle.normal.textColor  = Color.black;
                    _toggleStyle.focused.textColor = Color.black;
                    _toggleStyle.hover.textColor = Color.black;
                    _toggleStyle.active.textColor = Color.black;
                    _toggleStyle.onNormal.textColor  = Color.black;
                    _toggleStyle.onFocused.textColor = Color.black;
                    _toggleStyle.onHover.textColor = Color.black;
                    _toggleStyle.onActive.textColor = Color.black;
                }
                
                _draftBoxStyle ??= new GUIStyle(EditorStyles.helpBox)
                {
                    normal = new GUIStyleState
                    {
                        background = UIUtility.CreateColorTexture(4, 4, new Color(1f, 1f, 125f/255))
                    }
                };
                _draftBoxStyle.normal.background ??= UIUtility.CreateColorTexture(4, 4, new Color(1f, 1f, 125f / 255));
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    var rect = GUILayoutUtility.GetRect(0, (ControlPanelFooterButtonHeight + EditorGUIUtility.standardVerticalSpacing) * 3 + 10);
                    rect.x += EditorGUIUtility.standardVerticalSpacing;
                    rect.size -= new Vector2(EditorGUIUtility.standardVerticalSpacing * 2, 0);
                    GUI.Box(rect, "", _draftBoxStyle);
                    rect.x += 8;
                    rect.width -= 16;
                    
                    float maxWidth = position.width - 40;
                    var toggleText = /* "出展規約に同意し、入稿物は流血表現・センシティブな表現などの公序良俗に反したものではないことを保証します。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawDraftButton.Message");
                    rect.height -= ControlPanelFooterButtonHeight;
                    _toggleStyle.fontSize = UIUtility.GetOptimalFontSize(toggleText, _toggleStyle, maxWidth, rect.height - 10);
                    _agreeToggle = GUI.Toggle(rect, _agreeToggle, toggleText, _toggleStyle);
                    
                    rect.y += (ControlPanelFooterButtonHeight + EditorGUIUtility.standardVerticalSpacing) * 2 + 10;
                    rect.height = ControlPanelFooterButtonHeight;
                    EditorGUI.BeginDisabledGroup(!_agreeToggle);
                    if (GUI.Button(rect, /* 入稿 */AssetUtility.GetMain("Vket_ControlPanel.SubmissionButton"), _controlPanelButtonStyle1))
                    {
                        DraftButton_Click();
                    }
                    EditorGUI.EndDisabledGroup();
                }
            }
        }

        private static readonly List<string> LanguagePopupDisplayOptions = new()
        {
            SystemLanguage.Japanese.ToString(),
            SystemLanguage.English.ToString(),
        };
        
        private void DrawLanguageSettings()
        {
            var language = LocalizedSetting.Instance.Language;
            var languageIndex = EditorGUILayout.Popup(new GUIContent("Language:"), LanguagePopupDisplayOptions.IndexOf(language.ToString()), LanguagePopupDisplayOptions.Select(l => new GUIContent(l)).ToArray());
            var currentLanguage = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), LanguagePopupDisplayOptions[languageIndex]);
            if (language != currentLanguage)
            {
                LocalizedSetting.Instance.SetLanguage(currentLanguage);
                VitDeck.Language.LocalizedMessage.CurrentLanguage = currentLanguage;
            }
        }
        
        /// <summary>
        /// ツールボタン群の高さを取得
        /// </summary>
        /// <returns>ツールボタン群の高さ</returns>
        private static float GetToolButtonsHeight()
        {
            // ボタンの高さ * ボタンの数
            return (ControlPanelToolButtonHeight + EditorGUIUtility.standardVerticalSpacing) * 6f + EditorGUIUtility.standardVerticalSpacing;
        }

        /// <summary>
        /// ツールボタン群の表示
        /// </summary>
        /// <param name="buttonWidth">ボタンの横幅</param>
        private void DrawToolButtons(float buttonWidth)
        {
            EditorGUILayout.BeginVertical();
            {
                GUIContent content = new GUIContent( /* 入稿用シーン作成 */AssetUtility.GetMain("Vket_ControlPanel.CreateSubmissionSceneButton"));
                if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth),
                        GUILayout.Height(ControlPanelToolButtonHeight)))
                {
                    LoadTemplateButton_Click();
                }

                content = new GUIContent( /* 入稿用シーンを開く */AssetUtility.GetMain("Vket_ControlPanel.DrawToolButtons.OpenSubmissionSceneButton"));
                if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth),
                        GUILayout.Height(ControlPanelToolButtonHeight)))
                {
                    OpenSubmissionSceneButton_Click();
                }

                content = new GUIContent( /* ブースチェック */AssetUtility.GetMain("Vket_ControlPanel.BoothCheckButton"));
                if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth),
                        GUILayout.Height(ControlPanelToolButtonHeight)))
                {
                    BoothCheckButton_Click();
                }

                content = new GUIContent( /* 容量チェック */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckButton"));
                if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth),
                        GUILayout.Height(ControlPanelToolButtonHeight)))
                {
                    BuildSizeCheckButton_Click();
                }

                content = new GUIContent( /* SetPassチェック */AssetUtility.GetMain("Vket_ControlPanel.SetPassCheckButton"));
                if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth),
                        GUILayout.Height(ControlPanelToolButtonHeight)))
                {
                    SetPassCheckButton_Click();
                }

                content = new GUIContent( /* VRChat 動作確認 */AssetUtility.GetMain("Vket_ControlPanel.VRCCheckButton"));
                if (GUILayout.Button(content, UIUtility.GetContentSizeFitStyle(content, _controlPanelButtonStyle1, buttonWidth),
                        GUILayout.Height(ControlPanelToolButtonHeight)))
                {
                    VRCCheckButton_Click();
                }
            }
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// 入稿情報の表示
        /// </summary>
        /// <returns></returns>
        private float GetLoginInformationHeight()
        {
            float retVal = 0f;

            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.company:
                case VersionInfo.PackageType.community:
                    break;
                default:
                    // 名前
                    retVal += _controlPanelLabelStyle1.fixedHeight + EditorGUIUtility.standardVerticalSpacing;
                    break;
            }

            if (CircleNullOrEmptyCheck())
            {
                retVal += EditorGUIUtility.standardVerticalSpacing;
                return retVal;
            }

            // 配置ワールド名
            retVal += _controlPanelLabelStyle1.fixedHeight + EditorGUIUtility.standardVerticalSpacing;

            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.company:
                case VersionInfo.PackageType.community:
                    break;
                default:
                    // 入稿方法・入稿ルールボタン
                    retVal += 20f + EditorGUIUtility.standardVerticalSpacing;
                    break;
            }

            // イベント名
            retVal += _controlPanelLabelStyle1.fixedHeight + EditorGUIUtility.standardVerticalSpacing;

            //入稿期限
            retVal += _controlPanelLabelStyle2.fixedHeight + EditorGUIUtility.standardVerticalSpacing;

            // 空白
            retVal += 5f;

            // 日付, 日付
            retVal += (_controlPanelLabelStyle2.fixedHeight + EditorGUIUtility.standardVerticalSpacing) * 2f;

            // bottomSpace
            retVal += 10f;

            retVal += EditorGUIUtility.standardVerticalSpacing;
            return retVal;
        }

        private void DrawLoginInformation(LoginInfo info, float mainFiledWidth, float cutWidth = 10)
        {
            GUIContent content;

            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.company:
                case VersionInfo.PackageType.community:
                    break;
                default:
                {
                    // ユーザー名
                    string userName = "";
                    var user = info.User;
                    if (user != null)
                    {
                        userName = LocalizedSetting.Instance.Language == 
                                   SystemLanguage.Japanese ? user.name_ja : user.name_en;
                    }

                    content = new GUIContent($"{ /* 名前 */AssetUtility.GetMain("Vket_ControlPanel.UserName")}:{userName}", $"{userName}");
                    EditorGUILayout.LabelField(content, _controlPanelLabelStyle1, GUILayout.Height(_controlPanelLabelStyle1.fixedHeight));
                }
                    break;
            }

            // サークル情報
            if (!CircleNullOrEmptyCheck())
            {
                content = new GUIContent(
                    $"{ /* 配置ワールド */AssetUtility.GetMain("Vket_ControlPanel.PlacementWorld")} : {info.SelectedWorld.GetWorldName(LocalizedSetting.Instance.Language)}",
                    info.SelectedWorld.GetWorldName(LocalizedSetting.Instance.Language));

                // 配置ワールド名の表示
                EditorGUILayout.LabelField(content,
                    UIUtility.GetContentSizeFitStyle(content, _controlPanelLabelStyle1, position.width - cutWidth),
                    GUILayout.Height(_controlPanelLabelStyle1.fixedHeight));

                EditorGUILayout.BeginHorizontal();
                {
                    var content1 = new GUIContent( /* 入稿方法 */AssetUtility.GetMain("Vket_ControlPanel.SubmissionMethod"));
                    var content2 = new GUIContent( /* 入稿ルール */AssetUtility.GetMain("Vket_ControlPanel.SubmissionRule"));

                    // 右寄せ
                    GUILayout.FlexibleSpace();

                    switch (AssetUtility.VersionInfoData.Type)
                    {
                        case VersionInfo.PackageType.company:
                        case VersionInfo.PackageType.community:
                            break;
                        default:
                        {
                            if (GUILayout.Button(content1, _controlPanelButtonStyle1,
                                    GUILayout.Width(_controlPanelButtonStyle1.CalcSize(content2).x),
                                    GUILayout.Height(20f)))
                            {
                                var mouseRect = new Rect(Event.current.mousePosition, Vector2.one);
                                mouseRect.x = 0;

                                // Popupを開く
                                PopupWindow.Show(mouseRect,
                                    new MessagePopupWindowContent(AssetUtility.GetMain("SubmissionMethodPopup.Message")));
                            }

                            if (GUILayout.Button(content2, _controlPanelButtonStyle1, GUILayout.Width(_controlPanelButtonStyle1.CalcSize(content2).x),
                                    GUILayout.Height(20f)))
                            {
                                Application.OpenURL(GetRuleURL());
                            }
                        }
                            break;
                    }
                }
                EditorGUILayout.EndHorizontal();

                // イベント名
                content = new GUIContent(AssetUtility.VersionInfoData.EventName);
                EditorGUILayout.LabelField(content, _controlPanelLabelStyle1, GUILayout.Width(_controlPanelLabelStyle1.CalcSize(content).x),
                    GUILayout.Height(_controlPanelLabelStyle1.fixedHeight));

                // 入稿期限
                content = new GUIContent($"{ /* 入稿期限(JST) */AssetUtility.GetMain("Vket_ControlPanel.DrawLoginInformation.SubmissionDeadline")}:");
                EditorGUILayout.LabelField(content, _controlPanelLabelStyle2, GUILayout.Width(_controlPanelLabelStyle2.CalcSize(content).x),
                    GUILayout.Height(_controlPanelLabelStyle2.fixedHeight));

                GUILayout.Space(5);

                var startAt = string.Empty;
                var endAtDisplay = string.Empty;
                
                if (info.SubmissionTerm != null)
                {
                    startAt = GetJpTime(info.SubmissionTerm.SubmissionStartAt);
                    endAtDisplay = GetJpTime(info.SubmissionTerm.SubmissionEndAtDisplay);
                }
                
                string GetJpTime(DateTime timeUtc)
                {
                    try
                    {
                        TimeZoneInfo cstZone = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                        DateTime cstTime = TimeZoneInfo.ConvertTimeFromUtc(timeUtc, cstZone);
                        Console.WriteLine("The date and time are {0} {1}.",
                            cstTime,
                            cstZone.IsDaylightSavingTime(cstTime) ? cstZone.DaylightName : cstZone.StandardName);
                        return cstTime.ToString("yyyy/MM/dd HH:mm");
                    }
                    catch (TimeZoneNotFoundException)
                    {
                        Debug.LogError("The registry does not define the Central Standard Time zone.");
                    }
                    catch (InvalidTimeZoneException)
                    {
                        Debug.LogError("Registry data on the Central Standard Time zone has been corrupted.");
                    }
                    return string.Empty;
                }
                
                var tooltip = $"{startAt} - {endAtDisplay}";
                content = new GUIContent($"{startAt}-,", tooltip);
                EditorGUILayout.LabelField(content, _controlPanelLabelStyle2, GUILayout.Width(_controlPanelLabelStyle2.CalcSize(content).x),
                    GUILayout.Height(_controlPanelLabelStyle2.fixedHeight));
                content = new GUIContent($"{endAtDisplay}  ", tooltip);
                EditorGUILayout.LabelField(content, _controlPanelLabelStyle2, GUILayout.Width(_controlPanelLabelStyle2.CalcSize(content).x),
                    GUILayout.Height(_controlPanelLabelStyle2.fixedHeight));

                EditorGUILayout.Space(10);
            }
        }

        private static OuterFrameInfo.OuterFrameData[] _outerFrameDatas;

        private static float GetTemplateOuterFrameHeight(LoginInfo info)
        {
            _outerFrameDatas ??= LoadOuterFrameInfos(info.SelectedWorld.world_id);
            
            if(!_outerFrameDatas.Any())
                return 0;
            
            return 5 + EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight
                   + UIUtility.GetHelpBoxHeight(/* スペース出展の外枠テンプレートを表示します。※Tools上の表示のみで反映はされません。運営により設定された外枠が使用されます。必ず下見ワールドを確認してください。*/ AssetUtility.GetMain("Vket_ControlPanel.DrawTemplateOuterFrame.Help"));
        }
        
        private static void DrawTemplateOuterFrame(LoginInfo info)
        {
            _outerFrameDatas ??= LoadOuterFrameInfos(info.SelectedWorld.world_id);
            
            if(!_outerFrameDatas.Any())
                return;
            
            EditorGUILayout.Space(5);
            EditorGUI.BeginChangeCheck();
            var selected = EditorGUILayout.Popup(/* "外枠の設定" */ AssetUtility.GetMain("Vket_ControlPanel.DrawTemplateOuterFrame.Popup"), info.SelectedOuterFrameTemplate, _outerFrameDatas.Select(d => d.Name).ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                info.SelectedOuterFrameTemplate = selected;
                EditorUtility.SetDirty(info);
                UpdateOuterFrame(info);
            }
            EditorGUILayout.HelpBox(/* スペース出展の外枠テンプレートを表示します。※Tools上の表示のみで反映はされません。運営により設定された外枠が使用されます。必ず下見ワールドを確認してください。*/ AssetUtility.GetMain("Vket_ControlPanel.DrawTemplateOuterFrame.Help"), MessageType.Info);
        }

        private static int _currentOuterFrame;
        
        private static void UpdateOuterFrame(LoginInfo info)
        {
            var outerFrame = GameObject.Find("ReferenceObjects/OuterFrame")?.transform;
            if(!outerFrame)
                return;
            
            if(_currentOuterFrame == info.SelectedOuterFrameTemplate)
                return;
            
            _outerFrameDatas ??= LoadOuterFrameInfos(info.SelectedWorld.world_id);
            if(!_outerFrameDatas.Any())
                return;
            
            _currentOuterFrame = info.SelectedOuterFrameTemplate;
            
            if (0 < _currentOuterFrame)
            {
                if (0 < outerFrame.childCount && outerFrame.GetChild(0).name != _outerFrameDatas[_currentOuterFrame].Name)
                {
                    DestroyImmediate(outerFrame.GetChild(0).gameObject);
                }

                if (0 == outerFrame.childCount)
                {
                    _outerFrameDatas[_currentOuterFrame].Instantiate(outerFrame);
                }
            }
            else if(0 < outerFrame.childCount)
            {
                DestroyImmediate(outerFrame.GetChild(0).gameObject);
            }
        }

        private static OuterFrameInfo.OuterFrameData[] LoadOuterFrameInfos(int worldId)
        {
            var infos = AssetDatabase.FindAssets($"t:{nameof(OuterFrameInfo)}", new[] { "Assets/VketTools/Templates/OuterFrames" })
                                     .Select(guid => AssetDatabase.LoadAssetAtPath<OuterFrameInfo>(AssetDatabase.GUIDToAssetPath(guid)))
                                     .Where(info => info.TargetWorldIds != null && info.TargetWorldIds.Contains(worldId))
                                     .SelectMany(info => info.OuterFlames).ToArray();
            if (!infos.Any())
                return infos;
            
            OuterFrameInfo.OuterFrameData[] noneData = { new() { Name = "None", Prefab = null } };
            return noneData.Union(infos).ToArray();
        }

        private static VketItemInfo _itemInfo;
        private static float GetItemSettingHeight(LoginInfo info)
        {
            if (info.IsItem)
            {
                if (!_itemInfo)
                {
                    // ここを通るときにはログインしている
                    _itemInfo = LoadItemInfo();
                }
                
                float labelHeight = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
                // ラベル, 空白, アイテム設定フィールド
                labelHeight += + 5f + 50f
                       + UIUtility.GetHelpBoxHeight( /* "シーンの「サークルIDオブジェクト」以下に自動で配置されます。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.DragDrop.Help"))
                       + 5f
                       + labelHeight
                       + UIUtility.GetHelpBoxHeight( /* "Default:見た目のみを表示するアイテムです。\nPickup:アイテムが掴めるようになります。\nAvatar:アバターペデスタル(Blue Print ID)を設定可能です。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.SelectItemType.Help"))
                       + labelHeight
                       + 5f
                       + labelHeight * 3
                       + 5f
                       + labelHeight * 4
                       + UIUtility.GetHelpBoxHeight( /* "キャプションボードの商品名と販売価格を設定可能です。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.BordSetting.Help"))
                       + labelHeight * 2
                       + 4f;

                if (_itemInfo)
                {
                    bool validate = VketItemInfo.ValidateUrl(_itemInfo.Url);
                    
                    labelHeight += +UIUtility.GetHelpBoxHeight( /* "Urlを入力することで、リンク先をブラウザ表示することが可能です。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.VketURLOpener.Help"));
                    if (!string.IsNullOrEmpty(_itemInfo.Url) && !validate)
                    {
                        labelHeight += +UIUtility.GetHelpBoxHeight($"{AssetUtility.GetMain("VketItemInfo.VaridateUrl.HelpBox")}\nhttps://booth.pm/\nhttps://gumroad.com/\nhttps://jinxxy.com/\nhttps://payhip.com/", margineWidth:0f);
                    }
                    else
                    {
                        labelHeight += +UIUtility.GetHelpBoxHeight( /* "Urlを入力することで、リンク先をブラウザ表示することが可能です。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.VketURLOpener.Help"));
                    }
                }
                
                return labelHeight;
            }

            return 0f;
        }

        private void DrawItemSetting()
        {
            if (!_itemInfo)
            {
                // ここを通るときにはログインしている
                _itemInfo = LoadItemInfo();
            }

            if (_itemInfo)
            {
                // アイテム設定ラベル描画
                var content = new GUIContent( /* "アイテム設定" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.Title"));
                EditorGUILayout.LabelField(content, _controlPanelLabelStyle1,
                    GUILayout.Width(_controlPanelLabelStyle1.CalcSize(content).x),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));

                EditorGUILayout.Space(5f);

                var asset = DragAndDropAreaUtility.GetObject<GameObject>( /* "入稿するオブジェクトをここにドラッグ&ドロップで追加してください。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.DragDrop.FiledMessage"), Color.cyan, Color.black);
                if (asset != null)
                {
                    // Debug.Log($"{asset.name}を取得");
                    // 配置
                    // シーン上に存在しない場合は複製
                    if (asset.scene.name == null)
                    {
                        var copy = Instantiate(asset);
                        copy.name = asset.name;
                        asset = copy;
                        Undo.RegisterCreatedObjectUndo(asset, "Create Prefab");
                    }

                    // Rootの子として設定
                    asset.transform.parent = GetSceneNameObject();
                    asset.transform.localPosition = Vector3.zero;
                    asset.transform.localRotation = Quaternion.identity;
                }

                EditorGUILayout.HelpBox( /* "シーンの「サークルIDオブジェクト」以下に自動で配置されます。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.DragDrop.Help"), MessageType.Info);

                EditorGUILayout.Space(5f);

                EditorGUI.BeginChangeCheck();
                var typeIndex = EditorGUILayout.Popup( /* "アイテムの種類を選択" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.SelectItemType.Title"), (int)_itemInfo.SelectType, new[]
                {
                    "Default", "Pickup", "Avatar"
                });
                if (EditorGUI.EndChangeCheck())
                {
                    _itemInfo.SelectType = (VketItemInfo.ItemType)typeIndex;
                    EditorUtility.SetDirty(_itemInfo);
                    AssetDatabase.SaveAssets();

                    UpdateItemType();
                }

                EditorGUILayout.HelpBox( /* "Default:見た目のみを表示するアイテムです。\nPickup:アイテムが掴めるようになります。\nAvatar:アバターペデスタル(Blue Print ID)を設定可能です。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.SelectItemType.Help"), MessageType.Info);

                EditorGUI.BeginDisabledGroup(typeIndex != 2);
                EditorGUI.BeginChangeCheck();
                _itemInfo.BlueprintID = EditorGUILayout.TextField("Avatar Blue Print ID", _itemInfo.BlueprintID);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_itemInfo);
                    AssetDatabase.SaveAssets();
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space(5f);

                EditorGUILayout.LabelField(AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.SelectPedestal"));
                string[] enum_name =
                {
                    /* "基本台座" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.PedestalName1"), /* "小物用台座" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.PedestalName2")
                };
                GUIStyle style_radio = new GUIStyle(EditorStyles.radioButton);

                EditorGUI.BeginChangeCheck();
                var select = GUILayout.SelectionGrid(_itemInfo.SelectTemplateIndex, enum_name, 1, style_radio);
                if (EditorGUI.EndChangeCheck())
                {
                    _itemInfo.SelectTemplateIndex = select;
                    EditorUtility.SetDirty(_itemInfo);
                    AssetDatabase.SaveAssets();
                    var basic = GameObject.Find("ReferenceObjects/Pedestals/Basic");
                    var smallItems = GameObject.Find("ReferenceObjects/Pedestals/SmallItems");
                    if (basic && smallItems)
                    {
                        basic.SetActive(select == 0);
                        smallItems.SetActive(select == 1);
                    }
                }

                EditorGUILayout.Space(5f);
                EditorGUILayout.LabelField( /* "キャプションボードの設定" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.BordSetting.Title"));

                EditorGUI.BeginChangeCheck();
                _itemInfo.ItemName = EditorGUILayout.TextField( /* "商品名" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.BordSetting.ItemName"), _itemInfo.ItemName);

                _itemInfo.Price = EditorGUILayout.TextField( /* "販売価格" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.BordSetting.ItemPrice"), _itemInfo.Price);

                _itemInfo.Thumbnail = EditorGUILayout.ObjectField( /* "サムネイル" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.BordSetting.Thumbnail"), _itemInfo.Thumbnail, typeof(Texture2D), false, GUILayout.Height(EditorGUIUtility.singleLineHeight)) as Texture2D;
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateItemThumbnailMaterial();
                    EditorUtility.SetDirty(_itemInfo);
                    AssetDatabase.SaveAssets();
                }

                EditorGUILayout.HelpBox( /* "キャプションボードの商品名と販売価格を設定可能です。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.BordSetting.Help"), MessageType.Info);
                
                EditorGUI.BeginChangeCheck();
                _itemInfo.Url = EditorGUILayout.TextField(/* "商品ページのUrl" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.VketURLOpener.Url"), _itemInfo.Url);
                if (EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(_itemInfo);
                    AssetDatabase.SaveAssets();
                }
                
                bool validate = VketItemInfo.ValidateUrl(_itemInfo.Url);
                EditorGUI.BeginDisabledGroup(!validate);
                if (GUILayout.Button(/* "Open URL" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.VketURLOpener.OpenButton")))
                {
                    Application.OpenURL(_itemInfo.Url);
                }
                EditorGUI.EndDisabledGroup();

                if (!string.IsNullOrEmpty(_itemInfo.Url) && !validate)
                {
                    EditorGUILayout.HelpBox($"{AssetUtility.GetMain("VketItemInfo.VaridateUrl.HelpBox")}\nhttps://booth.pm/\nhttps://gumroad.com/\nhttps://jinxxy.com/\nhttps://payhip.com/", MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox(/* "Urlを入力することで、リンク先をブラウザ表示することが可能です。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.VketURLOpener.Help"), MessageType.Info);
                }
                
                // 線
                GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
            }
            else
            {
                EditorGUILayout.HelpBox( /* "アイテムの入稿用シーンが見つかりません。\n入稿用シーンを作成してください。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawItemSetting.NotFoundScene.Help"), MessageType.Error);
            }
        }

        private static void UpdateItemThumbnailMaterial()
        {
            var itemInfo = LoadItemInfo();
            if(!itemInfo)
                return;
            
            var quad = GameObject.Find("ReferenceObjects/Pedestals/Bord/ThumbnailQuad");
            if (quad)
            {
                var quadRenderer = quad.GetComponent<MeshRenderer>();
                if (quadRenderer)
                {
                    if (!quadRenderer.sharedMaterial)
                    {
                        var originMaterialAssetPath = "Assets/VketTools/Templates/VitDeckTemplates/04_Standard_Item/TemplateAssets/{CIRCLEID}/Materials/ThumbnailQuadMaterial.mat";
                        var destDirectoryMaterialPath = $"Assets/{GetExhibitorID()}/Materials";
                        string destMaterialAssetPath = $"{destDirectoryMaterialPath}/ThumbnailQuadMaterial.mat";
                        if (!Directory.Exists(destDirectoryMaterialPath))
                        {
                            Directory.CreateDirectory(destDirectoryMaterialPath);
                        }

                        AssetDatabase.CopyAsset(originMaterialAssetPath, destMaterialAssetPath);
                        quadRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(destMaterialAssetPath);
                    }

                    quadRenderer.sharedMaterial.mainTexture = itemInfo.Thumbnail;
                }
            }
        }

        private VketAvatarInfo _avatarInfo;
        private ReorderableList _avatarDataReorderableList;

        private float GetAvatarSettingHeight()
        {
            if (!LoginInfo.CurrentWorldDefinition || !LoginInfo.CurrentWorldDefinition.IsAvatarSubmission)
                return 0;
            
            return 2 + EditorGUIUtility.singleLineHeight * 5 + (_avatarDataReorderableList?.GetHeight() ?? 0)
                   + UIUtility.GetHelpBoxHeight(/* "アバターペデスタルに登録されたアバターが開催期間になってもクロスプラットフォームに対応していない場合はアバターペデスタルの撤去対応を行います。" */
                       AssetUtility.GetMain("Vket_ControlPanel.DrawAvatarSetting.Caution"))
                   + UIUtility.GetHelpBoxHeight(/* "最大3つまでアバターボードを設定できます。\n右下の[+]と[-]ボタンでボードの数を切り替えることが可能です。" */
                       AssetUtility.GetMain("Vket_ControlPanel.DrawAvatarSetting.BordInfoHelpBox"));
        }

        private void DrawAvatarSetting()
        {
            _avatarInfo ??= LoadAvatarInfo();
            if (_avatarInfo)
            {
                var content = new GUIContent( /* "アバター設定" */ AssetUtility.GetMain("Vket_ControlPanel.DrawAvatarSetting.Title"));
                EditorGUILayout.LabelField(content, _controlPanelLabelStyle1,
                    GUILayout.Width(_controlPanelLabelStyle1.CalcSize(content).x),
                    GUILayout.Height(EditorGUIUtility.singleLineHeight));

                EditorGUILayout.Space(5f);
                
                var avatarInfoSo = new SerializedObject(_avatarInfo);
                avatarInfoSo.Update();
                _avatarDataReorderableList ??= LoadAvatarDataReorderableList(avatarInfoSo);
                var tempWidth = EditorGUIUtility.labelWidth;

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(avatarInfoSo.FindProperty("UsePedestal"), new GUIContent(/* "展示台座を使用する" */ AssetUtility.GetMain("Vket_ControlPanel.DrawAvatarSetting.UsePedestal")));
                if (EditorGUI.EndChangeCheck())
                {
                    avatarInfoSo.ApplyModifiedProperties();
                    EditorUtility.SetDirty(_avatarInfo);
                    var modelRoot = GameObject.Find("ReferenceObjects/Pedestals/Model/Pixcelica_booth");
                    modelRoot.transform.Find("Stage").gameObject.SetActive(_avatarInfo.UsePedestal);
                }
                
                EditorGUILayout.HelpBox(/* "最大3つまでアバターボードを設定できます。\n右下の[+]と[-]ボタンでボードの数を切り替えることが可能です。" */
                    AssetUtility.GetMain("Vket_ControlPanel.DrawAvatarSetting.BordInfoHelpBox"), MessageType.Info);
                
                EditorGUIUtility.labelWidth = 60;
                _avatarDataReorderableList.DoLayoutList();
                
                EditorGUILayout.HelpBox(/* "アバターペデスタルに登録されたアバターが開催期間になってもクロスプラットフォームに対応していない場合はアバターペデスタルの撤去対応を行います。" */
                    AssetUtility.GetMain("Vket_ControlPanel.DrawAvatarSetting.Caution"), MessageType.Warning);
                
                EditorGUIUtility.labelWidth = tempWidth;
                // 線
                GUILayout.Box("", GUILayout.Height(2), GUILayout.ExpandWidth(true));
            }
            else
            {
                // シーンが削除された場合、こちらを通るようになるので次のループで読み込み直すためnullを挿入
                _avatarInfo = null;
                _avatarDataReorderableList = null;
                EditorGUILayout.HelpBox( /* "アバターの入稿用シーンが見つかりません。\n入稿用シーンを作成してください。" */ AssetUtility.GetMain("Vket_ControlPanel.DrawAvatarSetting.NotFoundScene.Help"), MessageType.Error);
            }
        }

        /// <summary>
        /// シーンからシーンと同名のオブジェクトを取得する
        /// </summary>
        /// <returns>シーンと同名のオブジェクト</returns>
        private static Transform GetSceneNameObject()
        {
            var scene = SceneManager.GetActiveScene();
            Transform root = null;
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj.name == scene.name)
                {
                    root = obj.transform;
                    break;
                }
            }

            return root;
        }

        /// <summary>
        /// 入稿フォルダからItemInfoを読み込む
        /// ログインしている状態で使用すること前提
        /// </summary>
        /// <param name="info"></param>
        /// <returns>入稿フォルダのItemInfo</returns>
        private static VketItemInfo LoadItemInfo()
        {
            if (!CircleNullOrEmptyCheck())
            {
                // シーンフォルダから読み込み
                return AssetDatabase.LoadAssetAtPath<VketItemInfo>($"Assets/{GetExhibitorID()}/Config/VketItemInfo.asset");
            }

            return null;
        }

        private static bool IsExistItemInfo()
        {
            if (CircleNullOrEmptyCheck())
                return false;

            return File.Exists($"Assets/{GetExhibitorID()}/Config/VketItemInfo.asset");
        }
        
        private static VketAvatarInfo LoadAvatarInfo()
        {
            if (!CircleNullOrEmptyCheck())
            {
                // シーンフォルダから読み込み
                return AssetDatabase.LoadAssetAtPath<VketAvatarInfo>($"Assets/{GetExhibitorID()}/Config/VketAvatarInfo.asset");
            }

            return null;
        }

        private ReorderableList LoadAvatarDataReorderableList(SerializedObject serializedObject)
        {
            const int maxListSize = 3;
            var listProp = serializedObject.FindProperty("AvatarDataList");
            var reorderableList = new ReorderableList(serializedObject, listProp);
            reorderableList.draggable = false;
            reorderableList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, AssetUtility.GetMain("Vket_ControlPanel.VketAvatarInfoList.Header"));
            reorderableList.onCanAddCallback = list => list.count < maxListSize;
            reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
            {
                var elementProperty = listProp.GetArrayElementAtIndex(index);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(rect, elementProperty, new GUIContent(/* "アバター {0}" */  AssetUtility.GetMain("VketAvatarInfo.Header", index)));
                if (EditorGUI.EndChangeCheck())
                {
                    UpdateList(reorderableList);
                }
            };
            
            reorderableList.onChangedCallback = UpdateList;
            // ReorderableList作成時に更新
            UpdateList(reorderableList);

            void UpdateList(ReorderableList list)
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(serializedObject.targetObject);
                
                var modelRoot = GameObject.Find("ReferenceObjects/Pedestals/Model/Pixcelica_booth");
                if (modelRoot)
                {
                    Transform[] boards = {
                        modelRoot.transform.Find("Boards1"),
                        modelRoot.transform.Find("Boards2"),
                        modelRoot.transform.Find("Boards3"),
                    };

                    for (int i = 0; i < boards.Length; i++)
                    {
                        boards[i].gameObject.SetActive(i == list.count-1);
                        if (i == list.count - 1)
                        {
                            //var circleName = AssetUtility.LoginInfoData.SelectedCircle.GetCircleName(LocalizedSetting.Instance.Language);
                            var avatarInfo = (VketAvatarInfo)serializedObject.targetObject;
                            int bordIndex = 0;
                            foreach (Transform bord in boards[i])
                            {
                                var shopNameTMP = bord.transform.Find("CircleNameTMP").GetComponent<TextMeshPro>();
                                var avatarNameTMP = bord.transform.Find("AvatarNameTMP").GetComponent<TextMeshPro>();
                                var priceTMP = bord.transform.Find("PriceTMP").GetComponent<TextMeshPro>();
                                var avatarPedestal = bord.transform.Find("VketAvatarPedestal_Default/Pedestal").GetComponent<VRCAvatarPedestal>();
                                
                                var data = avatarInfo.AvatarDataList[bordIndex];
                                shopNameTMP.text = data.ShopName;
                                avatarNameTMP.text = data.AvatarName;
                                priceTMP.text = data.Price;
                                avatarPedestal.blueprintId = data.BlueprintId;
                                EditorUtility.SetDirty(shopNameTMP);
                                EditorUtility.SetDirty(avatarNameTMP);
                                EditorUtility.SetDirty(priceTMP);
                                EditorUtility.SetDirty(avatarPedestal);
                                bordIndex++;
                            }
                        }
                    }
                    
                    modelRoot.transform.Find("Stage").gameObject.SetActive(_avatarInfo.UsePedestal);
                }
            }
            
            reorderableList.elementHeightCallback = index => EditorGUI.GetPropertyHeight(listProp.GetArrayElementAtIndex(index));
            return reorderableList;
        }

        private static bool IsExistAvatarInfo()
        {
            if (CircleNullOrEmptyCheck())
                return false;

            return File.Exists($"Assets/{GetExhibitorID()}/Config/VketAvatarInfo.asset");
        }

        /// <summary>
        /// ItemTypeの更新時に呼ぶ想定の処理
        /// </summary>
        /// <param name="isUploadTiming">入稿タイミングの場合true</param>
        private static void UpdateItemType(bool isUploadTiming = false)
        {
            Transform root = GetSceneNameObject();
            var itemInfo = LoadItemInfo();
            if (root && itemInfo)
            {
                // 初期化
                RemovePreviewPickup(root);
                RemovePreviewAvatarPedestal(root);

                // アップロード処理の場合は追加しない
                if (isUploadTiming)
                    return;

                switch (itemInfo.SelectType)
                {
                    case VketItemInfo.ItemType.None:
                        break;
                    case VketItemInfo.ItemType.Pickup:
                        AddPreviewPickup(root);
                        break;
                    case VketItemInfo.ItemType.AvatarPedestal:
                        AddPreviewAvatarPedestal(root, itemInfo.BlueprintID);
                        break;
                }
            }
            UpdateItemThumbnailMaterial();
        }

        private static void AddPreviewPickup(Transform root)
        {
            AdjustCollider(root);
            var pickup = root.gameObject.AddComponent<VRCPickup>();
            pickup.AutoHold = VRC_Pickup.AutoHoldMode.Yes;
            pickup.orientation = VRC_Pickup.PickupOrientation.Grip;
            var rb = root.GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;
        }

        private static void RemovePreviewPickup(Transform root)
        {
            if (!root)
            {
                Debug.Log($"Root Object not found.");
                return;
            }

            var pickups = root.gameObject.GetComponents<VRCPickup>();
            foreach (var t in pickups)
            {
                DestroyImmediate(t);
            }

            var colliders = root.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                DestroyImmediate(collider);
            }

            var rbs = root.GetComponents<Rigidbody>();
            foreach (var rb in rbs)
            {
                DestroyImmediate(rb);
            }
        }

        private const string AvatarPedestal3DPrefabGuid = "9fffe84a94533884eaf481963546643d";

        /// <summary>
        /// AvatarPedestalの追加
        /// rootがnullの場合はReferenceObjects内に生成する
        /// </summary>
        /// <param name="root"></param>
        /// <param name="bluePrintId"></param>
        private static void AddPreviewAvatarPedestal(Transform root, string bluePrintId)
        {
            var avatarPedestal3DPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(AvatarPedestal3DPrefabGuid));
            var pedestal3D = PrefabUtility.InstantiatePrefab(avatarPedestal3DPrefab) as GameObject;
            if (pedestal3D)
            {
                // ReferenceObjects内に生成
                var referenceObjects = GameObject.Find("ReferenceObjects");
                pedestal3D.transform.SetParent(referenceObjects.transform);

                pedestal3D.transform.position = new Vector3(0f, 1f, 0f);
                var prefabInfo = pedestal3D.GetComponent<VketPrefabInformation>();
                if (prefabInfo)
                    DestroyImmediate(prefabInfo);

                var vketAvatarPedestal = pedestal3D.GetComponent<VketAvatarPedestal>();
                if (vketAvatarPedestal)
                {
                    VketAvatarPedestalEditor.AdjustCapsuleCollider(vketAvatarPedestal, root);
                    var avatarPedestalSerializedObject = new SerializedObject(vketAvatarPedestal);
                    var avatarPedestalProperty = avatarPedestalSerializedObject.FindProperty("avatarPedestal");
                    avatarPedestalSerializedObject.Update();
                    var avatarPedestal = avatarPedestalProperty.objectReferenceValue as VRCAvatarPedestal;
                    if (avatarPedestal)
                    {
                        avatarPedestal.blueprintId = bluePrintId;
                        EditorUtility.SetDirty(avatarPedestal);
                        avatarPedestalSerializedObject.ApplyModifiedProperties();
                    }
                }
            }
        }

        private static void RemovePreviewAvatarPedestal(Transform root)
        {
            if (!root)
            {
                Debug.Log($"Root Object not found.");
                return;
            }

            // 入稿IDオブジェクト内のAvatarPedestal3D削除
            foreach (var childTransform in root.GetComponentsInChildren<Transform>())
            {
                if (PrefabUtility.GetPrefabInstanceStatus(childTransform.gameObject) != PrefabInstanceStatus.Connected)
                {
                    continue;
                }

                var prefabObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(childTransform);
                var path = AssetDatabase.GetAssetPath(prefabObject);
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (guid == AvatarPedestal3DPrefabGuid)
                {
                    DestroyImmediate(childTransform.gameObject);
                    return;
                }
            }

            // ReferenceObjects内のAvatarPedestal3D削除
            var referenceObjects = GameObject.Find("ReferenceObjects");
            foreach (var childTransform in referenceObjects.GetComponentsInChildren<Transform>())
            {
                if (PrefabUtility.GetPrefabInstanceStatus(childTransform.gameObject) != PrefabInstanceStatus.Connected)
                {
                    continue;
                }

                var prefabObject = PrefabUtility.GetCorrespondingObjectFromOriginalSource(childTransform);
                var path = AssetDatabase.GetAssetPath(prefabObject);
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (guid == AvatarPedestal3DPrefabGuid)
                {
                    DestroyImmediate(childTransform.gameObject);
                    return;
                }
            }
        }
        
        private static void AdjustCollider(Transform root)
        {
            if (!root)
            {
                Debug.Log($"Root Object not found.");
                return;
            }

            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.Log("Renderer not found.");
                return;
            }
            
            var collider = root.GetComponent<BoxCollider>();
            if (!collider)
            {
                collider = root.gameObject.AddComponent<BoxCollider>();
            }

            Bounds totalBounds = CalculateBounds(renderers);

            Vector3 pos = root.transform.position;
            Vector3 scale = root.transform.lossyScale;

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

            collider.center = localCenter;
            //collider.height = localSize.y;
            //collider.radius = localSize.x <= localSize.y ? localSize.x * 0.5f : localSize.y * 0.5f;
            collider.size = localSize;

            Debug.Log("Setup Completed.");
        }

        private static Bounds CalculateBounds(Renderer[] renderers)
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

        /// <summary>
        /// ワールド選択画面に戻る
        /// </summary>
        /// <param name="info"></param>
        private static void BackWorldSelectWindow(LoginInfo info)
        {
            _itemInfo = null;
            info.SelectedCircleIndex = 9999;
            info.SelectedWorld = default;
            info.SelectedType = LoginInfo.ExhibitType.None;
            info.SelectedOuterFrameTemplate = 0;
            info.Save();
            _outerFrameDatas = null;
        }
        
        #region ボタンクリック時の処理
        
        private static async void LoadTemplateButton_Click()
        {
            await RefreshAuth();

            if (EditorPlayCheck() || !UpdateCheck())
            {
                return;
            }

            Type type = typeof(TemplateLoaderWindow);
            ReflectionUtility.InvokeMethod(type, "Open", null, null);
            var templateIndex = LoginInfo.CurrentWorldDefinition.TemplateIndex;
            if (!CircleNullOrEmptyCheck())
            {
                ReflectionUtility.SetField(type, "popupIndex", null, templateIndex);
                var methodInfo = typeof(TemplateLoader)
                    .GetMethod("GetTemplateProperty", BindingFlags.Static | BindingFlags.Public);
                if (methodInfo != null)
                {
                    ReflectionUtility.SetField(type, "templateProperty", null,
                        methodInfo.Invoke(null,
                            new object[]
                            {
                                ((string[])ReflectionUtility.GetField(type, "templateFolders", null))[templateIndex]
                            }));
                }

                switch (AssetUtility.VersionInfoData.Type)
                {
                    case VersionInfo.PackageType.stable:
                    case VersionInfo.PackageType.company:
                    case VersionInfo.PackageType.community:
                    case VersionInfo.PackageType.develop:
                    {
                        ReflectionUtility.SetField(type, "replaceStringList", null,
                            new Dictionary<string, string> { { "CIRCLEID", GetExhibitorID() } });
                        break;
                    }
                    default:
                    {
                        ReflectionUtility.SetField(type, "replaceStringList", null,
                            new Dictionary<string, string> { { "CIRCLEID", "1" } });
                        break;
                    }
                }
            }
        }

        private static string GetExhibitorID()
        {
            LoginInfo info = AssetUtility.LoginInfoData;
            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.company:
                case VersionInfo.PackageType.community:
                case VersionInfo.PackageType.develop:
                    return info.SpecialExhibitedId.ToString();
                default:
                    var circles = info.Circles;
                    return circles[info.SelectedCircleIndex].circle_id.ToString();
            }
        }

        /// <summary>
        /// 入稿シーンを開くボタンの処理
        /// </summary>
        private static void OpenSubmissionSceneButton_Click()
        {
            if (!CircleNullOrEmptyCheck())
            {
                string exhibitorID = GetExhibitorID();
                var userSettings = SettingUtility.GetSettings<UserSettings>();
                userSettings.validatorFolderPath = $"Assets/{exhibitorID}";
                SettingUtility.SaveSettings(userSettings);
                if (!string.IsNullOrEmpty(exhibitorID))
                {
                    var scenePath = $"Assets/{exhibitorID}/{exhibitorID}.unity";

                    if (File.Exists(scenePath))
                    {
                        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                        {
                            EditorSceneManager.OpenScene(scenePath);
                        }
                    }
                    else
                    {
                        /* "シーンが開けませんでした。" */ /* "入稿用シーンが見つかりません。入稿用シーンを作成してください。" */
                        EditorUtility.DisplayDialog(AssetUtility.GetMain("Vket_ControlPanel.OpenSubmissionSceneButton_Click.ErrorDialog.Title"), AssetUtility.GetMain("Vket_ControlPanel.OpenSubmissionSceneButton_Click.ErrorDialog.Message"), "OK");
                    }
                }
                else
                {
                    /* "シーンが開けませんでした。" */ /* "入稿用シーンが見つかりません。入稿用シーンを作成してください。" */
                    EditorUtility.DisplayDialog(AssetUtility.GetMain("Vket_ControlPanel.OpenSubmissionSceneButton_Click.ErrorDialog.Title"), AssetUtility.GetMain("Vket_ControlPanel.OpenSubmissionSceneButton_Click.ErrorDialog.Message"), "OK");
                }
            }
        }

        private static string GetExhibitorSceneAssetPath()
        {
            if (CircleNullOrEmptyCheck())
                return null;

            string exhibitorID = GetExhibitorID();
            if (string.IsNullOrEmpty(exhibitorID))
                return null;

            var scenePath = $"Assets/{exhibitorID}/{exhibitorID}.unity";
            return File.Exists(scenePath) ? scenePath : null;
        }

        private static async void BoothCheckButton_Click()
        {
            await RefreshAuth();

            LoginInfo info = AssetUtility.LoginInfoData;
            bool isItem = info.IsItem;
            if (isItem)
            {
                // ItemType更新
                UpdateItemType();
            }

            if (EditorPlayCheck() || !UpdateCheck())
            {
                return;
            }

            try
            {
                _cts = new CancellationTokenSource();
                _currentUniTask = BoothCheckTask(_cts.Token);
                await _currentUniTask;
                Debug.Log("Booth check completed");
            }
            catch (Exception e)
            {
                Debug.Log($"Booth check has been canceled.:{e}");
                EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.BoothCheckButton_Click.Cancel.Title"), /* "ブースチェックをキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.BoothCheckButton_Click.Cancel.Message"), "OK");
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _currentUniTask = default;
            }
        }

        private static async void BuildSizeCheckButton_Click()
        {
            await RefreshAuth();

            if (EditorPlayCheck() || !UpdateCheck())
            {
                return;
            }

            try
            {
                _cts = new CancellationTokenSource();
                _currentUniTask = BuildSizeCheckTask(_cts.Token, AssetUtility.LoginInfoData);
                await _currentUniTask;
                Debug.Log("Capacity check completed");
            }
            catch (OperationCanceledException e)
            {
                Debug.Log($"Capacity check canceled.:{e}");
                EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckButton_Click.Cancel.Title"), /* "容量チェックをキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckButton_Click.Cancel.Message"), "OK");
            }
            catch (Exception)
            {
                EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckButton_Click.Cancel.Title"), /* "容量チェックをキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckButton_Click.Cancel.Message"), "OK");
                throw;
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _currentUniTask = default;
            }
        }

        private static async void SetPassCheckButton_Click()
        {
            await RefreshAuth();

            if (EditorPlayCheck() || !UpdateCheck())
            {
                return;
            }
            
            try
            {
                VketBlinderToolbar.IsBlind = true;
                _cts = new CancellationTokenSource();
                _currentUniTask = SetPassCheckPreprocessingTask(_cts.Token);
                await _currentUniTask;
                Debug.Log("SetPass check preprocessing completed");
            }
            catch (Exception e)
            {
                VketBlinderToolbar.IsBlind = false;
                Debug.Log($"Canceled SetPass check.:{e}");
                EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.SetPassCheckButton_Click.Cancel.Title"), /* "SetPassチェックをキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.SetPassCheckButton_Click.Cancel.Message"), "OK");
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _currentUniTask = default;
            }
        }

        private static async void VRCCheckButton_Click()
        {
            await RefreshAuth();

            LoginInfo info = AssetUtility.LoginInfoData;
            bool isItem = info.IsItem;
            if (isItem)
            {
                // ItemType更新
                UpdateItemType();
            }

            if (EditorPlayCheck() || !UpdateCheck())
            {
                return;
            }

            try
            {
                // ベイク
                _cts = new CancellationTokenSource();
                _currentUniTask = BakeCheckAndRun(false, _cts.Token);
                await _currentUniTask;
                // ビルド&Test
                _currentUniTask = BuildAndTestWorld(false);
                await _currentUniTask;
                Debug.Log("VRC check completed");
            }
            catch (Exception e)
            {
                Debug.Log($"Booth check has been canceled.:{e}");
                EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.VRCCheckButton_Click.Cancel.Title"), /* "VRCチェックをキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.VRCCheckButton_Click.Cancel.Message"), "OK");
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                _currentUniTask = default;
            }
        }
        
        private static void ExportBoothPackage(string baseFolderAssetPath, string exportFilePath, bool openSaveFilePanel = false, ExportPackageOptions flags = ExportPackageOptions.Interactive)
        {
            if (string.IsNullOrEmpty(exportFilePath))
                exportFilePath = "BoothData.unitypackage";

            if (openSaveFilePanel)
            {
                var exportFolderPath = "Release";
                if (!Directory.Exists(exportFolderPath))
                {
                    Directory.CreateDirectory(exportFolderPath);
                }

                // 保存先を選択
                exportFilePath = EditorUtility.SaveFilePanel("出力先", exportFolderPath, exportFilePath, "unitypackage");
                // 入力が無ければキャンセル
                if (string.IsNullOrEmpty(exportFilePath)) return;
            }

            var scenePath = GetExhibitorSceneAssetPath();
            if (string.IsNullOrEmpty(scenePath))
            {
                /* "入稿用シーンが存在しません。" */ /* "入稿用シーンがありません。入稿用シーンを作成してください。" */
                EditorUtility.DisplayDialog(AssetUtility.GetMain("Vket_ControlPanel.BoothExportButton_Click.ErrorDialog.Title"), AssetUtility.GetMain("Vket_ControlPanel.BoothExportButton_Click.ErrorDialog.Message"), "OK");
                return;
            }

            var scene = SceneManager.GetSceneByPath(scenePath);
            ResetBoundsComponents(scene);
            CopyProxyToUdon(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            if (!Directory.Exists(baseFolderAssetPath))
            {
                /* 入稿フォルダが見つかりません。 */ /* 入稿シーンを作成してください。 */
                EditorUtility.DisplayDialog(AssetUtility.GetMain("Vket_ControlPanel.BoothExportButton_Click.FailedExport.Title"),
                    AssetUtility.GetMain("Vket_ControlPanel.BoothExportButton_Click.FailedExport.Message"), "OK");
            }

            AssetDatabase.Refresh();

            var filePathArray = Directory.GetFileSystemEntries(baseFolderAssetPath, "*", SearchOption.AllDirectories)
                                         .Where(path => !path.Contains(".meta"))
                                         .Concat(new[] { baseFolderAssetPath })
                                         .Select(path => path.Replace('\\', '/'))
                                         .ToArray();
            
            AssetDatabase.ExportPackage(filePathArray, exportFilePath, flags);
        }

        /// <summary>
        /// 入稿ボタン押下時に呼ばれる
        /// </summary>
        private static async void DraftButton_Click()
        {
            if (AssetUtility.VersionInfoData.Type == VersionInfo.PackageType.develop)
            {
                DraftFirst().Forget();
                return;
            }
            
            await RefreshAuth();
            if (EditorPlayCheck() || !UpdateCheck())
            {
                return;
            }
            
            LoginInfo info = AssetUtility.LoginInfoData;
            // 権限がadminかtesterならば入稿期間チェックをスキップする
            if (info.User.roles != null && (info.User.roles.Contains(0) || info.User.roles.Contains(1)))
            {
                DraftFirst().Forget();
                return;
            }
            
            if (Utilities.Hiding.HidingUtil.DebugMode || await info.IsSubmit())
            {
                DraftFirst().Forget();
                return;
            }
            
            EditorUtility.DisplayDialog("Error", /* 入稿期間外です。 */AssetUtility.GetMain("Vket_ControlPanel.DraftButton_Click.OutsideSubmissionPeriod"), "OK");
        }
        
        #endregion //ボタンクリック時の処理

        #endregion // コントロールパネルウィンドウ

        #region 入稿シークエンスウィンドウ

        private void DraftSequenceWindow()
        {
            EditorGUILayout.LabelField( /* "入稿処理中です。" */ AssetUtility.GetMain("Vket_ControlPanel.DraftSequenceWindow.Title"), EditorStyles.wordWrappedLabel);
            EditorGUILayout.HelpBox( /* "入稿処理中はこれらのウィンドウを閉じないでください。\n[VRChat SDK], [VketTools], [Game]" */ AssetUtility.GetMain("Vket_ControlPanel.DraftSequenceWindow.Help"), MessageType.Warning);

            // 実行中のタスク
            EditorGUILayout.LabelField(VketSequenceDrawer.GetSequenceText((int)_currentSeq), EditorStyles.wordWrappedLabel);

            switch (_currentSeq)
            {
                case DraftSequence.VRChatCheck:
                    if (VRCSdkControlPanel.window.PanelState == SdkPanelState.Building)
                    {
                        EditorGUILayout.LabelField(_isBuildWorld
                            ?
                            /* "テストの準備中です。しばらくお待ちください。" */ AssetUtility.GetMain("Vket_ControlPanel.DraftSequenceWindow.Build.Message")
                            :
                            /* "確認ダイアログ表示中です。" */ AssetUtility.GetMain("Vket_ControlPanel.DraftSequenceWindow.Build.Confirm"), EditorStyles.wordWrappedLabel);
                    }

                    break;
            }

            VketSequenceDrawer.Draw(position);

            // 入稿キャンセルボタン
            if (_cts != null)
            {
                var buttonStyle = new GUIStyle(GUI.skin.button);
                buttonStyle.fontSize = 20;
                if (GUILayout.Button( /* "入稿キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.DraftSequenceWindow.CancelButton"), buttonStyle, GUILayout.Height(30f)))
                {
                    _cts.Cancel();
                    _cts.Dispose();
                    _cts = null;
                }
            }
        }

        #endregion // 入稿シークエンスウィンドウ
        
        #region タスク処理
        
        #region ベイクチェック
        
        /// <summary>
        /// 入稿処理①
        /// ベイクチェック
        /// </summary>
        /// <param name="cancellationToken"></param>
        static async UniTask DraftBakeCheckFunc(CancellationToken cancellationToken)
        {
            _currentSeq = DraftSequence.BakeCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Running);

            // 1秒待つ
            await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, cancellationToken: cancellationToken);
            // ベイク実行
            await BakeCheckAndRun(true, cancellationToken);

            VketSequenceDrawer.SetState(_currentSeq, Status.Complete);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }
        
        /// <summary>
        /// シーンのベイク
        /// </summary>
        /// <param name="isVRChatCheck"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async UniTask BakeCheckAndRun(bool isVRChatCheck, CancellationToken cancellationToken)
        {
            Scene scene = SceneManager.GetActiveScene();
            string scenePath = scene.path;
            if (string.IsNullOrEmpty(scenePath) || !File.Exists(scenePath))
            {
                EditorUtility.DisplayDialog("Error", /* シーンが見つかりませんでした。 */AssetUtility.GetMain("Vket_ControlPanel.BoothCheckFunc.NotFoundScene"), "OK");
                throw new OperationCanceledException("Not Found Scene");
            }

            Scene untitledScene = SceneManager.GetSceneByPath("");
            var rootObjectName = scene.name;
            if (scene == untitledScene)
            {
                EditorUtility.DisplayDialog("Error", /* "シーンファイルが保存されていません。" */ AssetUtility.GetMain("Vket_ControlPanel.BakeCheckAndRun.NotSaveScene.Message"), "OK");
                throw new Exception("Active Scene is Untitled Scene");
            }
            
            var objList = new Dictionary<GameObject, bool>();
            if (!isVRChatCheck)
            {
                // 親が無いオブジェクトのアクティブ状態を保持
                foreach (GameObject obj in Array.FindAll(FindObjectsOfType<GameObject>(),
                             (item) => item.transform.parent == null))
                {
                    if (obj.name != rootObjectName)
                    {
                        objList.Add(obj, obj.activeSelf);
                    }
                }
            }

            EditorUtility.DisplayProgressBar("Bake", "Baking...", 0);
            try
            {
                bool bakeFlag = Lightmapping.BakeAsync();
                if (!bakeFlag)
                {
                    if (EditorUtility.DisplayDialog("Error", /* Light Bakeに失敗しました。 */AssetUtility.GetMain("Vket_ControlPanel.BakeCheckAndRun.LightBakeFailed"), "OK"))
                    {
                        // Cancel例外を投げる
                        throw new OperationCanceledException("Light Bake Failed");
                    }
                }
                
                while (Lightmapping.isRunning)
                {
                    EditorUtility.DisplayProgressBar("Bake", "Baking...", Lightmapping.buildProgress);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }
            }
            catch
            {
                Lightmapping.Cancel();
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                BakeCheckAndRunExit();
            }

            void BakeCheckAndRunExit()
            {
                if (!isVRChatCheck)
                {
                    // 親が無いオブジェクトのアクティブ状態を戻す
                    foreach (var pair in objList)
                    {
                        pair.Key.SetActive(pair.Value);
                    }
                }
            }
        }
        
        #endregion // ベイクチェック

        #region VRChat確認

        /// <summary>
        /// 入稿処理②
        /// VRChat確認
        /// </summary>
        /// <param name="cancellationToken"></param>
        static async UniTask DraftVrcCheckFunc(CancellationToken cancellationToken)
        {
            _currentSeq = DraftSequence.VRChatCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Running);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);

            // VRChat.exe実行
            if (!await BuildAndTestWorld())
            {
                throw new OperationCanceledException("LocalTest Failed or Cancel");
            }

            VketSequenceDrawer.SetState(_currentSeq, Status.Complete);
            await UniTask.Yield(cancellationToken);
        }
        
        /// <summary>
        /// VRCローカルテスト
        /// </summary>
        private static async UniTask<bool> BuildAndTestWorld(bool showDialog = true)
        {
            // VRC_SceneDescriptorが存在するか確認
            if (!FindObjectsOfType<VRC_SceneDescriptor>().Any())
            {
                EditorUtility.DisplayDialog(
                    "Error", /* VRC_SceneDescriptorが見つかりませんでした。\nVRCWorldをシーンに追加してからもう一度実行してください。 */
                    AssetUtility.GetMain("Vket_ControlPanel.VRC_LocalTestLaunch.NotFoundVRCWorld"), "OK");
                return false;
            }

            // Builderが存在するか確認
            if (!VRCSdkControlPanel.TryGetBuilder<IVRCSdkWorldBuilderApi>(out var builder))
            {
                EditorUtility.DisplayDialog(
                    "Error", "Not Found IVRCSdkWorldBuilderApi", "OK");
                return false;
            }
            
            var scene = SceneManager.GetActiveScene();
            var info = AssetUtility.LoginInfoData;
            var circles = info.Circles;
            string rootObjectName;
            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.company:
                case VersionInfo.PackageType.community:
                case VersionInfo.PackageType.develop:
                    rootObjectName = scene.name;
                    break;
                default:
                    rootObjectName = circles[info.SelectedCircleIndex].circle_id.ToString();
                    break;
            }

            // シーンに配置しているparentを持たないオブジェクトで不要なもののアクティブ状態のリスト
            // Key:gameObject名,Value:activeSelf
            var activeDisturbingObjList = new List<GameObject>();
            // parentを持たないオブジェクトで不要なものを非アクティブにしてからローカルテストする
            foreach (var obj in scene.GetRootGameObjects())
            {
                if (obj.name != rootObjectName && obj.name != "ReferenceObjects")
                {
                    if (obj.activeSelf)
                    {
                        activeDisturbingObjList.Add(obj);
                        obj.SetActive(false);
                    }
                }
            }

            // ビルド実行
            try
            {
                _isBuildWorld = true;
                await builder.BuildAndTest();
                _isBuildWorld = false;
                // ウィンドウに反映
                await UniTask.DelayFrame(3);
            }
            catch (Exception e) when (!(e is OperationCanceledException))
            {
                // エラーメッセージ表示
                Debug.LogError(e.Message);
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "Error", /* "ビルドに失敗しました。入稿処理を中断します。" */
                        AssetUtility.GetMain("Vket_ControlPanel.BuildAndTestWorld.BuildFaild.Message"), "OK");
                }

                return false;
            }
            finally
            {
                _isBuildWorld = false;
                // アクティブ状態を元に戻す
                foreach (var obj in activeDisturbingObjList)
                    obj.SetActive(true);
            }

            // ビルド成功している場合、このタイミングでVRChat.exeが自動で起動する
            if (showDialog && !EditorUtility.DisplayDialog("VRChat Client Check.", /* VRChat内での確認を待っています。 */
                    AssetUtility.GetMain("Vket_ControlPanel.DraftButton_Click.WaitVRCCheck"), /* 確認しました */
                    AssetUtility.GetMain("Confirmed"), /* キャンセル */
                    AssetUtility.GetMain("Cancel")))
            {
                return false;
            }

            return true;
        }

        #endregion // VRChat確認
        
        #region ブースルールチェック

        /// <summary>
        /// ブースチェック
        /// </summary>
        /// <param name="cancellationToken"></param>
        private static async UniTask BoothCheckTask(CancellationToken cancellationToken) => await RuleCheck(false, cancellationToken);
        
        /// <summary>
        /// 入稿処理③
        /// ブースルールチェック
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="info"></param>
        static async UniTask DraftRuleCheckFunc(CancellationToken cancellationToken)
        {
            _currentSeq = DraftSequence.RuleCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Running);
            
            await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, cancellationToken: cancellationToken);

            bool ruleCheckResult = await RuleCheck(true, cancellationToken);
            if (!ruleCheckResult)
            {
                // エラーフラグの保持
                var editorInfo = AssetUtility.EditorPlayInfoData;
                editorInfo.ErrorFlag = true;
                editorInfo.Save();
            }
            
            VketSequenceDrawer.SetState(_currentSeq, Status.Complete, ruleCheckResult ? "Success" : "Error");
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            // ③ルールチェック終了
        }

        /// <summary>
        /// ルールチェック
        /// </summary>
        /// <param name="isDraft">入稿時に実行しているかのフラグ</param>
        /// <returns>ルールチェックエラーが無い場合にtrue</returns>
        /// <exception cref="OperationCanceledException"></exception>
        static async UniTask<bool> RuleCheck(bool isDraft, CancellationToken cancellationToken)
        {
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            var userSettings = SettingUtility.GetSettings<UserSettings>();
            userSettings.validatorRuleSetType = GetValidatorRule();
            if (!CircleNullOrEmptyCheck())
            {
                userSettings.validatorFolderPath = "Assets/" + GetExhibitorID();
            }
            SettingUtility.SaveSettings(userSettings);
            
            var baseFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(userSettings.validatorFolderPath);
            string baseFolderPath;
            if (baseFolder)
            {
                baseFolderPath = userSettings.validatorFolderPath;
            }
            else
            {
                if (isDraft)
                {
                    EditorUtility.DisplayDialog("Error", "Validator could not initialize validation rules.\nPlease Try Booth Check Manually.", "OK");
                    throw new OperationCanceledException("Validator could not initialize validation rules.");
                }
                
                if (!EditorUtility.DisplayDialog("Warning",
                        /* 入稿フォルダが見つかりませんでした。\n現在開いているシーンをチェックしますか？ */
                        AssetUtility.GetMain("Vket_ControlPanel.BoothCheckFunc.NotFoundFolder"), 
                        AssetUtility.GetMain("Yes"),
                        AssetUtility.GetMain("No")))
                {
                    throw new OperationCanceledException("Cancel Booth Check");
                }
                
                baseFolderPath = Path.GetDirectoryName(SceneManager.GetActiveScene().path);
                if (string.IsNullOrEmpty(baseFolderPath))
                {
                    EditorUtility.DisplayDialog("Error", /* シーンが見つかりませんでした。 */AssetUtility.GetMain("Vket_ControlPanel.BoothCheckFunc.NotFoundScene"), "OK");
                    throw new OperationCanceledException("Not Found Scene");
                }
            }

            if (!isDraft)
            {
                // VitDeckのSimpleValidatorWindowを呼び出してルールチェックを実行
                SimpleValidatorWindow.Validate(VitDeckValidator.GetRuleSet(userSettings.validatorRuleSetType), baseFolder ?? AssetDatabase.LoadAssetAtPath<DefaultAsset>(baseFolderPath));
                return true;
            }
            
            // ルールチェックのみを実行
            var results = VitDeckValidator.Validate(VitDeckValidator.GetRuleSet(userSettings.validatorRuleSetType), baseFolderPath);
            if (results == null)
            {
                Debug.LogError( /* ルールチェックが正常に終了しませんでした。 */ AssetUtility.GetMain("Vket_ControlPanel.DraftFunc.RuleCheckFailed"));
                Debug.Log(VketSequenceDrawer.GetResultLog());
                throw new OperationCanceledException("The Rule Check failed to run properly.");
            }

            return !results.Any(w => w.Issues.Any(w2 => w2.level == IssueLevel.Error));
        }

        #endregion
        
        #region ビルドサイズチェック
        
        class ReloadScriptsConfig : ScriptableSingleton<ReloadScriptsConfig>
        {
            internal enum ReloadScriptsState
            {
                None = 0,
                QuestBuildSizeCheck = 1,
                DraftQuestBuildSizeCheck = 2,
            }
            
            [SerializeField] private ReloadScriptsState _state = ReloadScriptsState.None;

            internal ReloadScriptsState State
            {
                get => _state;
                set => _state = value;
            }
            
            [DidReloadScripts]
            public static void OnDidReloadScripts()
            {
                switch (instance.State)
                {
                    // クロスプラットフォーム「容量チェック」ボタン後のコンパイル
                    case ReloadScriptsState.QuestBuildSizeCheck:
                        instance.State = 0;
                        EditorApplication.delayCall += () =>
                        {
                            // Buildタブを開く
                            OpenSDKBuilderTabTask().Forget();
                        };
                        break;
                    // クロスプラットフォーム入稿時「容量チェック」後のコンパイル
                    case ReloadScriptsState.DraftQuestBuildSizeCheck:
                        instance.State = 0;
                        EditorApplication.delayCall += () =>
                        {
                            DraftSecond().Forget();
                        };
                        break;
                }
            }
        }
        
        class IgnoreBuildSizeData
        {
            public IgnoreBuildSizeData(string assetPath, float fileSize, float androidFileSize)
            {
                AssetPath = assetPath;
                FileSize = fileSize;
                AndroidFileSize = androidFileSize;
            }

            public readonly string AssetPath;
            public readonly float FileSize;
            public readonly float AndroidFileSize;
        }

        /// <summary>
        /// ビルドに運営指定の容量が大きいアセットが含まれる場合のオフセット定義
        /// </summary>
        private static readonly IgnoreBuildSizeData[] BuildSizeDatas =
        {
            new("Assets/VketAssets/EssentialResources/Common/Fonts/Mplus1-Regular SDF VketAttachItem.asset", 0.1f, 0.1f),
        };
        
        /// <summary>
        /// 容量確認
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="info"></param>
        private static async UniTask BuildSizeCheckTask(CancellationToken cancellationToken, LoginInfo info)
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.isDirty)
            {
                if (!EditorUtility.DisplayDialog("Warning", /* シーンが変更されています。\n保存して実行しますか？ */
                        AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.SceneSaveConfirmationDialog"), /* はい */AssetUtility.GetMain("Yes"), /* いいえ */
                        AssetUtility.GetMain("No")))
                {
                    throw new OperationCanceledException("Select Not Save Scene");
                }
            }

            await BuildSizeCheck(cancellationToken, scene, info, false);

            // このままPlayModeに入るとAssemblyエラーを起こすので再コンパイル
            if (info.IsQuest)
            {
                ReloadScriptsConfig.instance.State = ReloadScriptsConfig.ReloadScriptsState.QuestBuildSizeCheck;
                AssetDatabase.Refresh();
                CompilationPipeline.RequestScriptCompilation();
            }
        }

        /// <summary>
        /// 入稿処理④
        /// ビルドサイズチェック
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="info"></param>
        static async UniTask DraftBuildSizeCheckTask(CancellationToken cancellationToken, LoginInfo info)
        {
            _currentSeq = DraftSequence.BuildSizeCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Running);
            await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, cancellationToken: cancellationToken);

            // アイテムの場合はPickup用コライダーの更新
            if (info.IsItem)
            {
                // Pickup更新
                UpdateItemType(true);
            }

            var buildSize = await BuildSizeCheck(cancellationToken, SceneManager.GetActiveScene(), info, true);
            var editorInfo = AssetUtility.EditorPlayInfoData;
            editorInfo.BuildSizeSuccessFlag = true;
            editorInfo.BuildSize = buildSize;
            editorInfo.Save();
            
            // このままPlayModeに入るとAssemblyエラーを起こすので再コンパイル
            if (info.IsQuest)
            {
                ReloadScriptsConfig.instance.State = ReloadScriptsConfig.ReloadScriptsState.DraftQuestBuildSizeCheck;
                AssetDatabase.Refresh();
                CompilationPipeline.RequestScriptCompilation();
            }
            else
            {
                DraftSecond().Forget();
            }
        }

        /// <summary>
        /// ビルドサイズ計算前の処理
        /// 入稿オブジェクト以外を削除する
        /// </summary>
        private static (float offset, float androidOffset) BuildSizeCheckPreprocessingFunc(LoginInfo info, Scene scene)
        {
            DestroySubObjects(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            float offset = 0;
            float androidOffset = 0;

            // 参照を確認し、オフセットを取得
            foreach (var dependency in AssetDatabase.GetDependencies(scene.path, true))
            {
                // Debug.Log(dependency);
                foreach (var ignoreBuildSizeData in BuildSizeDatas)
                {
                    if (dependency == ignoreBuildSizeData.AssetPath)
                    {
                        offset += ignoreBuildSizeData.FileSize;
                        androidOffset += ignoreBuildSizeData.AndroidFileSize;
                    }
                }
            }

            return (offset, androidOffset);
        }

        private static void DestroySubObjects(Scene scene)
        {
            string rootObjectName;
            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.company:
                case VersionInfo.PackageType.community:
                case VersionInfo.PackageType.develop:
                    rootObjectName = GetExhibitorID();
                    break;
                default:
                    var circles = AssetUtility.LoginInfoData.Circles;
                    rootObjectName = circles[AssetUtility.LoginInfoData.SelectedCircleIndex].circle_id.ToString();
                    break;
            }

            // 入稿オブジェクト以外を削除
            foreach (var rootGameObject in scene.GetRootGameObjects())
            {
                if (rootGameObject.name != rootObjectName)
                {
                    DestroyImmediate(rootGameObject);
                }
            }
        }
        
        static async UniTask<float> BuildSizeCheck(CancellationToken cancellationToken, Scene scene, LoginInfo info, bool isDraft)
        {
            // ビルドサイズ確認, Windows
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            var srcScenePath = scene.path;

            string tmpDirectoryPath = "Assets/VketTools/Tmp";
            string tmpScenePath = $"{tmpDirectoryPath}/TmpScene.unity";

            if (!Directory.Exists(tmpDirectoryPath))
            {
                Directory.CreateDirectory(tmpDirectoryPath);
                AssetDatabase.Refresh();
            }

            AssetDatabase.CopyAsset(srcScenePath, tmpScenePath);
            EditorSceneManager.OpenScene(tmpScenePath);
            scene = SceneManager.GetActiveScene();
            
            var offsetTuple = BuildSizeCheckPreprocessingFunc(info, scene);
            
            // 入稿時は①の処理でライトベイクを終えているのでスキップ
            if (!isDraft)
            {
                await BakeCheckAndRun(false, cancellationToken);
            }

            AssetExporter.DoPreExportShaderReplacement();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            bool isQuest = info.IsQuest;

            // ビルドサイズ確認
            float standaloneCmpSize;
            try
            {
                standaloneCmpSize = await CalculationCompileSize(BuildTarget.StandaloneWindows64, offsetTuple.offset);
            }
            finally
            {
                if (!isQuest)
                {
                    // シーンを再度開き元の状態に戻す。
                    EditorSceneManager.OpenScene(srcScenePath);
                    AssetDatabase.DeleteAsset(tmpDirectoryPath);
                }
            }

            float standaloneMaxSize = LoginInfo.CurrentWorldDefinition.BuildMaxSize;
            if (standaloneCmpSize > standaloneMaxSize)
            {
                Debug.LogError($"[<color=green>VketTools</color>] StandaloneWindows64 AssetBundle Size <color=#ff0000ff><size=30>{standaloneCmpSize}</size></color>MB is over Regulation: {standaloneMaxSize}MB");

                if (isDraft)
                {
                    // ビルドサイズ容量オーバーのためエラーフラグを立てておく
                    var editorInfo = AssetUtility.EditorPlayInfoData;
                    editorInfo.ErrorFlag = true;
                    editorInfo.Save();
                }
                else
                {
                    if (EditorUtility.DisplayDialog("Windows BuildSizeCheck",
                            $"{ /* 容量がオーバーしています。 */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.OverSize")}\r\n{standaloneCmpSize}MB\r\nRegulation {standaloneMaxSize}MB", /*入稿ルール*/
                            AssetUtility.GetMain("Vket_ControlPanel.SubmissionRule"), "OK"))
                    {
                        Application.OpenURL(GetRuleURL());
                    }

                    if (isQuest)
                    {
                        // シーンを再度開き元の状態に戻す。
                        EditorSceneManager.OpenScene(srcScenePath);
                        AssetDatabase.DeleteAsset(tmpDirectoryPath);
                    }

                    return standaloneCmpSize;
                }
            }
            else if (standaloneCmpSize == -1)
            {
                if (isDraft)
                {
                    EditorUtility.DisplayDialog("Error", /* 容量チェックに失敗しました。 */AssetUtility.GetMain("Vket_ControlPanel.DraftFunc.SizeCheckFailed"), "OK");
                    Debug.Log(VketSequenceDrawer.GetResultLog());
                    throw new OperationCanceledException("StandaloneWindows64 Build Size Check failed");
                }

                EditorUtility.DisplayDialog("Error", /* ビルドに失敗しました。 */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.Failed"), "OK");
            }
            else
            {
                if (!isDraft)
                {
                    switch (AssetUtility.VersionInfoData.Type)
                    {
                        case VersionInfo.PackageType.stable:
                        case VersionInfo.PackageType.develop:
                            if (EditorUtility.DisplayDialog("Windows BuildSizeCheck",
                                    $"{ /* PC ビルド完了！ */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.Compleate.PC")}\r\nCompressed Size: {standaloneCmpSize}MB\r\nRegulation {standaloneMaxSize}MB", /*入稿ルール*/
                                    AssetUtility.GetMain("Vket_ControlPanel.SubmissionRule"), "OK"))
                            {
                                Application.OpenURL(GetRuleURL());
                            }

                            break;
                        case VersionInfo.PackageType.company:
                        case VersionInfo.PackageType.community:
                            EditorUtility.DisplayDialog("Windows BuildSizeCheck",
                                $"{ /* PC ビルド完了！ */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.Compleate.PC")}\r\nCompressed Size: {standaloneCmpSize}MB\r\nRegulation {standaloneMaxSize}MB",
                                "OK");
                            break;
                    }
                }
            }

            if (!isQuest)
            {
                if (isDraft)
                {
                    VketSequenceDrawer.SetState(_currentSeq, Status.Complete, $"PC: {standaloneCmpSize} / {standaloneMaxSize} MB");
                    Debug.Log($"{ /* PC ビルド完了！ */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.Compleate.PC")}\r\nCompressed Size: {standaloneCmpSize}MB\r\nRegulation {standaloneMaxSize}MB");
                }

                return standaloneCmpSize;
            }

            // 2frame待機(1frameだと連続でのビルド実行判定となり、エラーを出力する)
            await UniTask.DelayFrame(2, cancellationToken: cancellationToken);

            // ビルドサイズ確認, Android
            float androidCmpSize;
            try
            {
                androidCmpSize = await CalculationCompileSize(BuildTarget.Android, offsetTuple.androidOffset);
            }
            finally
            {
                // シーンを再度開き元の状態に戻す。
                EditorSceneManager.OpenScene(srcScenePath);
                AssetDatabase.DeleteAsset(tmpDirectoryPath);
            }

            float androidMaxSize = LoginInfo.CurrentWorldDefinition.AndroidBuildMaxSize;
            if (androidCmpSize > androidMaxSize)
            {
                Debug.LogError($"[<color=green>VketTools</color>] Android AssetBundle Size <color=#ff0000ff><size=30>{androidCmpSize}</size></color>MB is over Regulation: {androidMaxSize}MB");

                if (isDraft)
                {
                    var editorInfo = AssetUtility.EditorPlayInfoData;
                    editorInfo.ErrorFlag = true;
                    editorInfo.Save();
                }
                else
                {
                    if (EditorUtility.DisplayDialog("Android BuildSizeCheck",
                            $"{ /* 容量がオーバーしています。 */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.OverSize")}\r\n{androidCmpSize}MB\r\nRegulation {androidMaxSize}MB", /*入稿ルール*/
                            AssetUtility.GetMain("Vket_ControlPanel.SubmissionRule"), "OK"))
                    {
                        Application.OpenURL(GetRuleURL());
                    }

                    return androidCmpSize;
                }
            }
            else if (androidCmpSize == -1)
            {
                if (isDraft)
                {
                    EditorUtility.DisplayDialog("Error", /* 容量チェックに失敗しました。 */AssetUtility.GetMain("Vket_ControlPanel.DraftFunc.SizeCheckFailed"), "OK");
                    Debug.Log(VketSequenceDrawer.GetResultLog());
                    throw new OperationCanceledException("Android Build Size Check failed");
                }

                EditorUtility.DisplayDialog("Error", /* ビルドに失敗しました。 */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.Failed"), "OK");
            }
            else
            {
                if (!isDraft)
                {
                    if (EditorUtility.DisplayDialog("Android BuildSizeCheck Complete",
                            $"{ /* CrossPlatform ビルド完了！ */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.Compleate.CrossPlatform")}\r\nCompressed Size: {androidCmpSize}MB\r\nRegulation {androidMaxSize}MB", /*入稿ルール*/
                            AssetUtility.GetMain("Vket_ControlPanel.SubmissionRule"), "OK"))
                    {
                        Application.OpenURL(GetRuleURL());
                    }
                }
            }
            
            if (isDraft)
            {
                VketSequenceDrawer.SetState(_currentSeq, Status.Complete, $"PC: {standaloneCmpSize} / {standaloneMaxSize} MB, CrossPlatform: {androidCmpSize} / {androidMaxSize} MB");
                Debug.Log($"{ /* CrossPlatform ビルド完了！ */AssetUtility.GetMain("Vket_ControlPanel.BuildSizeCheckFunc.Compleate.CrossPlatform")}\r\nCompressed Size: {androidCmpSize}MB\r\nRegulation {androidMaxSize}MB");
            }

            return androidCmpSize;
        }
        
        /// <summary>
        /// ビルドサイズを取得するタスク
        /// </summary>
        /// <returns>ビルドサイズ</returns>
        private static async UniTask<float> CalculationCompileSize(BuildTarget targetPlatform, float offset)
        {
            float cmpSize = AssetUtility.ForceRebuild(targetPlatform);
            cmpSize = BuildSizeZeroAdjust(cmpSize, offset);

            /*
             * NOTE: このタイミングで、Yieldを使用しないとタスクが止まる
             * await new YieldAwaitable(PlayerLoopTiming.Update)と同等の処理
             * このタイミングでのYield(CancellationToken cancellationToken)は処理が止まるので使用しないこと
             */
            await UniTask.Yield();

            return cmpSize;
        }

        #endregion // ビルドサイズチェック

        #region SetPassチェック & スクリーンショット撮影
        
        private static readonly Type GameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        
        /// <summary>
        /// SetPassチェック前処理(ボタン)
        /// </summary>
        private static async UniTask SetPassCheckPreprocessingTask(CancellationToken cancellationToken) => await SetPassCheckPreprocessing(false, cancellationToken);
        
        /// <summary>
        /// セットパスチェック前処理(入稿)
        /// </summary>
        static async UniTask DraftSetPassCheckPreprocessingTask(CancellationToken cancellationToken)
        {
            _currentSeq = DraftSequence.SetPassCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Running);
            await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, cancellationToken: cancellationToken);
            await SetPassCheckPreprocessing(true, cancellationToken);
        }
        static async UniTask SetPassCheckPreprocessing(bool isDraft, CancellationToken cancellationToken)
        {
            if (!isDraft)
            {
                // シーンのベイク
                await BakeCheckAndRun(false, cancellationToken);
            }
            
            EditorUtility.DisplayProgressBar("SetPassCalls and Batches Check.", "Start up...", 0);
            
            var editorInfo = AssetUtility.EditorPlayInfoData;
            editorInfo.IsVketEditorPlay = true;
            editorInfo.IsSetPassCheckOnly = !isDraft;
            editorInfo.ClientSimEnabledRestoreFlag = GetClientSimEnabled();
            editorInfo.Save();
            
            // ブラインド解除(Layoutに保存させない)
            VketBlinderToolbar.IsBlind = false;

            try
            {
                // Note: バックグラウンドだとOnDisableが何故かすぐに呼ばれないため、SDKウィンドウを直接閉じる
                CloseWindowIfOpen();
                if (HasOpenInstances<VRCSdkControlPanel>())
                {
                    var vrcsdkPanel = GetWindow<VRCSdkControlPanel>();
                    if (vrcsdkPanel)
                    {
                        vrcsdkPanel.Close();
                    }
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                await UniTask.Delay(TimeSpan.FromSeconds(3), DelayType.Realtime, cancellationToken: cancellationToken);
            }
            
            // WindowLayoutを保存
            SaveEditorLayout();
            await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, cancellationToken: cancellationToken);
            
            var game = GetWindow(GameViewType);
            game.Close();

            var setPassGameWindow = GetWindow(GameViewType, true);
            setPassGameWindow.minSize = new Vector2(960, 540);

            SetClientSimEnabled(false);
            
            EditorUtility.ClearProgressBar();
            
            if (isDraft)
            {
                // UniTaskがnullになるので次のフレームでUnityEditorを起動
                EditorApplication.delayCall += () => EditorApplication.isPlaying = true;
            }
            else
            {
                // Playモードに変更
                EditorApplication.isPlaying = true;
            }
        }
        
        /// <summary>
        /// セットパス確認ボタンが押された後、PlayModeに突入したときに呼ばれる
        /// 入稿タスク2
        /// </summary>
        private static async UniTask SetPassCheckIntermediateProcessingTask()
        {
            var editorInfo = AssetUtility.EditorPlayInfoData;
            bool isDraft = !editorInfo.IsSetPassCheckOnly;
            try
            {
                VketBlinderToolbar.IsBlind = true;
                _cts = new CancellationTokenSource();
                _currentUniTask = CheckSetPassEditorPlayTask(_cts.Token, editorInfo);
                await _currentUniTask;
                Debug.Log(isDraft ? "入稿タスク2終了" : "SetPass check postprocessing completed");
            }
            catch (OperationCanceledException e)
            {
                if (isDraft)
                {
                    // キャンセル時に呼ばれる例外
                    VketBlinderToolbar.IsBlind = false;
                    Debug.Log($"入稿タスク2をキャンセルしました。{e}");
                    EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.DraftTask.Cancel.Title"), /* "入稿をキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.DraftTask.Cancel.Message"), "OK");
                }
                else
                {
                    // キャンセル時に呼ばれる例外
                    Debug.Log($"Canceled SetPass check.:{e}");
                    EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.SetPassCheckButtonIntermediateProcessingTask.Cancel.Title"), /* "SetPassチェックをキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.SetPassCheckButtonIntermediateProcessingTask.Cancel.Message"), "OK");
                }
            }
        }
        
        #region PlayMode時のタスク
        
        const string SetPassCheckProgressTitle = "SetPassCalls and Batches Check.";
        
        private static void SetPassGameViewUpdate()
        {
            UnityEngine.Object[] objectsOfTypeAll = Resources.FindObjectsOfTypeAll(GameViewType);
            if (objectsOfTypeAll == null || objectsOfTypeAll.Length == 0)
            {
                var setPassGameWindow = GetWindow(GameViewType, true);
                setPassGameWindow.minSize = new Vector2(960, 540);
            }
        }
        
        /// <summary>
        /// プレイモード突入時に実行するタスク
        /// セットパス確認とスクリーンショット撮影
        /// </summary>
        /// <param name="cancellationToken"></param>
        private static async UniTask CheckSetPassEditorPlayTask(CancellationToken cancellationToken, EditorPlayInfo editorInfo)
        {
            DestroySubObjects(SceneManager.GetActiveScene());
            
            // コントロールパネルは閉じるので、タスクはキャンセルされない想定
            bool isDraft = !editorInfo.IsSetPassCheckOnly;
            try
            {
                // Playモード突入時にnullになっているので再読み込み
                LoginInfo.ReloadWorldDefinition();
                // セットパスチェック後半
                await SetPassCheckPostprocessingTask(cancellationToken, editorInfo);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                editorInfo.SetPassFailedFlag = true;
                editorInfo.Save();
                EditorApplication.isPlaying = false;
                
                if (isDraft)
                {
                    Debug.Log(VketSequenceDrawer.GetResultLog());
                    DraftExitFunc();
                }
                
                throw;
            }
            finally
            {
                if (!isDraft)
                {
                    VketBlinderToolbar.IsBlind = false;

                    // プレイモード終了
                    EditorApplication.isPlaying = false;
                    RestoreClientSimEnabled();

                    _cts.Dispose();
                    _cts = null;
                    _currentUniTask = default;
                }
            }
        }

        /// <summary>
        /// セットパスチェック後処理
        /// カメラを動かしてセットパスを計算する
        /// </summary>
        static async UniTask SetPassCheckPostprocessingTask(CancellationToken cancellationToken, EditorPlayInfo editorInfo)
        {
            if (EditorApplication.isPaused)
            {
                EditorApplication.isPaused = false;
            }

            foreach (Camera cam in Camera.allCameras.Union(SceneView.sceneViews.ToArray()
                                                                    .Select(s => ((SceneView)s).camera)))
            {
                cam.enabled = false;
            }

            GameObject checkParentObj = new GameObject("SetPassCheck");
            checkParentObj.transform.position = new Vector3(0, 0, 0);
            GameObject cameraObj = new GameObject("CheckCamera");
            Camera checkCam = cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
            checkCam.depth = 100;
            cameraObj.transform.SetParent(checkParentObj.transform);
            var standaloneMaxSize = LoginInfo.CurrentWorldDefinition.BuildMaxSize;
            var pos = new Vector3(0.0f, 2.5f * standaloneMaxSize / 10.0f, standaloneMaxSize * -1.0f);
            cameraObj.transform.position = pos;
            SceneManager.GetActiveScene().GetRootGameObjects()[0]?.transform.Find("Dynamic")?.gameObject
                        .SetActive(true);

            List<int> setPassCallsList = new List<int>();
            List<int> batchesList = new List<int>();
            int setPassCalls;
            int batches;
            
            try
            {
                // 360度計測
                for (int rotation = 0; rotation < 360; rotation++)
                {
                    if (EditorApplication.isPaused)
                    {
                        EditorApplication.isPaused = false;
                    }

                    float progress = (float)rotation / 360;
                    if (EditorUtility.DisplayCancelableProgressBar(SetPassCheckProgressTitle,
                            (progress * 100).ToString("F2") + "%",
                            progress))
                    {
                        throw new OperationCanceledException("Cancel Set Pass Check");
                    }

                    checkParentObj.transform.rotation = Quaternion.Euler(0, rotation, 0);
                    setPassCallsList.Add(UnityStats.setPassCalls);
                    batchesList.Add(UnityStats.batches);
                    await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                }

                DestroyImmediate(cameraObj);
                DestroyImmediate(checkParentObj);

                setPassCalls = editorInfo.SetPassCalls = (int)setPassCallsList.Average() - GetSetPassCallsOffset();
                batches = editorInfo.Batches = (int)batchesList.Average() - GetBatchesOffset();
                editorInfo.Save();

                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
            // 非入稿時
            if (editorInfo.IsSetPassCheckOnly)
            {
                var maxSetPass = -1;
                var maxBatches = -1;
                if (LoginInfo.CurrentWorldDefinition)
                {
                    maxSetPass = LoginInfo.CurrentWorldDefinition.SetPassMaxSize;
                    maxBatches = LoginInfo.CurrentWorldDefinition.BatchesMaxSize;
                }
                var overSetPass = setPassCalls > maxSetPass;
                var overBatches = batches > maxBatches;
                if (overSetPass)
                    Debug.LogError(
                        $"[<color=green>VketTools</color>] SetPass Calls <color=#ff0000ff><size=30>{setPassCalls}</size></color> exceed the limit: {maxSetPass}");
                if (overBatches)
                    Debug.LogError(
                        $"[<color=green>VketTools</color>] Batches <color=#ff0000ff><size=30>{batches}</size></color> exceed the limit: {maxBatches}");
                var message = $"SetPass Calls: {setPassCalls} / {maxSetPass}, "
                              + $"Batches: {batches} / {maxBatches}";
                if (overSetPass || overBatches)
                {
                    message += "\r\n\r\n" + (Application.systemLanguage == SystemLanguage.Japanese
                            ? "制限を超過しています"
                            : "exceed the regulation"
                        );
                }

                switch (AssetUtility.VersionInfoData.Type)
                {
                    case VersionInfo.PackageType.stable:
                    case VersionInfo.PackageType.develop:
                        if (EditorUtility.DisplayDialog("Complete!", message, /*入稿ルール*/
                                AssetUtility.GetMain("Vket_ControlPanel.SubmissionRule"), "OK"))
                        {
                            Application.OpenURL(GetRuleURL());
                        }

                        break;
                    case VersionInfo.PackageType.company:
                    case VersionInfo.PackageType.community:
                        EditorUtility.DisplayDialog("Complete!", message, "OK");
                        break;
                }
            }
            // 入稿時処理
            else
            {
                await ScreenShotPreprocessingTask(cancellationToken);
                
                // スクリーンショット撮影はssManager側に任せる
                var game = GetWindow(GameViewType, true);
                game.minSize = new Vector2(960f, 540f);
                GameObject SSCaptureObj = Instantiate(AssetUtility.SSCapturePrefab);
                GameObject SSCameraObj = Instantiate(AssetUtility.SSCameraPrefab);
                SSManager ssManager = SSCaptureObj.GetComponent<SSManager>();
                ssManager.capCamera = SSCameraObj.GetComponent<Camera>();

                var info = AssetUtility.LoginInfoData;
                if (info.IsItem)
                {
                    SSCameraObj.transform.position = new Vector3(0f, 1f,
                        3.0f);
                }
                else
                {
                    SSCameraObj.transform.position = new Vector3(0, 2.5f * LoginInfo.CurrentWorldDefinition.BuildMaxSize / 10.0f,
                        LoginInfo.CurrentWorldDefinition.BuildMaxSize - 2.0f);
                }
                
                ssManager.dcThumbnailChangeToggle.isOn = true;
                ssManager.dcThumbnailChangeToggle.interactable = false;
                ssManager.previewPlaceHopeToggle.transform.Find("Label").GetComponent<Text>()
                         .text = /* 下見時に配置されていた場所を希望する */AssetUtility.GetMain("Vket_ControlPanel.EditorPlay.HopePreview");
                ssManager.mainSubmissionToggle.transform.Find("Label").GetComponent<Text>().text = /* この入稿を本入稿とする */
                    AssetUtility.GetMain("Vket_ControlPanel.EditorPlay.FormalSubmission");
                ssManager.screenshotMessageText
                         .text = /* ※ <size=38>※</size> こちらのスクリーンショットは、Vketからの告知などに使用する可能性があります。\n　 あらかじめご了承下さい。また、ブース全体が見えるように撮影して下さい */
                    AssetUtility.GetMain("Vket_ControlPanel.EditorPlay.ScreenshotAttention");
                ssManager.dcThumbnailChangeToggle.gameObject.SetActive(true);
                ssManager.dcThumbnailChangeToggle.transform.Find("Label").GetComponent<Text>().text = /* ブース画像を変更する */
                    AssetUtility.GetMain("Vket_ControlPanel.EditorPlay.ChangeThumbnail");

                try
                {
                    if (await info.IsSubmit())
                    {
                        ssManager.mainSubmissionToggle.isOn = true;
                        ssManager.mainSubmissionToggle.interactable = false;
                    }
                }
                // TermCheckのNull例外は握りつぶす
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
        }

        #endregion PlayMode時のタスク
        
        /// <summary>
        /// SetPassチェック後処理(ボタン)
        /// </summary>
        private static void SetPassCheckPostprocessingFunc(EditorPlayInfo editorInfo)
        {
            // セットパス確認失敗フラグが立っている場合
            if (editorInfo.SetPassFailedFlag)
            {
                ReloadLayout();
                editorInfo.IsVketEditorPlay = false;
                editorInfo.IsSetPassCheckOnly = false;
                editorInfo.SetPassFailedFlag = false;
                editorInfo.Save();
                return;
            }

            editorInfo.IsVketEditorPlay = false;
            editorInfo.Save();

            if (!editorInfo.IsSetPassCheckOnly &&
                !editorInfo.SsSuccessFlag)
            {
                // 入稿処理中断
                Debug.Log(VketSequenceDrawer.GetResultLog());
                DraftExitFunc();
            }
            // 入稿処理中かつスクリーンショットの撮影に成功している場合
            else if (!editorInfo.IsSetPassCheckOnly &&
                     editorInfo.SsSuccessFlag)
            {
                ReloadLayout();
                // 続きの入稿処理へ
                DraftThird().Forget();
            }
            else
            {
                // セットパスコールチェック終了
                ReloadLayout();
                editorInfo.IsSetPassCheckOnly = false;
                editorInfo.Save();
            }

            void ReloadLayout()
            {
                // WindowLayoutを復元
                LoadEditorLayout();
                DeleteEditorLayout();

                OpenWindowIfClose();
                // Builderタブを開く
                OpenSDKBuilderTabTask().Forget();
            }
        }
        
        /// <summary>
        /// SetPassチェック後処理(入稿)
        /// スクリーンショットのキャプチャ後に呼ばれる
        /// </summary>
        static async UniTask DraftSetPassCheckPostprocessingTask(CancellationToken cancellationToken, EditorPlayInfo editorPlayInfo)
        {
            int setPassCalls = editorPlayInfo.SetPassCalls;
            int batches = editorPlayInfo.Batches;
            
            int maxSetPass = -1;
            int maxBatches = -1;
            if (LoginInfo.CurrentWorldDefinition)
            {
                maxSetPass = LoginInfo.CurrentWorldDefinition.SetPassMaxSize;
                maxBatches = LoginInfo.CurrentWorldDefinition.BatchesMaxSize;
            }

            var message = $"SetPass Calls {setPassCalls} / {maxSetPass}, Batches {batches} / {maxBatches}";
            if (setPassCalls > maxSetPass || batches > maxBatches)
            {
                // エラーフラグを建てる
                var editorInfo = AssetUtility.EditorPlayInfoData;
                editorInfo.ErrorFlag = true;
                editorInfo.Save();
                
                if (setPassCalls > maxSetPass)
                    Debug.LogError(
                        $"[<color=green>VketTools</color>] SetPass Calls <color=#ff0000ff><size=30>{setPassCalls}</size></color> is over Regulation: {maxSetPass}");
                if (batches > maxBatches)
                    Debug.LogError(
                        $"[<color=green>VketTools</color>] Batches <color=#ff0000ff><size=30>{batches}</size></color> is over Regulation: {maxBatches}");

                // こちらは画面領域が狭いので記号で対応
                message = "⚠ " + message;
            }

            _currentSeq = DraftSequence.SetPassCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Complete, message);
            editorPlayInfo.SetPassSuccessFlag = true;
            editorPlayInfo.Save();

            await ScreenShotPostprocessingTask(cancellationToken);
        }

        #region スクリーンショットステータス設定

        /// <summary>
        /// スクリーンショット撮影前処理
        /// </summary>
        /// <param name="cancellationToken"></param>
        static async UniTask ScreenShotPreprocessingTask(CancellationToken cancellationToken)
        {
            _currentSeq = DraftSequence.ScreenShotCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Running);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        /// <summary>
        /// スクリーンショット撮影後処理
        /// </summary>
        static async UniTask ScreenShotPostprocessingTask(CancellationToken cancellationToken)
        {
            _currentSeq = DraftSequence.ScreenShotCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Complete);
            await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime, cancellationToken: cancellationToken);
        }

        #endregion // スクリーンショットステータス設定
        
        #endregion // SetPassチェック & スクリーンショット撮影
        
        #region 入稿タスク
        
        #region 入稿メインタスク

        /// <summary>
        /// 入稿ボタンが押された後~Playモードに入るまで
        /// ①ベイクチェック, ②VRChatチェック, ③ルールチェック, ④ビルドサイズチェック
        /// </summary>
        private static async UniTask DraftFirst()
        {
            // 入稿前タスク1の実行
            try
            {
                VketBlinderToolbar.IsBlind = true;
                _cts = new CancellationTokenSource();
                // ①ベイクチェック, ②VRChatチェック, ③ルールチェック, ④ビルドサイズチェック
                _currentUniTask = DraftFirstGroupTask(_cts.Token);
                await _currentUniTask;
                Debug.Log("入稿タスク1終了");
            }
            catch (OperationCanceledException e)
            {
                // キャンセル時に呼ばれる例外
                VketBlinderToolbar.IsBlind = false;
                Debug.Log($"入稿タスク1をキャンセルしました。{e}");
                EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.DraftTask.Cancel.Title"), /* "入稿をキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.DraftTask.Cancel.Message"), "OK");
            }
        }
        
        /// <summary>
        /// Playモードに入ってから~Playモード終了まで
        /// ⑤セットパス確認, ⑥スクリーンショット撮影
        /// </summary>
        static async UniTask DraftSecond()
        {
            try
            {
                VketBlinderToolbar.IsBlind = true;
                _cts = new CancellationTokenSource();
                // ⑤セットパス確認, ⑥スクリーンショット撮影
                _currentUniTask = DraftSetPassCheckPreprocessingTask(_cts.Token);
                await _currentUniTask;
                Debug.Log("セットパス確認タスク終了");
            }
            catch (Exception e)
            {
                DraftExitFunc();
                // キャンセル時に呼ばれる例外
                VketBlinderToolbar.IsBlind = false;
                Debug.Log($"セットパス確認タスクをキャンセルしました。{e}");
                EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.DraftTask.Cancel.Title"), /* "入稿をキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.DraftTask.Cancel.Message"), "OK");
            }
        }
        
        /// <summary>
        /// Playモード終了から入稿完了まで
        /// 入稿タスク3
        /// </summary>
        private static async UniTask DraftThird()
        {
            try
            {
                _cts = new CancellationTokenSource();
                await UniTask.Yield(_cts.Token);
                VketBlinderToolbar.IsBlind = true;
                _currentUniTask = ExportAndUpload(_cts.Token);
                await _currentUniTask;
                Debug.Log("入稿タスク3終了");
            }
            catch (OperationCanceledException e)
            {
                // キャンセル時に呼ばれる例外
                VketBlinderToolbar.IsBlind = false;
                Debug.Log($"入稿タスク3をキャンセルしました。{e}");
                EditorUtility.DisplayDialog( /* "キャンセル" */ AssetUtility.GetMain("Vket_ControlPanel.DraftTask.Cancel.Title"), /* "入稿をキャンセルしました。" */ AssetUtility.GetMain("Vket_ControlPanel.DraftTask.Cancel.Message"), "OK");
            }
        }

        #endregion // 入稿メインタスク
        
        #region 入稿逐次処理
        
        /// <summary>
        /// キャンセルで中断できるように入稿ボタンが押された後の処理をひとまとまりのタスクとして定義
        /// </summary>
        /// <param name="cancellationToken"></param>
        static async UniTask DraftFirstGroupTask(CancellationToken cancellationToken)
        {
            // セットアップ
            CopyProxyToUdon(SceneManager.GetActiveScene());
            // シーケンス状況の初期化
            VketSequenceDrawer.ResetSequence();
            
            // 入稿チェックフラグの初期化
            var editorInfo = AssetUtility.EditorPlayInfoData;
            editorInfo.ResetDraftFlags();

            var info = AssetUtility.LoginInfoData;
            try
            {
                await DraftBakeCheckFunc(cancellationToken);
                await DraftVrcCheckFunc(cancellationToken);
                await DraftRuleCheckFunc(cancellationToken);
                await DraftBuildSizeCheckTask(cancellationToken, info);
            }
            catch (Exception)
            {
                DraftExitFunc();
                throw;
            }
        }
        
        #region 出力とアップロード
        
        /// <summary>
        /// 入稿処理
        /// </summary>
        private static async UniTask ExportAndUpload(CancellationToken cancellationToken)
        {
            // ウィンドウが開いていない場合は開きなおす。
            OpenWindowIfClose();
            
            if (CircleNullOrEmptyCheck())
            {
                ExitExportAndUpload();
                throw new OperationCanceledException("Failed Circle Null Or Empty Check");
            }

            var info = AssetUtility.LoginInfoData;
            var editorPlayInfo = AssetUtility.EditorPlayInfoData;
            try
            {
                // ⑤セットパス確認後処理、⑥スクリーンショット撮影後処理
                await DraftSetPassCheckPostprocessingTask(cancellationToken, editorPlayInfo);
                // ⑦アップロード開始処理
                var exportFilePath = await DraftUploadPreprocessingTask(cancellationToken, editorPlayInfo);

                if (editorPlayInfo.BuildSizeSuccessFlag && editorPlayInfo.SetPassSuccessFlag
                                                        && editorPlayInfo.SsSuccessFlag)
                {
                    // ⑦データアップロード処理
                    if (await DraftUploadTask(info, exportFilePath))
                    {
                        // ⑧入稿情報アップロード処理
                        await DraftUploadPostprocessingTask(cancellationToken, info, exportFilePath, editorPlayInfo);
                    }
                    else
                    {
                        Debug.LogError("#00-0005");
                        EditorUtility.DisplayDialog("Error", "Upload Failed.\r\n#00-0005", "OK");
                        throw new OperationCanceledException("Upload Failed.");
                    }

                    await UniTask.Delay(TimeSpan.FromSeconds(1f), DelayType.Realtime, cancellationToken: cancellationToken);
                }
                else
                {
                    // ビルドサイズチェックエラー、セットパスチェックエラー、スクリーンショットエラーが出る場合は通ることになるが無いはず
                    Debug.LogWarning("ビルドサイズチェック、セットパスチェック、スクリーンショットのいずれかに失敗しています。");
                }
            }
            finally
            {
                ExitExportAndUpload();
            }

            void ExitExportAndUpload()
            {
                RestoreClientSimEnabled();
                DraftExitFunc();
            }
        }
        
        /// <summary>
        /// ⑦-1アップロード前処理開始
        /// </summary>
        /// <param name="cancellationToken"></param>
        static async UniTask<string> DraftUploadPreprocessingTask(CancellationToken cancellationToken, EditorPlayInfo editorPlayInfo)
        {
            _currentSeq = DraftSequence.UploadCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Running);
            await UniTask.Delay(TimeSpan.FromSeconds(1.84f), DelayType.Realtime, cancellationToken: cancellationToken);
            
            const string exportFolderPath = "Assets/Vket_Exports";
            if (!Directory.Exists(exportFolderPath))
            {
                Directory.CreateDirectory(exportFolderPath);
                AssetDatabase.Refresh();
            }

            var exportFilePath = $"{exportFolderPath}/export.unitypackage";
            ExportBoothPackage($"Assets/{GetExhibitorID()}", exportFilePath, false, ExportPackageOptions.Default);
            
            // 出力したパッケージのバイナリデータからハッシュ値を取得し、パッケージの名称とする。
            var newPath = $"{exportFolderPath}/{GetHash()}.unitypackage";
            if (File.Exists(newPath)) File.Delete(newPath);
            File.Move(exportFilePath, newPath);
            exportFilePath = newPath;
            
            string GetHash()
            {
                using var fs = new FileStream(exportFilePath, FileMode.Open, FileAccess.Read);
                return BitConverter.ToString(SHA1.Create().ComputeHash(fs))
                                                .Replace("-", "").ToLower();
            }
            
            if (exportFilePath == null || string.IsNullOrEmpty(editorPlayInfo.SsPath))
            {
                EditorUtility.DisplayDialog("Error", /* 問題が発生した為、入稿を中断しました。 */AssetUtility.GetMain("Vket_ControlPanel.ExportAndUpload.InterruptedSubmission"), "OK");
                throw new OperationCanceledException("Submission interrupted due to an error");
            }

            if (AssetUtility.VersionInfoData.Type == VersionInfo.PackageType.develop)
                return exportFilePath;

            var info = AssetUtility.LoginInfoData;
            if (!info.IsOwner)
            {
                EditorUtility.DisplayDialog("Error", /* 入稿は代表者のみが可能です。{n}入稿を中断します。 */AssetUtility.GetMain("Vket_ControlPanel.DraftButton_Click.CancelSubmission"),
                    "OK");
                throw new OperationCanceledException("Only the Circle Leader can submit.");
            }

            return exportFilePath;
        }

        /// <summary>
        /// ⑦-2アップロード処理
        /// </summary>
        /// <param name="info"></param>
        /// <param name="exportFilePath"></param>
        /// <returns></returns>
        static async UniTask<bool> DraftUploadTask(LoginInfo info, string exportFilePath)
        {
            string boothCategory = info.IsQuest ? "CrossPlatform" : "PC";

            // "Item" or "Space"
            var category = info.SelectedType;

            // 以下のObjectNameとしてアップロード
            // eventName-Space/stable_PC/worldId/circleId/fileName
            // eventName-Item/stable_CrossPlatform/worldId/circleId/fileName
            
            var objectName = $"{AssetUtility.VersionInfoData.EventName}-{category}/{AssetUtility.VersionInfoData.Type}_{boothCategory}/{info.SelectedWorld.world_id}/{GetExhibitorID()}/{Path.GetFileName(exportFilePath)}";
            var filePath = exportFilePath.Replace("Assets", Application.dataPath).Replace("/", "\\");

            if (await S3Storage.VketUploadAsync(objectName, filePath, SynchronizationContext.Current))
            {
                // アップロード終了処理
                VketSequenceDrawer.SetState(_currentSeq, Status.Complete);
                return true;
            }

            return false;
        }

        /// <summary>
        /// ⑧後処理
        /// 入稿が正常に完了したか問い合わせる
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <param name="info"></param>
        /// <param name="exportFilePath"></param>
        /// <param name="editorPlayInfo"></param>
        /// <returns></returns>
        static async UniTask DraftUploadPostprocessingTask(CancellationToken cancellationToken, LoginInfo info, string exportFilePath, EditorPlayInfo editorPlayInfo)
        {
            _currentSeq = DraftSequence.PostUploadCheck;
            VketSequenceDrawer.SetState(_currentSeq, Status.Running);
            await UniTask.Delay(TimeSpan.FromSeconds(1), DelayType.Realtime,
                cancellationToken: cancellationToken);
            
            // "item" or "space" or "avatar"
            string category;
            switch (info.SelectedType)
            {
                case LoginInfo.ExhibitType.Item:
                    category = "item";
                    break;
                case LoginInfo.ExhibitType.Space:
                {
                    if (LoginInfo.CurrentWorldDefinition.IsAvatarSubmission)
                    {
                        category = "avatar";
                        break;
                    }
                    category = "space";
                    break;
                }
                default:
                    category = "space";
                    break;
            }
            
            var param = new VketApi.SubmitResultParameters
            (AssetUtility.VersionInfoData.EventID, info.SelectedWorld.world_id,
                info.Circles[info.SelectedCircleIndex].circle_id, category,
                Path.GetFileNameWithoutExtension(exportFilePath), editorPlayInfo.ErrorFlag, 0,
                editorPlayInfo.SsPath, editorPlayInfo.BuildSize, editorPlayInfo.SetPassCalls, editorPlayInfo.Batches);

            VketApi.SubmitResultResponse submitResultResponse = null;
            try
            {
                if (AssetUtility.VersionInfoData.Type == VersionInfo.PackageType.stable)
                {
                    submitResultResponse = await VketApi.UploadResult(param, info.Authentication.AccessToken);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("入稿失敗");
                Debug.LogError(e);
                EditorUtility.DisplayDialog( /*入稿*/
                    AssetUtility.GetMain(
                        "Vket_ControlPanel.SubmissionButton"), /* 入稿に失敗しました。\r\n入稿データを確認してからもう一度お試しください。 */
                    AssetUtility.GetMain("Vket_ControlPanel.ExportAndUpload.FailedSubmission"), "OK");
                throw new OperationCanceledException("Submission failed.");
            }
            
            // ⑨アップロード処理終了
            VketSequenceDrawer.SetState(_currentSeq, Status.Complete);
            await UniTask.Delay(TimeSpan.FromSeconds(0.5f), DelayType.Realtime,
                cancellationToken: cancellationToken);

            if (editorPlayInfo.ErrorFlag)
            {
                EditorUtility.DisplayDialog(
                    AssetUtility.GetMain("Vket_ControlPanel.SubmissionButton"), /* 入稿 */
                    AssetUtility.GetMain("Vket_ControlPanel.ExportAndUpload.ErrorMessage") /*"エラーのある状態でアップロードしました。\nエラー修正後再度入稿してください。"*/,
                    "OK");
            }
            else
            {
                if (submitResultResponse == null)
                {
                    EditorUtility.DisplayDialog(
                        AssetUtility.GetMain("Vket_ControlPanel.SubmissionButton"), /*入稿*/
                        AssetUtility.GetMain("Vket_ControlPanel.ExportAndUpload.CompleateSubmission"), /* 入稿完了しました。\r\n公式サイトのマイページから「入稿管理ページ」をご確認ください。 */
                        "OK");
                }
                else
                {
                    if (EditorUtility.DisplayDialog(
                            AssetUtility.GetMain("Vket_ControlPanel.SubmissionButton"), /*入稿*/
                            AssetUtility.GetMain("Vket_ControlPanel.ExportAndUpload.CompleateSubmission"), /* 入稿完了しました。\r\n公式サイトのマイページから「入稿管理ページ」をご確認ください。 */
                            "OK"))
                    {
                        // Debug.Log($"下見ワールドリンク: {submitResultResponse.preview_world_link}");
                        if(string.IsNullOrEmpty(submitResultResponse.preview_world_link))
                            Application.OpenURL(submitResultResponse.preview_world_link);
                    }
                }
                
            }
        }
        
        #endregion // 出力とアップロード

        #endregion // 入稿逐次処理

        /// <summary>
        /// 入稿処理完了時に初期化する処理
        /// </summary>
        private static void DraftExitFunc()
        {
            VketBlinderToolbar.IsBlind = false;
            _currentSeq = DraftSequence.None;
            if (_cts != null)
            {
                _cts.Dispose();
                _cts = null;
            }

            _currentUniTask = default;
            AssetUtility.EditorPlayInfoData.ResetDraftFlags();
            VketSequenceDrawer.ResetSequence();

            // Pedestalだった場合は元に戻すため更新する
            LoginInfo info = AssetUtility.LoginInfoData;
            if (info.IsItem)
            {
                UpdateItemType();
                // シーン保存
                Scene scene = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }
        
        #endregion // 入稿タスク

        #endregion // タスク処理

        /// <summary>
        /// SDKのBuilderタブを開く
        /// </summary>
        private static async UniTask OpenSDKBuilderTabTask()
        {
            // VRCSdkControlPanelが開くまで待つ
            await UniTask.Delay(1000, DelayType.Realtime);

            // 開かれていない場合はスルーする
            if (HasOpenInstances<VRCSdkControlPanel>())
            {
                // タイムアウト
                var timeoutController = new TimeoutController();
                var timeoutToken = timeoutController.Timeout(TimeSpan.FromSeconds(7));

                // SDKにログインするまで待つ
                try
                {
                    await UniTask.WaitUntil(() => APIUser.IsLoggedIn, cancellationToken: timeoutToken);
                }
                catch (Exception)
                {
                    // 例外を握りつぶす
                    Debug.LogError("Time out OpenSDKBuilderTab");
                    return;
                }

                OpenSDKBuilderTab();
            }
            else
            {
                Debug.LogError("Not Found VRCSdkControlPanel");
            }
        }

        /// <summary>
        /// SDKタブを開く
        /// コントロールパネルが開いており、SDKにログインしていること前提の処理
        /// </summary>
        private static void OpenSDKBuilderTab()
        {
            MethodInfo info = null;
            try
            {
                info = typeof(VRCSdkControlPanel).GetMethod("RenderTabs", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            catch (Exception)
            {
                Debug.LogError("Exception:Not Found VRCSdkControlPanel.RenderTabs Method");
            }

            if (info != null)
            {
                VRCSettings.ActiveWindowPanel = 1;
                info.Invoke(GetWindow<VRCSdkControlPanel>(), null);
            }
            else
            {
                Debug.LogError("Not Found VRCSdkControlPanel.RenderTabs Method");
            }
        }
        
        private static void CopyProxyToUdon(Scene scene)
        {
            var udonSharpComponents = scene.GetRootGameObjects()
                                           .SelectMany(root => root.GetComponentsInChildren<UdonSharpBehaviour>(true));
            foreach (var udonSharpComponent in udonSharpComponents)
            {
                UdonSharpEditorUtility.CopyProxyToUdon(udonSharpComponent);
                EditorUtility.SetDirty(UdonSharpEditorUtility.GetBackingUdonBehaviour(udonSharpComponent));
            }
        }
        
        private static string GetValidatorRule()
        {
            if (!LoginInfo.CurrentWorldDefinition)
                return DefaultRuleSet;
            
            return LoginInfo.CurrentWorldDefinition.ValidatorRuleSet;
        }

        private static string GetExportSetting()
        {
            if (!LoginInfo.CurrentWorldDefinition)
                return DefaultExportSetting;

            return LoginInfo.CurrentWorldDefinition.ExportSetting;
        }

        private static void Logout()
        {
            if (EditorPlayCheck())
            {
                return;
            }

            AssetUtility.LoginInfoData.Logout();
        }

        private static bool UpdateCheck()
        {
            if (AssetUtility.VersionInfoData.DLLType == VersionInfo.DllType.Develop
                || AssetUtility.VersionInfoData.DLLType == VersionInfo.DllType.ApiDummy
                || Utilities.Hiding.HidingUtil.DebugMode)
                return true;
            
            if (!UpdateUtility.UpdateCheck())
            {
                EditorUtility.DisplayDialog("Error", /* アップデートを行ってから実行してください。 */AssetUtility.GetMain("Vket_ControlPanel.UpdateCheck.RequestUpdate"), "OK");
                return false;
            }

            return true;
        }

        private static bool EditorPlayCheck()
        {
            if (EditorApplication.isPlaying)
            {
                if (EditorUtility.DisplayDialog("Error", /* Editorを再生中は実行できません。 */AssetUtility.GetMain("Vket_ControlPanel.EditorPlayCheck.CannotExecute"),
                        "OK"))
                    EditorApplication.isPlaying = false;
                return true;
            }

            return false;
        }

        /// <summary>
        /// サークル情報があるかチェック
        /// falseでの運用を想定
        /// </summary>
        /// <returns>サークルが存在しない場合trueを返す</returns>
        private static bool CircleNullOrEmptyCheck()
        {
            switch (AssetUtility.VersionInfoData.Type)
            {
                case VersionInfo.PackageType.stable:
                    return AssetUtility.LoginInfoData.Circles == null;
                default:
                    return false;
            }
        }

        private static void SetExportValidateLog(Type type, object instance, ValidatedExportResult result,
            string baseFolderPath, string ruleSetName)
        {
            string header = string.Format("- version:{0}", ProductInfoUtility.GetVersion()) +
                            Environment.NewLine;
            header += string.Format("- Rule set:{0}", ruleSetName) + Environment.NewLine;
            header += string.Format("- Base folder:{0}", baseFolderPath) + Environment.NewLine;
            string log = header + result.GetValidationLog() + result.GetExportLog() + result.log;
            ReflectionUtility.InvokeMethod(type, "SetMessages", instance, new object[] { header, result });
            ReflectionUtility.InvokeMethod(type, "OutLog", instance, new object[] { log });
            ReflectionUtility.InvokeMethod(type, "OutLog", instance, new object[] { "Export completed." });
        }

        // どのテンプレートを読み込んでも固定だったので
        private static float GetBuildSizeOffset() => 0.07074451f;

        // 背景か何かで、空シーンでも固定で2取っていくので軽減
        private static int GetSetPassCallsOffset() => 2;
        private static int GetBatchesOffset() => 2;

        private static float BuildSizeZeroAdjust(float cmpSize, float offset)
        {
            if (cmpSize == -1) return cmpSize;
            float original = cmpSize;
            // 浮動小数点数の誤差や、環境の違いによる数値のずれを丸めで吸収する
            float adjusted = Mathf.Max(0, (float)Math.Round(cmpSize - GetBuildSizeOffset() - offset, 5));
            Debug.Log($"vketscene.vrcw fileSize: {original}MiB (zero-adjusted: {adjusted}MiB)");
            return adjusted;
        }

        private static string GetRuleURL()
        {
            return LoginInfo.CurrentWorldDefinition ? LoginInfo.CurrentWorldDefinition.GetRuleURL() : string.Empty;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            var editorInfo = AssetUtility.EditorPlayInfoData;
            switch (state)
            {
                case PlayModeStateChange.EnteredPlayMode:
                {
                    // 入稿処理orセットパス確認中の場合
                    if (editorInfo.IsVketEditorPlay)
                    {
                        EditorApplication.update += SetPassGameViewUpdate;
                        SetPassCheckIntermediateProcessingTask().Forget();
                    }
                    else
                    {
                        // 表示フラグが立っている場合はダイアログを表示する。
                        if (editorInfo.IsShowPlayModeNotification &&
                            EditorUtility.DisplayDialog("Warning", /* "Vketでは専用のプログラムで動作させるため、UnityEditor上での確認では入稿物の動作が保証されません。" */ AssetUtility.GetMain("Vket_ControlPanel.OnPlayModeStateChanged.PlayModeNotification.Message"), /* "次回以降表示しない" */ AssetUtility.GetMain("Vket_ControlPanel.OnPlayModeStateChanged.PlayModeNotification.NotDisplayNext"), "OK"))
                        {
                            editorInfo.IsShowPlayModeNotification = false;
                            editorInfo.Save();
                        }
                    }
                }
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                {
                    // 入稿処理orセットパス確認中の場合
                    if (editorInfo.IsVketEditorPlay)
                    {
                        EditorApplication.update -= SetPassGameViewUpdate;
                    }
                }
                    break;
                case PlayModeStateChange.ExitingEditMode:
                {
                    CloseWindowIfOpen();
                }
                    break;
                case PlayModeStateChange.EnteredEditMode:
                {
                    if (editorInfo.IsVketEditorPlay)
                    {
                        SetPassCheckPostprocessingFunc(editorInfo);
                    }
                }
                    break;
            }
        }

        private static bool GetClientSimEnabled()
        {
            try
            {
                var settings = Assembly.Load("VRC.ClientSim").GetType("VRC.SDK3.ClientSim.ClientSimSettings");
                var instance = settings.GetProperty("Instance").GetMethod.Invoke(null, new object[] { });
                var enabled = (bool)settings.GetField("enableClientSim").GetValue(instance);
                Debug.Log($"ClientSimEnabled: {enabled}");
                return enabled;
            }
            catch
            {
            }

            return true;
        }

        private static void SetClientSimEnabled(bool enabled)
        {
            try
            {
                var settings = Assembly.Load("VRC.ClientSim").GetType("VRC.SDK3.ClientSim.ClientSimSettings");
                var instance = settings.GetProperty("Instance").GetMethod.Invoke(null, new object[] { });
                settings.GetField("enableClientSim").SetValue(instance, enabled);
                settings.GetMethod("SaveSettings").Invoke(null, new[] { instance });
            }
            catch
            {
            }
        }

        private static void RestoreClientSimEnabled()
        {
            var flag = AssetUtility.EditorPlayInfoData.ClientSimEnabledRestoreFlag;
            SetClientSimEnabled(flag);
            Debug.Log($"ClientSimEnabled: restored to {flag}");
        }

        /// <summary>
        /// VRChatのパスが間違えていないか判定
        /// </summary>
        /// <returns>
        /// 間違えていない場合true
        /// VRChatをインストールしていない場合はfalseを返す
        /// </returns>
        /// <exception cref="Exception">VRCSDKにログインできていない場合は例外を投げる</exception>
        private static bool ExistVRCClient()
        {
            if (!APIUser.IsLoggedIn)
                throw new OperationCanceledException("VRCHAT SDK is not Login!");

            // EditorPrefsからVRChat.exeのパスを取得
            var clientInstallPath = SDKClientUtilities.GetSavedVRCInstallPath();

            // 空文字の場合はデフォルトパスを取得
            if (string.IsNullOrEmpty(clientInstallPath))
                clientInstallPath = SDKClientUtilities.LoadRegistryVRCInstallPath();

            // 判定
            return File.Exists(clientInstallPath);
        }

        /// <summary>
        /// 一時的に保存するWindowレイアウトのファイル名
        /// </summary>
        private static readonly string TempLayoutFileName = "TempLayout.wlt";

        /// <summary>
        /// Windowレイアウトの保存
        /// SetPassCallチェック時のPlayモード切替時に呼ぶ想定
        /// </summary>
        private static void SaveEditorLayout()
        {
            Debug.Log("SaveEditorLayout Start");
            var layout = Type.GetType("UnityEditor.WindowLayout,UnityEditor");
            
            //C:/Users/{UserName}/AppData/Roaming/Unity/Editor-5.x/Preferences\Layouts
            var directoryPath = layout.GetProperty("layoutsPreferencesPath", BindingFlags.NonPublic | BindingFlags.Static)
                                      .GetValue(null) as string;
            
            if (!Directory.Exists(directoryPath))
                Directory.CreateDirectory(directoryPath);
            
            var assetPath = $"{directoryPath}/{TempLayoutFileName}";
            var method = layout.GetMethod("SaveWindowLayout",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[] { typeof(string) },
                null);
            method.Invoke(null, new object[] { assetPath });

            Debug.Log("SaveEditorLayout End");
        }

        /// <summary>
        /// Windowレイアウトの読み込み
        /// SetPassCallチェック時のEditorモード切替時に呼ぶ想定
        /// </summary>
        private static void LoadEditorLayout()
        {
            Debug.Log("LoadEditorLayout Start");
            var layout = Type.GetType("UnityEditor.WindowLayout,UnityEditor");
            var directoryPath = layout.GetProperty("layoutsPreferencesPath", BindingFlags.NonPublic | BindingFlags.Static)
                                      .GetValue(null) as string;
            var assetPath = $"{directoryPath}/{TempLayoutFileName}";
            var method = layout.GetMethod("LoadWindowLayout",
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null,
                new Type[] { typeof(string), typeof(bool) }, null);
            method.Invoke(null, new object[] { assetPath, false });
            Debug.Log("LoadEditorLayout End");
        }

        /// <summary>
        /// Windowレイアウトの削除
        /// </summary>
        private static void DeleteEditorLayout()
        {
            var assetPath = Path.Combine(Path.Combine(InternalEditorUtility.unityPreferencesFolder, "Layouts"),
                TempLayoutFileName);
            File.Delete(assetPath);
            InternalEditorUtility.ReloadWindowLayoutMenu();
        }
        
        #region VitDeckブース範囲チェック上書きPatch

        [InitializeOnLoad]
        public static class VitDeckExhibitorGUIPatch
        {
            static VitDeckExhibitorGUIPatch()
            {
                Harmony harmony = new Harmony("Shatoo_VitDeckExhibitorGUIPatch");
                harmony.PatchAll();
                MethodBase originMethod = typeof(ValidatorWindow).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo overrideMethod = typeof(VitDeckExhibitorGUIPatch).GetMethod(nameof(PostProcess), BindingFlags.NonPublic | BindingFlags.Static);
                harmony.Patch(originMethod, null, new HarmonyMethod(overrideMethod));
            }

            static void PostProcess()
            {
                // OnValidateではUndo.RevertAllInCurrentGroupでGizmo用のMonoBehaviourがリセットされるためもう一度BoothBoundsルールを発火することでGizmoを表示
                DrawBoundsLimitGizmos(SceneManager.GetActiveScene());
            }
        }
        
        [InitializeOnLoad]
        public static class VitDeckExhibitorGUIPatch2
        {
            static VitDeckExhibitorGUIPatch2()
            {
                Harmony harmony = new Harmony("SimpleValidatorWindowPatch");
                harmony.PatchAll();
                MethodBase originMethod = typeof(SimpleValidatorWindow).GetMethod("Validate", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo overrideMethod = typeof(VitDeckExhibitorGUIPatch2).GetMethod(nameof(PostProcess), BindingFlags.NonPublic | BindingFlags.Static);
                harmony.Patch(originMethod, null, new HarmonyMethod(overrideMethod));
            }

            static void PostProcess()
            {
                // OnValidateではUndo.RevertAllInCurrentGroupでGizmo用のMonoBehaviourがリセットされるためもう一度BoothBoundsルールを発火することでGizmoを表示
                DrawBoundsLimitGizmos(SceneManager.GetActiveScene());
            }
        }

        #endregion // VitDeckブース範囲チェック上書きPatch
    }
}
#endif