using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HTTPStreamAmbisonics : HTTPStream
{
    protected override void Start()
    {
        base.Start();

        _audioQueue = new AudioQueueManager(new Queue<float>(), new Queue<float>());
    }

    protected override bool Setup(byte[] data)
    {
        _offset = ReadWavHeaders(data);
        if (_wavContainer.Failed)
        {
            return false;
        }

        float totalFrames = (_wavContainer.FileLength / _wavContainer.BytesPerFrame);
        _wavContainer.FileLength = (int)totalFrames;

        totalFrames /= 2;

        if(_wavContainer.ChannelCount != 4)
        {
            Debug.LogError("ERROR: Wrong channel count found in file.");
            return false;
        }

        _wavContainer.ByteRate /= 2; // Adjust for splitting channels
        AudioClip clip12 = AudioClip.Create("Channel12", (int)totalFrames, 2, _wavContainer.SampleRate, true, OnAudioRead12);
        AudioClip clip34 = AudioClip.Create("Channel34", (int)totalFrames, 2, _wavContainer.SampleRate, true, OnAudioRead34);
        _audioInformation = new AudioInformation(clip12, clip34);

        return true;
    }

    protected override void SaveByteArrayToData(byte[] sourceData)
    {
        float[] data = ByteArrayToWavFloatArray(sourceData);

        _offset = 0;
        
        // Read the data unless we have more data than the wav headers said.
        for (int i = 0; i < data.Length && i + _valuesRead < _wavContainer.FileLength; i++)
        {
            float position = i % 4;

            if (position == 0 || position == 1)
            {
                _audioQueue.Enqueue(data[i], AudioQueueManager.Channel.ambisonics12);
            }
            else if (position == 2 || position == 3)
            {
                _audioQueue.Enqueue(data[i], AudioQueueManager.Channel.ambisonics34);
            }
        }

        if (_saveCache)
        {
            SaveToCache(sourceData);
        }

        _valuesRead += data.Length;
    }

    private void OnAudioRead12(float[] audioBytes)
    {
        audioBytes = AudioRead(AudioQueueManager.Channel.ambisonics12, audioBytes);
    }

    private void OnAudioRead34(float[] audioBytes)
    {
        audioBytes = AudioRead(AudioQueueManager.Channel.ambisonics34, audioBytes);
    }

    private float[] AudioRead(AudioQueueManager.Channel channel, float[] audioBytes)
    {
        // If we don't have enough values yet we must wait.
        if (_valuesRead < audioBytes.Length)
        {
            return audioBytes;
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
                audioBytes[count++] = _audioQueue.Dequeue(channel);
            }
        }

        return audioBytes;
    }
}
