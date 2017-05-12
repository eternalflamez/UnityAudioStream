#if UNITY_4_6 || UNITY_4_7 || UNITY_4_8 || UNITY_5 || UNITY_5_4_OR_NEWER
	#define UNITY_FEATURE_UGUI
#endif

using UnityEngine;
#if UNITY_FEATURE_UGUI
using UnityEngine.UI;
using System.Collections;
using RenderHeads.Media.AVProVideo;

//-----------------------------------------------------------------------------
// Copyright 2015-2017 RenderHeads Ltd.  All rights reserverd.
//-----------------------------------------------------------------------------

namespace RenderHeads.Media.AVProVideo.Demos
{
	public class VCR : MonoBehaviour 
	{
		public MediaPlayer	_mediaPlayer;
		public MediaPlayer	_mediaPlayerB;
		public DisplayUGUI	_mediaDisplay;

		public Slider		_videoSeekSlider;
		private float		_setVideoSeekSliderValue;
		private bool		_wasPlayingOnScrub;

		public Slider		_audioVolumeSlider;
		private float		_setAudioVolumeSliderValue;

		public Toggle		_AutoStartToggle;
		public Toggle		_MuteToggle;

		public MediaPlayer.FileLocation _location = MediaPlayer.FileLocation.RelativeToStreamingAssetsFolder;
		public string _folder = "AVProVideoDemos/";
		public string[] _videoFiles = { "BigBuckBunny_720p30.mp4", "SampleSphere.mp4" };

		private int _VideoIndex = 0;

		private MediaPlayer _loadingPlayer;

		public MediaPlayer PlayingPlayer
		{
			get
			{
				if (LoadingPlayer == _mediaPlayer)
				{
					return _mediaPlayerB;
				}
				return _mediaPlayer;
			}
		}

		public MediaPlayer LoadingPlayer
		{
			get
			{
				return _loadingPlayer;
			}
		}

		private void SwapPlayers()
		{
			// Pause the previously playing video
			PlayingPlayer.Control.Pause();

			// Swap the videos
			if (LoadingPlayer == _mediaPlayer)
			{
				_loadingPlayer = _mediaPlayerB;
			}
			else
			{
				_loadingPlayer = _mediaPlayer;
			}

			// Change the displaying video
			_mediaDisplay.CurrentMediaPlayer = PlayingPlayer;
		}

		public void OnOpenVideoFile()
 		{
			LoadingPlayer.m_VideoPath = System.IO.Path.Combine(_folder, _videoFiles[_VideoIndex]);
			_VideoIndex = (_VideoIndex + 1) % (_videoFiles.Length);
			if (string.IsNullOrEmpty(LoadingPlayer.m_VideoPath))
			{
				LoadingPlayer.CloseVideo();
				_VideoIndex = 0;
			}
			else
			{
				LoadingPlayer.OpenVideoFromFile(_location, LoadingPlayer.m_VideoPath, _AutoStartToggle.isOn);
//				SetButtonEnabled( "PlayButton", !_mediaPlayer.m_AutoStart );
//				SetButtonEnabled( "PauseButton", _mediaPlayer.m_AutoStart );
			}
		}

		public void OnAutoStartChange()
		{
			if(PlayingPlayer && 
				_AutoStartToggle && _AutoStartToggle.enabled &&
				PlayingPlayer.m_AutoStart != _AutoStartToggle.isOn )
			{
				PlayingPlayer.m_AutoStart = _AutoStartToggle.isOn;
			}
			if (LoadingPlayer &&
				_AutoStartToggle && _AutoStartToggle.enabled &&
				LoadingPlayer.m_AutoStart != _AutoStartToggle.isOn)
			{
				LoadingPlayer.m_AutoStart = _AutoStartToggle.isOn;
			}
		}

		public void OnMuteChange()
		{
			if (PlayingPlayer)
			{
				PlayingPlayer.Control.MuteAudio(_MuteToggle.isOn);
			}
			if (LoadingPlayer)
			{
				LoadingPlayer.Control.MuteAudio(_MuteToggle.isOn);
			}
		}

		public void OnPlayButton()
		{
			if(PlayingPlayer)
			{
				PlayingPlayer.Control.Play();
//				SetButtonEnabled( "PlayButton", false );
//				SetButtonEnabled( "PauseButton", true );
			}
		}
		public void OnPauseButton()
		{
			if(PlayingPlayer)
			{
				PlayingPlayer.Control.Pause();
//				SetButtonEnabled( "PauseButton", false );
//				SetButtonEnabled( "PlayButton", true );
			}
		}

		public void OnVideoSeekSlider()
		{
			if (PlayingPlayer && _videoSeekSlider && _videoSeekSlider.value != _setVideoSeekSliderValue)
			{
				PlayingPlayer.Control.Seek(_videoSeekSlider.value * PlayingPlayer.Info.GetDurationMs());
			}
		}
		public void OnVideoSliderDown()
		{
			if(PlayingPlayer)
			{
				_wasPlayingOnScrub = PlayingPlayer.Control.IsPlaying();
				if( _wasPlayingOnScrub )
				{
					PlayingPlayer.Control.Pause();
//					SetButtonEnabled( "PauseButton", false );
//					SetButtonEnabled( "PlayButton", true );
				}
				OnVideoSeekSlider();
			}
		}
		public void OnVideoSliderUp()
		{
			if(PlayingPlayer && _wasPlayingOnScrub )
			{
				PlayingPlayer.Control.Play();
				_wasPlayingOnScrub = false;

//				SetButtonEnabled( "PlayButton", false );
//				SetButtonEnabled( "PauseButton", true );
			}			
		}

		public void OnAudioVolumeSlider()
		{
			if (PlayingPlayer && _audioVolumeSlider && _audioVolumeSlider.value != _setAudioVolumeSliderValue)
			{
				PlayingPlayer.Control.SetVolume(_audioVolumeSlider.value);
			}
			if (LoadingPlayer && _audioVolumeSlider && _audioVolumeSlider.value != _setAudioVolumeSliderValue)
			{
				LoadingPlayer.Control.SetVolume(_audioVolumeSlider.value);
			}
		}
		//		public void OnMuteAudioButton()
		//		{
		//			if( _mediaPlayer )
		//			{
		//				_mediaPlayer.Control.MuteAudio( true );
		//				SetButtonEnabled( "MuteButton", false );
		//				SetButtonEnabled( "UnmuteButton", true );
		//			}
		//		}
		//		public void OnUnmuteAudioButton()
		//		{
		//			if( _mediaPlayer )
		//			{
		//				_mediaPlayer.Control.MuteAudio( false );
		//				SetButtonEnabled( "UnmuteButton", false );
		//				SetButtonEnabled( "MuteButton", true );
		//			}
		//		}

		public void OnRewindButton()
		{
			if(PlayingPlayer)
			{
				PlayingPlayer.Control.Rewind();
			}
		}

		private void Awake()
		{
			_loadingPlayer = _mediaPlayerB;
		}

		void Start()
		{
			if(PlayingPlayer)
			{
				PlayingPlayer.Events.AddListener(OnVideoEvent);

				if (LoadingPlayer)
				{
					LoadingPlayer.Events.AddListener(OnVideoEvent);
				}

				if ( _audioVolumeSlider )
				{
					// Volume
					if (PlayingPlayer.Control != null)
					{
						float volume = PlayingPlayer.Control.GetVolume();
						_setAudioVolumeSliderValue = volume;
						_audioVolumeSlider.value = volume;
					}
				}

				// Auto start toggle
				_AutoStartToggle.isOn = PlayingPlayer.m_AutoStart;

				if(PlayingPlayer.m_AutoOpen )
				{
//					RemoveOpenVideoButton();

//					SetButtonEnabled( "PlayButton", !_mediaPlayer.m_AutoStart );
//					SetButtonEnabled( "PauseButton", _mediaPlayer.m_AutoStart );
				}
				else
				{
//					SetButtonEnabled( "PlayButton", false );
//					SetButtonEnabled( "PauseButton", false );
				}

//				SetButtonEnabled( "MuteButton", !_mediaPlayer.m_Muted );
//				SetButtonEnabled( "UnmuteButton", _mediaPlayer.m_Muted );

				OnOpenVideoFile();
			}
		}

		void Update()
		{
			if (PlayingPlayer && PlayingPlayer.Info != null && PlayingPlayer.Info.GetDurationMs() > 0f)
			{
				float time = PlayingPlayer.Control.GetCurrentTimeMs();
				float d = time / PlayingPlayer.Info.GetDurationMs();
				_setVideoSeekSliderValue = d;
				_videoSeekSlider.value = d;
			}
		}

		// Callback function to handle events
		public void OnVideoEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
		{
			switch (et)
			{
				case MediaPlayerEvent.EventType.ReadyToPlay:
				break;
				case MediaPlayerEvent.EventType.Started:
				break;
				case MediaPlayerEvent.EventType.FirstFrameReady:
					SwapPlayers();
				break;
				case MediaPlayerEvent.EventType.FinishedPlaying:
				break;
			}

			Debug.Log("Event: " + et.ToString());
		}

//		private void SetButtonEnabled( string objectName, bool bEnabled )
//		{
//			Button button = GameObject.Find( objectName ).GetComponent<Button>();
//			if( button )
//			{
//				button.enabled = bEnabled;
//				button.GetComponentInChildren<CanvasRenderer>().SetAlpha( bEnabled ? 1.0f : 0.4f );
//				button.GetComponentInChildren<Text>().color = Color.clear;
//			}
//		}

//		private void RemoveOpenVideoButton()
//		{
//			Button openVideoButton = GameObject.Find( "OpenVideoButton" ).GetComponent<Button>();
//			if( openVideoButton )
//			{
//				openVideoButton.enabled = false;
//				openVideoButton.GetComponentInChildren<CanvasRenderer>().SetAlpha( 0.0f );
//				openVideoButton.GetComponentInChildren<Text>().color = Color.clear;
//			}
//
//			if( _AutoStartToggle )
//			{
//				_AutoStartToggle.enabled = false;
//				_AutoStartToggle.isOn = false;
//				_AutoStartToggle.GetComponentInChildren<CanvasRenderer>().SetAlpha( 0.0f );
//				_AutoStartToggle.GetComponentInChildren<Text>().color = Color.clear;
//				_AutoStartToggle.GetComponentInChildren<Image>().enabled = false;
//				_AutoStartToggle.GetComponentInChildren<Image>().color = Color.clear;
//			}
//		}
	}
}
#endif