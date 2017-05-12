using RenderHeads.Media.AVProVideo;
using System.Collections;
using UnityEngine;

public class AmbisonicsStreamer : MonoBehaviour
{
    [SerializeField]
    private HTTPStream _httpStream;

    [SerializeField]
    private GvrAudioSoundfield _soundField;

    [SerializeField]
    private MediaPlayer _mediaPlayer;

    [SerializeField]
    private HTTPStreamSynchroniser _httpStreamSynchroniser;

    private bool _ready = false;
    private bool _videoReady = false;

    private float _test;

    // Use this for initialization
    void Start()
    {
        _httpStream.StartStream();

        StartCoroutine(WaitForBuffer());
        _mediaPlayer.Events.AddListener(OnMediaPlayerEvent);
    }

    private void OnMediaPlayerEvent(MediaPlayer mediaPlayer, MediaPlayerEvent.EventType eventType, ErrorCode errorCode)
    {
        if (eventType == MediaPlayerEvent.EventType.FirstFrameReady)
        {
            _videoReady = true;
        }
    }

    private void Update()
    {
        _test += Time.deltaTime;

        if (_test >= 1)
        {
            _test = 0;
            Debug.Log("D: " + (_soundField.time - (_mediaPlayer.Control.GetCurrentTimeMs() / 1000)));
        }

        if (_ready)
        {
            if (_httpStreamSynchroniser.BufferShortage)
            {
                _soundField.Pause();
                _mediaPlayer.Pause();
            }
            else if ((!_soundField.isPlaying || !_mediaPlayer.Control.IsPlaying()))
            {
                _mediaPlayer.Play();
                _soundField.Play();
            }
        }
    }

    private IEnumerator WaitForBuffer()
    {
        yield return new WaitUntil(() => _httpStream.AudioInformation != null);
        yield return new WaitUntil(() => _httpStream.BufferLength > 3);

        _mediaPlayer.Play();

        yield return new WaitUntil(() => _videoReady && !_httpStreamSynchroniser.BufferShortage);

        _soundField.clip0102 = _httpStream.AudioInformation.Ambisonics12;
        _soundField.clip0304 = _httpStream.AudioInformation.Ambisonics34;
        _ready = true;
    }
}