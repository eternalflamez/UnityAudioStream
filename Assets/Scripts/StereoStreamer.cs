using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StereoStreamer : MonoBehaviour {
    [SerializeField]
    private HTTPStream _stereo;
    [SerializeField]
    private AudioSource _audioSource;

    // Use this for initialization
    void Start () {
        _stereo.StartStream();
        StartCoroutine(WaitForAudio());
    }

    private IEnumerator WaitForAudio()
    {
        yield return new WaitUntil(() => _stereo.AudioInformation != null);

        _audioSource.clip = _stereo.AudioInformation.StereoClip;
        _audioSource.Play();
    }
}
