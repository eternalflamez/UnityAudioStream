using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioQueueManager
{
    public enum Channel
    {
        Stereo,
        ambisonics12,
        ambisonics34
    }

    private Queue<float> _channel12;
    private Queue<float> _channel34;

    public int Count {
        get {
            if(_channel12 != null)
            {
                if(_channel34 != null)
                {
                    return Mathf.Min(_channel12.Count, _channel34.Count);
                }

                return _channel12.Count;
            }

            return 0;
        }
    }

    public void Enqueue(float value, Channel channel = Channel.Stereo)
    {
        switch (channel)
        {
            case Channel.Stereo:
            case Channel.ambisonics12:
                _channel12.Enqueue(value);
                break;
            case Channel.ambisonics34:
                _channel34.Enqueue(value);
                break;
        }
    }

    public float Dequeue(Channel channel = Channel.Stereo)
    {
        switch (channel)
        {
            case Channel.Stereo:
            case Channel.ambisonics12:
                return _channel12.Dequeue();
            case Channel.ambisonics34:
                return _channel34.Dequeue();
        }

        return 0;
    }

    public AudioQueueManager(Queue<float> stereo)
    {
        _channel12 = stereo;
    }

    public AudioQueueManager(Queue<float> ambisonics12, Queue<float> ambisonics34)
    {
        _channel12 = ambisonics12;
        _channel34 = ambisonics34;
    }
}