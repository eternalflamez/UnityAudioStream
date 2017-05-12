using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HTTPStreamStereo : HTTPStream {

    protected override void Start()
    {
        base.Start();
        
        _audioQueue = new AudioQueueManager(new Queue<float>());
    }

    protected override bool Setup(byte[] data)
    {
        _offset = ReadWavHeaders(data);
        if (_wavContainer.Failed)
        {
            return false;
        }

        float totalFrames = _wavContainer.FileLength / _wavContainer.BytesPerFrame;
        _wavContainer.FileLength = (int)totalFrames;
        AudioClip clip = AudioClip.Create("Streamed audio", _wavContainer.FileLength, _wavContainer.ChannelCount, _wavContainer.SampleRate, true, OnAudioRead);
        _audioInformation = new AudioInformation(clip);

        return true;
    }

    protected override void SaveByteArrayToData(byte[] sourceData)
    {
        float[] data = ByteArrayToWavFloatArray(sourceData);

        _offset = 0;

        // Read the data unless we have more data than the wav headers said.
        for (int i = 0; i < data.Length && i + _valuesRead < _wavContainer.FileLength; i++)
        {
            _audioQueue.Enqueue(data[i]);
        }

        if (_saveCache)
        {
            SaveToCache(sourceData);
        }

        _valuesRead += data.Length;
    }

    /// <summary>
    /// Gets called when the clip needs more audio data.
    /// </summary>
    /// <param name="audioBytes">An array of floats to fill with new data.</param>
    private void OnAudioRead(float[] audioBytes)
    {
        // If we don't have enough values yet we must wait.
        if (_valuesRead < audioBytes.Length)
        {
            return;
        }

        int count = 0;
        while (count < audioBytes.Length)
        {
            if (_audioQueue.Count == 0)
            {
                // Past loop point.
                // If we're not looping then send no sound and make sure we stop.
                audioBytes[count++] = 0;
                _ended = true;
            }
            else
            {
                // Keep reading bytes and play the sound.
                audioBytes[count++] = _audioQueue.Dequeue();
            }
        }
    }
}
