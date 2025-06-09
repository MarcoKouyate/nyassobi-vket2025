
using System;
using UdonSharp;
using UnityEngine;
using VRC.Udon;

namespace Vket.VketPrefabs
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class VketSoundFade : UdonSharpBehaviour
    {
        [SerializeField]
        private float fadeInTime = 0f;
        [SerializeField]
        private bool onBoothFading;

        [SerializeField]
        private AudioSource audioSource;
        [SerializeField]
        private AudioSource[] bgmAudioSources;
        [SerializeField]
        private UdonBehaviour udonManager;
        [SerializeField]
        private UdonBehaviour audioLink;

        private readonly float fadingTime = 1.0f;

        private float[] initOutAudioVolumes;
        private float initInAudioVolume;
        private float fadeOutValue = 1.0f;
        private float fadeInValue = 0;
        private bool isFading;
        private bool isInArea;
        private bool isEnableSoundFade;
        private bool hasInitialized;

        public void _VketStart()
        {
            initOutAudioVolumes = new float[bgmAudioSources.Length];
            for (int i = 0; i < bgmAudioSources.Length; i++)
                initOutAudioVolumes[i] = bgmAudioSources[i].volume;
            initInAudioVolume = audioSource.volume;
            hasInitialized = true;
        }

        private void SetAudioLink(bool active)
        {
            if (audioLink)
            {
                if (active)
                {
                    var newAudioList = new AudioSource[bgmAudioSources.Length + 1];
                    Array.Copy(bgmAudioSources,newAudioList,bgmAudioSources.Length);
                    newAudioList[bgmAudioSources.Length] = audioSource;
                    audioLink.SetProgramVariable("audioSource", newAudioList);
                }
                else
                {
                    audioLink.SetProgramVariable("audioSource", bgmAudioSources);
                }
            }
        }

        public void _VketOnBoothEnter()
        {
            if (onBoothFading)
                InitFadeOut();

            isInArea = true;
            SetAudioLink(true);
        }

        public void _VketOnBoothExit()
        {
            InitFadeIn();

            isInArea = false;
        }

        public override void Interact()
        {
            if (!isInArea)
                return;

            if (isEnableSoundFade)
            {
                InitFadeIn();
            }
            else
            {
                udonManager.SendCustomEvent("_StopAllSoundFade");
                InitFadeOut();
            }
        }

        private void Update()
        {
            if (!isFading || !hasInitialized)
                return;

            // Volumeを初期値に設定
            for (int i = 0; i < bgmAudioSources.Length; i++)
                bgmAudioSources[i].volume = initOutAudioVolumes[i];
            audioSource.volume = initInAudioVolume;
        }

        private void LateUpdate()
        {
            if (!isFading || !hasInitialized)
                return;

            // 補正値フェード
            FadingValues();

            for (int i = 0; i < bgmAudioSources.Length; i++)
                bgmAudioSources[i].volume *= fadeOutValue;

            if (audioSource.clip == null)
                return;

            audioSource.volume *= fadeInValue;
            // 再生終了チェック
            if (fadeInValue > 0)
            {
                if (!audioSource.isPlaying)
                {
                    isEnableSoundFade = false;
                    audioSource.enabled = false;
                    fadeInValue = 0;
                }
            }
        }

        private void InitFadeOut()
        {
            isEnableSoundFade = true;
            isFading = true;

            if (fadeOutValue == 0 && audioSource.clip != null)
            {
                audioSource.enabled = true;
                audioSource.Play();
            }
        }

        private void InitFadeIn()
        {
            SetAudioLink(true);
            isEnableSoundFade = false;

            if (fadeInValue == 0 && audioSource.clip != null)
            {
                audioSource.enabled = false;
                audioSource.Stop();
            }
        }

        private void FadingValues()
        {
            if (isInArea && isEnableSoundFade)
            {
                if (fadeOutValue > 0)
                {
                    fadeOutValue -= Time.deltaTime / fadingTime;
                    if (fadeOutValue <= 0)
                    {
                        fadeOutValue = 0;

                        if (!audioSource.isPlaying && audioSource.clip != null)
                        {
                            audioSource.enabled = true;
                            audioSource.Play();
                        }
                    }
                }
                else if (fadeInValue < 1.0f)
                {
                    if (fadeInTime <= 0)
                    {
                        fadeInValue = 1.0f;
                    }
                    else
                    {
                        fadeInValue += Time.deltaTime / fadeInTime;
                        if (fadeInValue > 1.0f)
                            fadeInValue = 1.0f;
                    }
                }
            }
            else
            {
                if (fadeInValue > 0 && audioSource.clip != null)
                {
                    fadeInValue -= Time.deltaTime / fadingTime;
                    if (fadeInValue <= 0)
                    {
                        fadeInValue = 0;

                        audioSource.enabled = false;
                        audioSource.Stop();
                        SetAudioLink(false);
                    }
                }
                else if (fadeOutValue <= 1.0f)
                {
                    fadeOutValue += Time.deltaTime / fadingTime;
                    if (fadeOutValue >= 1.0f)
                    {
                        fadeOutValue = 1.0f;
                        isFading = false;
                    }
                }
            }
        }

        public void _StopSoundFade()
        {
            isEnableSoundFade = false;
        }
    }
}