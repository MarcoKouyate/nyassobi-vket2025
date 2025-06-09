using UdonSharp;
using UnityEngine;
using Vket.AudioSystem;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common.Interfaces;

namespace Vket.VketPrefabs
{
	[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
	public class VketBGMFader : UdonSharpBehaviour
	{
		[SerializeField] private float _fadeInTime = 1;
		[SerializeField] private float _fadeOutTime = 1;
		[SerializeField] private float _fadeInVolumeRatio = 1;
		[SerializeField] private float _fadeOutVolumeRatio = 0;
		[SerializeField] private bool _onBoothFading = true;
		[SerializeField] private AudioSource _audioSource;
		[SerializeField] private AudioVolumeManager _bgmVolumeManager;
		[SerializeField] private UdonBehaviour _audioLink;

		private bool _isInArea = false;
		private float _maxVolume;
		private float _tmpPlayingTime = 0;

		private void OnDisable()
		{
			_VketOnBoothExit();
		}

		public void _VketStart()
		{
			_maxVolume = _audioSource.volume;
			_audioSource.volume = 0;
		}

		public void _VketOnBoothEnter()
		{
			_isInArea = true;
			if (_onBoothFading) _StartFade();
		}

		public void _VketOnBoothExit()
		{
			_EndFade();
			_isInArea = false;
		}

		public void _StartFade()
		{
			if (!_isInArea) return;

			var isExecute = !Mathf.Approximately(_audioSource.volume, _maxVolume);
			_tmpPlayingTime = 0;
			if (isExecute) _ActiveAudio();
			_bgmVolumeManager.TriggerSource = (IUdonEventReceiver)this;
			_bgmVolumeManager._StartFade(_audioSource, _maxVolume, _fadeInTime, _fadeInVolumeRatio, _fadeOutVolumeRatio);
			_bgmVolumeManager.TriggerSource = null;
			if (isExecute) _SetAudioLink();
		}

		public void _EndFade()
		{
			if (!_isInArea) return;

			_tmpPlayingTime = _audioSource.time;
			if (!Mathf.Approximately(_audioSource.volume, 0)) SendCustomEventDelayedFrames(nameof(_ActiveAudio), 0);
			var callbacks = new AudioCallbackInfo[2];
			callbacks[0] = AudioCallbackInfo.New((IUdonEventReceiver)this, nameof(_DeactiveAudio));
			callbacks[1] = AudioCallbackInfo.New((IUdonEventReceiver)this, nameof(_ResetAudioLink));
			_bgmVolumeManager.TriggerSource = (IUdonEventReceiver)this;
			_bgmVolumeManager._EndFade(_fadeOutTime, callbacks);
			_bgmVolumeManager.TriggerSource = null;
		}

		public void _ActiveAudio()
		{
			_audioSource.enabled = true;
			if (!_audioSource.isPlaying)
			{
				if (Utilities.IsValid(_audioSource.clip)) _audioSource.Play();
				_audioSource.time = _tmpPlayingTime;
				_tmpPlayingTime = 0;
			}
		}

		public void _DeactiveAudio()
		{
			if (Utilities.IsValid(_audioSource.clip)) _audioSource.Stop();
			_audioSource.enabled = false;
		}

		public void _SetAudioLink()
		{
			if (!Utilities.IsValid(_audioLink) || !Utilities.IsValid(_audioSource.clip)) return;

			var audioSources = (AudioSource[])_audioLink.GetProgramVariable("audioSource");
			if (!Utilities.IsValid(audioSources)) audioSources = new AudioSource[0];
			var tmpArray = new AudioSource[audioSources.Length + 1];
			System.Array.Copy(audioSources, tmpArray, audioSources.Length);
			tmpArray[audioSources.Length] = _audioSource;
			_audioLink.SetProgramVariable("audioSource", tmpArray);
		}

		public void _ResetAudioLink()
		{
			if (!Utilities.IsValid(_audioLink) || !Utilities.IsValid(_audioSource.clip)) return;

			var audioSources = (AudioSource[])_audioLink.GetProgramVariable("audioSource");
			if (!Utilities.IsValid(audioSources)) return;
			var index = System.Array.IndexOf(audioSources, _audioSource);
			if (index < 0 || index >= audioSources.Length || audioSources.Length == 0) return;
			var tmpArray = new AudioSource[audioSources.Length - 1];
			if (index > 0) System.Array.Copy(audioSources, 0, tmpArray, 0, index);
			System.Array.Copy(audioSources, index + 1, tmpArray, index, tmpArray.Length - index);
			_audioLink.SetProgramVariable("audioSource", tmpArray);
		}
	}
}