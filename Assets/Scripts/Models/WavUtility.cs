using UnityEngine;
using System.Text;
using System.IO;
using System;

/// <summary>
/// WAV utility for recording and audio playback functions in Unity.
/// Version: 1.0 alpha 1
///
/// - Use "ToAudioClip" method for loading wav file / bytes.
/// Loads .wav (PCM uncompressed) files at 8,16,24 and 32 bits and converts data to Unity's AudioClip.
///
/// - Use "FromAudioClip" method for saving wav file / bytes.
/// Converts an AudioClip's float data into wav byte array at 16 bit.
/// </summary>
/// <remarks>
/// For documentation and usage examples: https://github.com/deadlyfingers/UnityWav
/// </remarks>

public class WavUtility
{
    public static float[] Convert8BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize)
    {
        float[] data = new float[dataSize];

        sbyte maxValue = sbyte.MaxValue;

        int i = 0;
        while (i < dataSize)
        {
            data[i] = (float)source[i] / maxValue;
            ++i;
        }

        return data;
    }

    public static float[] Convert16BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize, int format)
    {
        int x = sizeof(Int16); // block size = 2
        int convertedSize = dataSize / x;

        float[] data = new float[convertedSize];

        Int16 maxValue = Int16.MaxValue;

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;

            if (format == 1)
            {
                data[i] = (float)BitConverter.ToInt16(source, offset) / maxValue;
            }
            else if(format == 3)
            {
                data[i] = BitConverter.ToSingle(source, offset);
            }

            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

        return data;
    }

    public static float[] Convert24BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize, int format)
    {
        int x = 3; // block size = 3
        int convertedSize = dataSize / x;

        int maxValue = Int32.MaxValue;

        float[] data = new float[convertedSize];

        byte[] block = new byte[sizeof(int)]; // using a 4 byte block for copying 3 bytes, then copy bytes with 1 offset

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;
            Buffer.BlockCopy(source, offset, block, 1, x);

            if (format == 1)
            {
                data[i] = (float)BitConverter.ToInt32(block, 0) / maxValue;
            }
            else if (format == 3)
            {
                data[i] = BitConverter.ToSingle(block, 0);
            }

            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

        return data;
    }

    public static float[] Convert32BitByteArrayToAudioClipData(byte[] source, int headerOffset, int dataSize, int format)
    {
        int x = sizeof(float); //  block size = 4
        int convertedSize = dataSize / x;

        Int32 maxValue = Int32.MaxValue;

        float[] data = new float[convertedSize];

        int offset = 0;
        int i = 0;
        while (i < convertedSize)
        {
            offset = i * x + headerOffset;

            if (format == 1)
            {
                data[i] = (float)BitConverter.ToInt32(source, offset) / maxValue;
            }
            else if(format == 3)
            {
                data[i] = BitConverter.ToSingle(source, offset);
            }

            ++i;
        }

        Debug.AssertFormat(data.Length == convertedSize, "AudioClip .wav data is wrong size: {0} == {1}", data.Length, convertedSize);

        return data;
    }

    /// <summary>
    /// Calculates the bit depth of an AudioClip
    /// </summary>
    /// <returns>The bit depth. Should be 8 or 16 or 32 bit.</returns>
    /// <param name="audioClip">Audio clip.</param>
    public static UInt16 BitDepth(AudioClip audioClip)
    {
        UInt16 bitDepth = Convert.ToUInt16(audioClip.samples * audioClip.channels * audioClip.length / audioClip.frequency);
        Debug.AssertFormat(bitDepth == 8 || bitDepth == 16 || bitDepth == 32, "Unexpected AudioClip bit depth: {0}. Expected 8 or 16 or 32 bit.", bitDepth);
        return bitDepth;
    }

    private static int BytesPerSample(UInt16 bitDepth)
    {
        return bitDepth / 8;
    }

    private static int BlockSize(UInt16 bitDepth)
    {
        switch (bitDepth)
        {
            case 32:
                return sizeof(Int32); // 32-bit -> 4 bytes (Int32)
            case 16:
                return sizeof(Int16); // 16-bit -> 2 bytes (Int16)
            case 8:
                return sizeof(sbyte); // 8-bit -> 1 byte (sbyte)
            default:
                throw new Exception(bitDepth + " bit depth is not supported.");
        }
    }

    public static string FormatCode(UInt16 code)
    {
        switch (code)
        {
            case 1:
                return "PCM";
            case 2:
                return "ADPCM";
            case 3:
                return "IEEE";
            case 7:
                return "μ-law";
            case 65534:
                return "WaveFormatExtensible";
            default:
                Debug.LogWarning("Unknown wav code format:" + code);
                return "";
        }
    }

}