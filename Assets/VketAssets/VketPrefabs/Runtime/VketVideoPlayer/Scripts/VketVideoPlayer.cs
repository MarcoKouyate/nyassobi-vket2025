
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;
using VRC.SDK3.Components.Video;
using VRC.SDK3.Video.Components.Base;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketVideoPlayer : UdonSharpBehaviour
    {
        [SerializeField]
        private VRCUrl videoUrl;
        // Settings
        [SerializeField]
        private bool worldBgmFade = true;
        [SerializeField]
        private bool onBoothPlay;
        [SerializeField]
        private Texture2D disabledImage;
        [SerializeField]
        private Texture2D loadingImage;
        // References
        [SerializeField]
        private BaseVRCVideoPlayer videoPlayer;
        [SerializeField]
        private MeshRenderer screen;
        [SerializeField]
        private AudioSource audioSource;
        // Interface
        [SerializeField]
        private GameObject isPlayActiveObject;
        [SerializeField]
        private GameObject isStopActiveObject;
        [SerializeField]
        private Text durationText;
        [SerializeField]
        private Text currentTimeText;
        [SerializeField]
        private Slider seekSlider;
        [SerializeField]
        private Slider volumeSlider;
        [SerializeField]
        private RectTransform seekbarRectTransform;
        [SerializeField]
        private GameObject seekbarTextRootObject;
        // Auto Setup
        [SerializeField]
        private AudioSource[] bgmAudioSources;

        private readonly float errorRequestTime = 1.5f;
        private readonly float fadingTime = 1.0f;

        private VRCPlayerApi localPlayer;
        private Transform audioTransform;
        private byte errorCount = 0;
        private float maxDistance;
        private float initAudioVolume;
        private float[] initBgmAudioVolumes;
        private float fadeOutValue = 1.0f;
        private float duration;
        private float currentTime;
        private bool isLoading;
        private bool isFading;
        private bool isInArea;
        private bool isCounting;
        private bool isPlayerInternalsDirty = false;
        
        public float Duration => duration;
        public float CurrentTime
        {
            get => videoPlayer.GetTime();
            set
            {
                videoPlayer.SetTime(value);
                if (currentTimeText || seekbarRectTransform)
                {
                    float time = videoPlayer.GetTime();
                    if(currentTimeText)
                        currentTimeText.text = ConvertTimeToText(time);
                    if(seekbarRectTransform)
                        seekbarRectTransform.anchorMax = new Vector2(time / duration, 1.0f);
                }
            }
        }

        public VRCUrl VideoUrl
        {
            get => videoUrl;
            set
            {
                if (value.ToString() == null || videoUrl == value)
                    return;

                videoUrl = value;
                _StopVideo();
            }
        }

        public void _VketStart()
        {
            if(screen)
                screen.material.mainTexture = disabledImage;

            initAudioVolume = audioSource.volume;

            if (worldBgmFade)
            {
                initBgmAudioVolumes = new float[bgmAudioSources.Length];
                for (int i = 0; i < bgmAudioSources.Length; i++)
                    initBgmAudioVolumes[i] = bgmAudioSources[i].volume;
            }
        }

        public void _VketOnBoothEnter()
        {
            isInArea = true;

            if (onBoothPlay)
                _LoadVideo();
        }

        public void _VketOnBoothExit()
        {
            isInArea = false;

            _StopVideo();
        }

        // Audioフェード
        private void Update()
        {
            if (videoPlayer.IsPlaying)
            {
                currentTime = videoPlayer.GetTime();
                if(currentTimeText)
                    currentTimeText.text = ConvertTimeToText(currentTime);
                if(seekbarRectTransform)
                    seekbarRectTransform.anchorMax = new Vector2(currentTime / duration, 1.0f);
            }

            if (!isFading)
                return;

            for (int i = 0; i < bgmAudioSources.Length; i++)
                bgmAudioSources[i].volume = initBgmAudioVolumes[i];
        }
        private void LateUpdate()
        {
            if (!isFading)
                return;

            FadingValue();

            for (int i = 0; i < bgmAudioSources.Length; i++)
                bgmAudioSources[i].volume *= fadeOutValue;
        }
        private void FadingValue()
        {
            bool fadeFlag = false;
            if (videoPlayer.IsPlaying)
            {
                Vector3 headPos = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
                if (Vector3.Distance(headPos, audioTransform.position) < maxDistance)
                    fadeFlag = true;
            }

            if (fadeFlag)
            {
                if (fadeOutValue > 0)
                {
                    fadeOutValue -= Time.deltaTime / fadingTime;
                    if (fadeOutValue < 0)
                        fadeOutValue = 0;
                }
            }
            else
            {
                if (fadeOutValue <= 1.0f)
                {
                    fadeOutValue += Time.deltaTime / fadingTime;
                    if (fadeOutValue > 1.0f)
                    {
                        fadeOutValue = 1.0f;
                    }

                    if (!videoPlayer.IsPlaying && fadeOutValue == 1.0f)
                        isFading = false;
                }
            }
        }

        // インターフェース
        public void _CountVideoTime()
        {
            if (!videoPlayer.IsPlaying)
            {
                isCounting = false;
                return;
            }
            if(currentTimeText)
                currentTimeText.text = ConvertTimeToText(currentTime);
            SendCustomEventDelayedSeconds(nameof(_CountVideoTime), 1.0f);
        }
        public void _ChangeVolume()
        {
            if (audioSource.mute)
                return;

            audioSource.volume = initAudioVolume * volumeSlider.value;
        }
        public void _Seek()
        {
            if (!videoPlayer.IsReady)
                return;

            videoPlayer.SetTime(Mathf.Lerp(0, duration, seekSlider.value));

            float time = videoPlayer.GetTime();
            if(currentTimeText)
                currentTimeText.text = ConvertTimeToText(time);
            if(seekbarRectTransform)
                seekbarRectTransform.anchorMax = new Vector2(time / duration, 1.0f);
        }
        public void _ResetTime()
        {
            if (!videoPlayer.IsReady)
                return;

            videoPlayer.SetTime(0);
            if(currentTimeText)
                currentTimeText.text = "00:00:00";
            if(seekbarRectTransform)
                seekbarRectTransform.anchorMax = new Vector2(0, 1.0f);
        }
        public void _EnableMute()
        {
            audioSource.mute = true;
        }
        public void _DisableMute()
        {
            audioSource.mute = false;
        }
        private string ConvertTimeToText(float time)
        {
            int second = (int)(time % 60f);
            int minute = (int)(time / 60f % 60f);
            int hour = (int)(time / 3600f);
            return $"{hour:00}:{minute:00}:{second:00}";
        }

        // Video操作
        public void _LoadVideo()
        {
            if (!isInArea || isLoading)
                return;

            if (videoPlayer.IsReady && videoPlayer.GetTime() > 0 && !isPlayerInternalsDirty)
            {
                ResumeVideo();
            }
            else
            {
                isPlayerInternalsDirty = false;
                isLoading = true;
                if(screen)
                    screen.material.mainTexture = loadingImage;
                videoPlayer.LoadURL(videoUrl);
            }

            
        }
        public void _StopVideo()
        {
            isLoading = false;
            errorCount = 0;

            if (isPlayActiveObject != null)
                isPlayActiveObject.SetActive(false);
            if (isStopActiveObject != null)
                isStopActiveObject.SetActive(true);

            if (durationText != null)
                durationText.text = "00:00:00";
            if (currentTimeText != null)
                currentTimeText.text = "00:00:00";
            if (seekbarRectTransform != null)
                seekbarRectTransform.anchorMax = new Vector2(0, 1.0f);
            if (seekbarTextRootObject != null)
                seekbarTextRootObject.SetActive(false);

            videoPlayer.Stop();
            
#if UNITY_ANDROID
            isPlayerInternalsDirty = true;
#endif

            SendCustomEventDelayedSeconds(nameof(_DelaySetDisabledImage), 0.5f);
        }
        public void _DelaySetDisabledImage()
        {
            if(screen)
                screen.material.mainTexture = disabledImage;
        }
        public void _PauseVideo()
        {
            if (!videoPlayer.IsPlaying)
                return;

            if (isPlayActiveObject != null)
                isPlayActiveObject.SetActive(false);
            if (isStopActiveObject != null)
                isStopActiveObject.SetActive(true);

            videoPlayer.Pause();
        }
        private void StartVideo()
        {
            audioSource.enabled = true;

            if (worldBgmFade)
            {
                if (localPlayer == null)
                {
                    localPlayer = Networking.LocalPlayer;
                    audioTransform = audioSource.transform;
                    maxDistance = audioSource.maxDistance;
                }
                isFading = true;
            }

            if (isPlayActiveObject != null)
                isPlayActiveObject.SetActive(true);
            if (isStopActiveObject != null)
                isStopActiveObject.SetActive(false);

            if (!isCounting)
            {
                isCounting = true;
                SendCustomEventDelayedFrames(nameof(_CountVideoTime), 1);
            }
            
            if (seekbarTextRootObject != null)
            {
                seekbarTextRootObject.SetActive(true);
            }

            videoPlayer.Play();
        }
        private void ResumeVideo()
        {
            audioSource.enabled = true;

            if (isPlayActiveObject != null)
                isPlayActiveObject.SetActive(true);
            if (isStopActiveObject != null)
                isStopActiveObject.SetActive(false);

            if (!isCounting)
            {
                isCounting = true;
                SendCustomEventDelayedFrames(nameof(_CountVideoTime), 1);
            }
            
            videoPlayer.Play();
        }

        // Videoコールバック
        public override void OnVideoReady()
        {
            if (!isLoading)
                return;

            isLoading = false;
            duration = videoPlayer.GetDuration();
            if (durationText != null)
                durationText.text = ConvertTimeToText(duration);
            if (currentTimeText != null)
                currentTimeText.text = "00:00:00";

            StartVideo();
        }
        public override void OnVideoError(VideoError videoError)
        {
            if (!isLoading)
                return;

            switch (videoError)
            {
                case VideoError.PlayerError:
                case VideoError.RateLimited:
                case VideoError.AccessDenied:
                    RetryLoadingVideo();
                    break;
                default:
                    errorCount = 0;
                    _StopVideo();
                    break;
            }
            
        }
        private void RetryLoadingVideo()
        {
            if (!isInArea)
                return;

            ++errorCount;
            if (errorCount >= 10)
            {
                errorCount = 0;
                _StopVideo();
            }
            else
            {
                SendCustomEventDelayedSeconds(nameof(_AgainLoadingRequest), errorRequestTime, VRC.Udon.Common.Enums.EventTiming.Update);
            }
        }
        public override void OnVideoEnd()
        {
            //if (!videoPlayer.Loop)
                _StopVideo();
        }
        public void _AgainLoadingRequest()
        {
            if (!isLoading)
                return;

            videoPlayer.LoadURL(videoUrl);
        }
        public override void OnVideoLoop()
        {
            SendCustomEventDelayedSeconds(nameof(_DelayLoop), 0.5f);
        }
        public void _DelayLoop()
        {
            if (!isInArea)
                return;

            StartVideo();
        }
    }
}