using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioInformation {
    private AudioClip _stereoClip;

    private AudioClip _ambisonics12;
    private AudioClip _ambisonics34;

    public AudioClip StereoClip
    {
        get { return _stereoClip; }
    }

    public AudioClip Ambisonics12
    {
        get { return _ambisonics12; }
    }

    public AudioClip Ambisonics34
    {
        get { return _ambisonics34; }
    }

    public AudioInformation(AudioClip stereoClip)
    {
        _stereoClip = stereoClip;
    }

    public AudioInformation(AudioClip ambisonics12, AudioClip ambisonics34)
    {
        _ambisonics12 = ambisonics12;
        _ambisonics34 = ambisonics34;
    }
}
