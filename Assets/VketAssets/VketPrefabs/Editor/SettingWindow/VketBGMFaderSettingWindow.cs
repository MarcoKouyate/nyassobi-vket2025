using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    public class VketBGMFaderSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        private VketBGMFader _vketBGMFader;

        private AudioSource _audioSource;
        private AudioClip _clip;
        private float _fadeInTime;
        private float _fadeOutTime;
        private float _fadeInVolumeRatio;
        private float _fadeOutVolumeRatio;
        private bool _onBoothFading;

        #endregion

        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 430f);

            if (_vketPrefabInstance)
            {
                _vketBGMFader = _vketPrefabInstance.GetComponent<VketBGMFader>();
            }

            if (_vketBGMFader)
            {
                _audioSource = _vketBGMFader.GetProgramVariable("_audioSource") as AudioSource;
                _fadeInTime = (float)_vketBGMFader.GetProgramVariable("_fadeInTime");
                _fadeOutTime = (float)_vketBGMFader.GetProgramVariable("_fadeOutTime");
                _fadeInVolumeRatio = (float)_vketBGMFader.GetProgramVariable("_fadeInVolumeRatio");
                _fadeOutVolumeRatio = (float)_vketBGMFader.GetProgramVariable("_fadeOutVolumeRatio");
                _onBoothFading = (bool)_vketBGMFader.GetProgramVariable("_onBoothFading");

                if (_audioSource)
                    _clip = _audioSource.clip;
            }
        }

        private void OnGUI()
        {
            InitStyle();

            if (!BaseHeader("VketBGMFader"))
                return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);

            /* "1.音源のフェードインにかける時間の設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketBGMFaderSettingWindow.FadeInTimeSetting"), _settingItemStyle);
            EditorGUI.BeginChangeCheck();
            _fadeInTime = Mathf.Max(0f, EditorGUILayout.FloatField(_fadeInTime));
            if (EditorGUI.EndChangeCheck() && _vketBGMFader)
            {
                _vketBGMFader.SetProgramVariable("_fadeInTime", _fadeInTime);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketBGMFader);
            }

            GUILayout.Space(3);

            /* "2.BGMのフェードアウトにかける時間の設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketBGMFaderSettingWindow.FadeOutTimeSetting"), _settingItemStyle);
            EditorGUI.BeginChangeCheck();
            _fadeOutTime = Mathf.Max(0f, EditorGUILayout.FloatField(_fadeOutTime));
            if (EditorGUI.EndChangeCheck() && _vketBGMFader)
            {
                _vketBGMFader.SetProgramVariable("_fadeOutTime", _fadeOutTime);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketBGMFader);
            }

            GUILayout.Space(3);

            /* "3.音源のフェードイン音量の設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketBGMFaderSettingWindow.FadeInVolumeRatioSetting"), _settingItemStyle);
            EditorGUI.BeginChangeCheck();
            _fadeInVolumeRatio = Mathf.Max(0f, EditorGUILayout.FloatField(_fadeInVolumeRatio));
            if (EditorGUI.EndChangeCheck() && _vketBGMFader)
            {
                _vketBGMFader.SetProgramVariable("_fadeInVolumeRatio", _fadeInVolumeRatio);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketBGMFader);
            }

            GUILayout.Space(3);

            /* "4.BGMのフェードアウト音量の設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketBGMFaderSettingWindow.FadeOutVolumeRatioSetting"), _settingItemStyle);
            EditorGUI.BeginChangeCheck();
            _fadeOutVolumeRatio = Mathf.Max(0f, EditorGUILayout.FloatField(_fadeOutVolumeRatio));
            if (EditorGUI.EndChangeCheck() && _vketBGMFader)
            {
                _vketBGMFader.SetProgramVariable("_fadeOutVolumeRatio", _fadeOutVolumeRatio);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketBGMFader);
            }

            GUILayout.Space(3);

            /* "5.プレイヤーがブースに接近した時に自動的にフェードする場合はチェック" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketBGMFaderSettingWindow.OnBoothFadeSetting"), _settingItemStyle);
            EditorGUI.BeginChangeCheck();
            _onBoothFading = EditorGUILayout.Toggle(_onBoothFading);
            if (EditorGUI.EndChangeCheck() && _vketBGMFader)
            {
                _vketBGMFader.SetProgramVariable("_onBoothFading", _onBoothFading);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketBGMFader);
            }

            GUILayout.Space(3);

            /* "6.入れ替えで再生する音源を設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketBGMFaderSettingWindow.AudioClipSetting"), _settingItemStyle);
            EditorGUI.BeginChangeCheck();
            _clip = EditorGUILayout.ObjectField(_clip, typeof(AudioClip), false) as AudioClip;
            if (EditorGUI.EndChangeCheck())
            {
                _audioSource.clip = _clip;
                PrefabUtility.RecordPrefabInstancePropertyModifications(_audioSource);
            }

            /* "音源を設定しておくとワールドBGMと入れ替わる形で音源をフェードします。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketBGMFaderSettingWindow.AudioClipSetting.Help"), MessageType.Info);

            EditorGUILayout.EndScrollView();

            BaseFooter("VketBGMFader");
        }
    }
}
