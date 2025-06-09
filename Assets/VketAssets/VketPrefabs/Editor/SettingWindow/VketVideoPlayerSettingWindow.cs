
using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;
using VRC.SDK3.Video.Components.Base;
using VRC.SDKBase;

namespace Vket.VketPrefabs
{
    public class VketVideoPlayerSettingWindow : VketPrefabSettingWindow
    {
        
        #region 設定用変数
        
        private VketVideoPlayer _vketVideoPlayer;
        private BaseVRCVideoPlayer _videoPlayer;
        
        private string _videoURL;
        private bool _worldBgmFade;
        private bool _onBoothPlay;
        private Texture2D _disabledImage;
        private Texture2D _loadingImage;
        
        /// <summary>
        /// 操作パネル調整用
        /// </summary>
        private GameObject _uiGameObject;
        private bool _enabledUI;
        
        /// <summary>
        /// スクリーン調整用
        /// </summary>
        private Transform _screen;
        private Vector2Int _aspectSize;
        private float _screenScale;
        
        /// <summary>
        /// AudioSourceオブジェクト
        /// </summary>
        private Transform _audioTransform;

        #endregion
        
        #region const定義
        
        private const string UIRelativePath = "Interface";
        private const string ScreenRelativePath = "Screen";
        private const string AudioRelativePath = "AudioSource";
        
        #endregion

        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 500f);
            
            if (_vketPrefabInstance)
            {
                _vketVideoPlayer = _vketPrefabInstance.GetComponent<VketVideoPlayer>();
            }

            if (_vketVideoPlayer)
            {
                var url = _vketVideoPlayer.GetProgramVariable("videoUrl") as VRCUrl;
                _videoURL = url.ToString();
                _videoPlayer = _vketVideoPlayer.GetProgramVariable("videoPlayer") as BaseVRCVideoPlayer;
                _worldBgmFade = (bool)_vketVideoPlayer.GetProgramVariable("worldBgmFade");
                _onBoothPlay = (bool)_vketVideoPlayer.GetProgramVariable("onBoothPlay");
                _disabledImage = _vketVideoPlayer.GetProgramVariable("disabledImage") as Texture2D;
                _loadingImage = _vketVideoPlayer.GetProgramVariable("loadingImage") as Texture2D;
            }

            if (_vketPrefabInstance)
            {
                var uiTransform = _vketPrefabInstance.Find(UIRelativePath);
                if (uiTransform)
                {
                    _uiGameObject = uiTransform.gameObject;
                    _enabledUI = _uiGameObject.activeSelf;
                }

                _screen = _vketPrefabInstance.Find(ScreenRelativePath);
                _aspectSize = new Vector2Int(16, 9);
                _screenScale = 0.2f;
                
                _audioTransform = _vketPrefabInstance.Find(AudioRelativePath);
            }
        }
        
        private void OnGUI()
        {
            InitStyle();
            
            if(!BaseHeader("VketVideoPlayer"))
                return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            
            /* "1.ビデオのURLを指定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.VideoUrlSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _videoURL = EditorGUILayout.TextField(_videoURL);
            if (EditorGUI.EndChangeCheck() && _vketVideoPlayer)
            {
                _vketVideoPlayer.SetProgramVariable("videoUrl", new VRCUrl(_videoURL));
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketVideoPlayer);
            }
            
            if (!string.IsNullOrEmpty(_videoURL))
            {
                if (GUILayout.Button("Open Video Url"))
                {
                    Application.OpenURL(_videoURL);
                }
            }

            GUILayout.Space(3);
            
            /* "2.ビデオをLoopする場合はチェック" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.VideoLoopSetting"), _l3Style);

            // Draw Field of Loop
            if (_videoPlayer != null)
            {
                var so = new SerializedObject(_videoPlayer);
                so.Update();
                var loopProp = so.FindProperty("loop");
                EditorGUI.BeginChangeCheck();
                bool loop = EditorGUILayout.Toggle(loopProp.boolValue);
                if (EditorGUI.EndChangeCheck())
                {
                    loopProp.boolValue = loop;
                    so.ApplyModifiedProperties();
                }
            }
            
            GUILayout.Space(3);
            
            /* "3.再生時にワールドのBGMを減衰する場合はチェック" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.FadeBgmSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _worldBgmFade = EditorGUILayout.Toggle(_worldBgmFade);
            if (EditorGUI.EndChangeCheck())
            {
                _vketVideoPlayer.SetProgramVariable("worldBgmFade", _worldBgmFade);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketVideoPlayer);
            }
            
            GUILayout.Space(3);
            
            /* "4.プレイヤーがブースに近づいたときに自動再生する場合はチェック" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.OnEnterPlayerSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _onBoothPlay = EditorGUILayout.Toggle(_onBoothPlay);
            if (EditorGUI.EndChangeCheck())
            {
                _vketVideoPlayer.SetProgramVariable("onBoothPlay", _onBoothPlay);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketVideoPlayer);
            }
            
            GUILayout.Space(3);
            
            /* "5.操作パネルが不要な場合はチェック" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.EnabledUiSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _enabledUI = EditorGUILayout.Toggle(_enabledUI);
            if (EditorGUI.EndChangeCheck() && _uiGameObject)
            {
                _uiGameObject.SetActive(_enabledUI);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_uiGameObject);
            }
            
            GUILayout.Space(3);
            
            /* "6.スクリーンサイズの調整" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.ScreenSizeSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            
            /* "アスペクト比" */
            _aspectSize = EditorGUILayout.Vector2IntField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.ScreenSizeSetting.Aspect"), _aspectSize);
            
            /* "スクリーンサイズ調整" */
            _screenScale = EditorGUILayout.FloatField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.ScreenSizeSetting.Size"), _screenScale);
            if (EditorGUI.EndChangeCheck() && _screen)
            {
                var screenSize = new Vector2(_aspectSize.x, _aspectSize.y) * _screenScale;
                _screen.localScale = new Vector3(screenSize.x, screenSize.y, 1);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_screen);
            }
            
            GUILayout.Space(3);
            
            /* "7.動画が再生されていないときに表示する画像" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.DisabledImageSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _disabledImage = TextureField(_disabledImage);
            if (EditorGUI.EndChangeCheck())
            {
                _vketVideoPlayer.SetProgramVariable("disabledImage", _disabledImage);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketVideoPlayer);
            }
            
            GUILayout.Space(3);
            
            /* "8.動画ロード中に表示する画像" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketVideoPlayerSettingWindow.LoadingImageSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _loadingImage = TextureField(_loadingImage);
            if (EditorGUI.EndChangeCheck())
            {
                _vketVideoPlayer.SetProgramVariable("loadingImage", _loadingImage);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketVideoPlayer);
            }
            
            EditorGUILayout.EndScrollView();
            
            GUILayout.Space(5);
            
            /* "AudioSourceを選択" */
            if (GUILayout.Button(LocalizedUtility.Get("VketVideoPlayerSettingWindow.SelectAudioSourceButton")))
            {
                Selection.activeTransform = _audioTransform;
                EditorGUIUtility.PingObject(_audioTransform.gameObject);
            }
            
            /* "動画の音声の大きさや範囲を調整するオブジェクトを選択します。
             Inspectorから調整してください。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketVideoPlayerSettingWindow.SelectAudioSourceButton.Help"), MessageType.Info);
            
            GUILayout.Space(5);
            
            BaseFooter("VketVideoPlayer");
        }
    }
}
