using RenderHeads.Media.AVProVideo;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HTTPStreamSynchroniser : MonoBehaviour {
    [SerializeField]
    private List<HTTPStream> _httpStreams;

    [SerializeField]
    private MediaPlayer _mediaPlayer;

    private bool _bufferShortage;

    public bool BufferShortage
    {
        get { return _bufferShortage; }
    }

    private void Update()
    {
        bool shortage = false;

        foreach (HTTPStream streamer in _httpStreams)
        {
            if(streamer.CriticalBufferSize)
            {
                shortage = true;
            }
        }

        if(_mediaPlayer && _mediaPlayer.Control.IsBuffering())
        {
            shortage = true;
        }

        _bufferShortage = shortage;
    }
}
