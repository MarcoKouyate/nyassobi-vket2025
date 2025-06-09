using UnityEditor;
using UnityEngine;
using VketAssets.VketPrefabs.Editor;

namespace Vket.VketPrefabs
{
    public class VketSoundFadeSettingWindow : VketPrefabSettingWindow
    {
        #region 設定用変数
        private VketSoundFade _vketSoundFade;

        private AudioSource _audioSource;
        private AudioClip _clip;
        private float _fadeInTime;
        private bool _onBoothFading;
        
        #endregion

        protected override void InitWindow()
        {
            // ウィンドウ最小サイズの設定
            minSize = new Vector2(350f, 430f);

            if (_vketPrefabInstance)
            {
                _vketSoundFade = _vketPrefabInstance.GetComponent<VketSoundFade>();
            }
            
            if (_vketSoundFade)
            {
                _audioSource = _vketSoundFade.GetProgramVariable("audioSource") as AudioSource;
                _fadeInTime = (float)_vketSoundFade.GetProgramVariable("fadeInTime");
                _onBoothFading = (bool)_vketSoundFade.GetProgramVariable("onBoothFading");

                if (_audioSource)
                    _clip = _audioSource.clip;
            }
        }
        
         private void OnGUI()
        {
            InitStyle();
            
            if(!BaseHeader("VketSoundFade"))
                return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUI.skin.box);
            
            /* "1.音源のフェードインにかける時間の設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketSoundFadeSettingWindow.FadeInTimeSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _fadeInTime = Mathf.Max(0f, EditorGUILayout.FloatField (_fadeInTime));
            if (EditorGUI.EndChangeCheck() && _vketSoundFade)
            {
                _vketSoundFade.SetProgramVariable("fadeInTime", _fadeInTime);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketSoundFade);
            }
            
            GUILayout.Space(3);
            
            /* "2.プレイヤーがブースに接近した時に自動的にフェードする場合はチェック" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketSoundFadeSettingWindow.OnBoothFadeSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _onBoothFading = EditorGUILayout.Toggle(_onBoothFading);
            if (EditorGUI.EndChangeCheck() && _vketSoundFade)
            {
                _vketSoundFade.SetProgramVariable("onBoothFading", _onBoothFading);
                PrefabUtility.RecordPrefabInstancePropertyModifications(_vketSoundFade);
            }
            
            GUILayout.Space(3);
            
            /* "3.入れ替えで再生する音源を設定" */
            EditorGUILayout.LabelField(LocalizedUtility.Get("VketSoundFadeSettingWindow.AudioClipSetting"), _l3Style);
            EditorGUI.BeginChangeCheck();
            _clip = EditorGUILayout.ObjectField(_clip, typeof(AudioClip), false) as AudioClip;
            if (EditorGUI.EndChangeCheck())
            {
                _audioSource.clip = _clip;
                PrefabUtility.RecordPrefabInstancePropertyModifications(_audioSource);
            }
            
            /* "音源を設定しておくとワールドBGMと入れ替わる形で音源をフェードします。" */
            EditorGUILayout.HelpBox(LocalizedUtility.Get("VketSoundFadeSettingWindow.AudioClipSetting.Help"), MessageType.Info);

            EditorGUILayout.EndScrollView();
            
            BaseFooter("VketSoundFade");
        }
    }
}
