using BestHTTP;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public abstract class HTTPStream : MonoBehaviour
{
    [SerializeField]
    private string _url;
    [SerializeField]
    private int _cacheDownloadChunkSeconds = 5; // How much time do we download per chunk?
    [SerializeField]
    private float _minCacheLengthInSeconds = 5;
    [SerializeField]
    private float _criticalBufferSizeInSeconds = 1f;
    [SerializeField]
    private bool _useCache = false;

    protected bool _saveCache;
    private bool _criticalBufferSize;
    private bool _downloading; // Are we already waiting for a http request?
    protected bool _ended; // Did the audio end?
    protected bool _started;
    protected int _offset = 0; // Offset so we skip the headers in reading the data
    protected int _valuesRead; // Used to verify data length vs read.
    private int _downloadPosition = 200; // 1.920.000, or 2 channels * 48k sample rate * 2 (chunk size for 16 bit) * 10 seconds.
    private float _queuedValues; // Debug value
    private string _filePath;
    private HTTPRequest _request;
    protected AudioInformation _audioInformation;
    protected WavContainer _wavContainer;
    private BinaryWriter _binaryWriter;
    private BinaryReader _binaryReader;
    protected AudioQueueManager _audioQueue;

    public bool Ended
    {
        get { return _ended; }
    }

    public AudioInformation AudioInformation
    {
        get { return _audioInformation; }
    }

    public bool CriticalBufferSize
    {
        get { return _criticalBufferSize; }
    }

    /// <summary>
    /// The buffer length in seconds.
    /// </summary>
    public float BufferLength
    {
        get {
            if (_wavContainer.ByteRate == 0)
            {
                return 0;
            }
            else
            {
                return (_audioQueue.Count * _wavContainer.BytesPerFrame) / _wavContainer.ByteRate;
            }
        }
    }

    // Use this for initialization
    protected virtual void Start()
    {
        if (_useCache || _saveCache)
        {
            string[] pathSplit = _url.Split('.');
            _filePath = pathSplit[pathSplit.Length - 2];

            while (_filePath[0] != '/')
            {
                _filePath = _filePath.Remove(0, 1);
            }

            _filePath = Application.persistentDataPath + _filePath + ".wav";
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath));
        }
    }

    /// <summary>
    /// Starts the stream.
    /// </summary>
    public void StartStream()
    {
        CreateHttpRequest();

        if (_useCache && File.Exists(_filePath))
        {
            SetupFileRead();
            return;
        }
        else if (_useCache && !File.Exists(_filePath))
        {
            _useCache = false;
            _saveCache = true;
        }
        
        SendHttpRequest(0, _downloadPosition);
    }

    /// <summary>
    /// Reads the wav headers and makes sure we can begin.
    /// </summary>
    /// <param name="data">The first chunk of data of the wav.</param>
    /// <returns>True if the headers were found, false otherwise.</returns>
    protected abstract bool Setup(byte[] data);

    private void SetupFileRead()
    {
        _binaryReader = new BinaryReader(File.OpenRead(_filePath));
        byte[] chunk = _binaryReader.ReadBytes(_downloadPosition);

        if (chunk.Length < _downloadPosition || !Setup(chunk))
        {
            // The file is not long enough for the start cache, we should use http requests instead, but save cache anyway.
            _useCache = false;
            _saveCache = true;
            _binaryReader.Close();

            // Remove the file for now, it's empty anyway.
            File.Delete(_filePath);

            SendHttpRequest(0, _downloadPosition);
            return;
        }
        
        SaveByteArrayToData(chunk);
    }

    /// <summary>
    /// Creates a http request for later use.
    /// </summary>
    private void CreateHttpRequest()
    {
        _request = new HTTPRequest(new Uri(_url), (req, resp) =>
        {
            if (resp == null || !resp.IsSuccess)
            {
                _downloading = false;
                return;
            }

            byte[] fragment = resp.Data;

            if (!_started)
            {
                // After downloading the first chunk, read headers and create a clip.
                Setup(fragment);
                _started = true;
            }

            if (fragment != null)
            {
                SaveByteArrayToData(fragment);
            }
            
            _downloading = false;
        });

        // Set this to false if you want to cache the audio.
        _request.DisableCache = true;
    }

    /// <summary>
    /// Sends a request for bytes.
    /// </summary>
    /// <param name="start">The start of the fragment, inclusive.</param>
    /// <param name="end">The end of the fragment, exclusive.</param>
    private void SendHttpRequest(int start, int end)
    {
        _request.SetRangeHeader(start, end - 1);
        _request.Send();
        _downloading = true;
    }

    protected float[] ByteArrayToWavFloatArray(byte[] sourceData)
    {
        float[] data;

        switch (_wavContainer.BitDepth)
        {
            case 8:
                data = WavUtility.Convert8BitByteArrayToAudioClipData(sourceData, _offset, sourceData.Length - _offset);
                break;
            case 16:
                data = WavUtility.Convert16BitByteArrayToAudioClipData(sourceData, _offset, sourceData.Length - _offset, _wavContainer.Format);
                break;
            case 24:
                data = WavUtility.Convert24BitByteArrayToAudioClipData(sourceData, _offset, sourceData.Length - _offset, _wavContainer.Format);
                break;
            case 32:
                data = WavUtility.Convert32BitByteArrayToAudioClipData(sourceData, _offset, sourceData.Length - _offset, _wavContainer.Format);
                break;
            default:
                throw new Exception(_wavContainer.BitDepth + " bit depth is not supported.");
        }

        return data;
    }

    private void Update()
    {
        if (_audioQueue != null)
        {
            // Save this value in a variable so we can see it in the editor.
            _queuedValues = _audioQueue.Count;

            if (_wavContainer != null)
            {
                // If not already downloading, our cache is getting small and we can download more:
                if (!_downloading
                    && BufferLength < _minCacheLengthInSeconds
                    && _downloadPosition < _wavContainer.FileLength * _wavContainer.BytesPerFrame)
                {
                    // Either download the required seconds or the remainder of the file, whichever is smaller.
                    int chunkBlock = Mathf.Min(_cacheDownloadChunkSeconds * _wavContainer.ByteRate, (_wavContainer.FileLength * (int)_wavContainer.BytesPerFrame) - _downloadPosition);

                    // If we want to use the cache.
                    if (_useCache)
                    {
                        // Read the chunk.
                        byte[] chunk = _binaryReader.ReadBytes(chunkBlock);

                        // Check if its all there.
                        if (chunk.Length == chunkBlock)
                        {
                            SaveByteArrayToData(chunk);
                        }
                        else
                        {
                            _binaryReader.Close();
                            File.Delete(_filePath);
                            _useCache = false;
                        }

                        _downloadPosition += chunkBlock;
                    }
                    else
                    {
                        SendHttpRequest(_downloadPosition, _downloadPosition + chunkBlock);
                        _downloadPosition += chunkBlock;
                    }

                    _criticalBufferSize = !_downloading && BufferLength < _criticalBufferSizeInSeconds;
                }
            }
        }
    }

    /// <summary>
    /// Takes source byte data and converts it to wav format floats.
    /// </summary>
    /// <param name="sourceData">Source byte data.</param>
    protected abstract void SaveByteArrayToData(byte[] sourceData);

    /// <summary>
    /// Reads the headers and notes the bytes/sec and total frame count.
    /// </summary>
    /// <param name="data"></param>
    /// <returns>The header length to skip</returns>
    protected int ReadWavHeaders(byte[] data)
    {
        string riff = Encoding.ASCII.GetString(data, 0, 4);
        string wave = Encoding.ASCII.GetString(data, 8, 4);

        int headerOffset = 12;
        bool found = false;

        // Loop through the headers to look for the "fmt " tag. This skips the JUNK header, among others.
        while (!found)
        {
            if (Encoding.ASCII.GetString(data, headerOffset, 4) == "fmt ")
            {
                found = true;
                break;
            }
            else if (headerOffset > 2018) // max offset if we assume the total header length < 2048
            {
                Debug.Log("Error: No headers could be found.");
                break;
            }

            // Move forward 2 bytes because all the values are bundled per 2.
            headerOffset += 2;
        }

        int subchunk1 = BitConverter.ToInt32(data, headerOffset + 4);
        UInt16 audioFormat = BitConverter.ToUInt16(data, headerOffset + 8);

        // NB: Only uncompressed PCM wav files are supported.
        string formatCode = WavUtility.FormatCode(audioFormat);
        Debug.AssertFormat(audioFormat == 1 || audioFormat == 3 || audioFormat == 65534, "Detected format code '{0}' {1}, but only PCM and WaveFormatExtensable uncompressed formats are currently supported.", audioFormat, formatCode);

        // Read the headers.
        int channels = BitConverter.ToUInt16(data, headerOffset + 10);
        int sampleRate = BitConverter.ToInt32(data, headerOffset + 12);
        int byteRate = BitConverter.ToInt32(data, headerOffset + 16);
        UInt16 blockAlign = BitConverter.ToUInt16(data, headerOffset + 20);
        ushort bitDepth = BitConverter.ToUInt16(data, headerOffset + 22);

        headerOffset += 24;
        found = false;

        // Look for the data header.
        while (!found)
        {
            if (Encoding.ASCII.GetString(data, headerOffset, 4) == "data")
            {
                found = true;
                headerOffset += 4;
                break;
            }
            else if (headerOffset > 2048)
            {
                Debug.Log("Error: No data could be found.");
                break;
            }

            headerOffset += 2;
        }

        int fileLength = BitConverter.ToInt32(data, headerOffset);
        Debug.LogFormat("riff={0} wave={1} subchunk1={2} format={3} channels={4} sampleRate={5} byteRate={6} blockAlign={7} bitDepth={8} headerOffset={9} subchunk2={10} chunksize={11}", riff, wave, subchunk1, formatCode, channels, sampleRate, byteRate, blockAlign, bitDepth, headerOffset, fileLength, data.Length);

        if (!found)
        {
            _wavContainer = new WavContainer(true);
        }
        else
        {
            _wavContainer = new WavContainer(bitDepth, channels, sampleRate, byteRate, fileLength, audioFormat);
        }

        // Add 4 to move past the data for the file length.
        return headerOffset + 4;
    }

    protected void SaveToCache(byte[] data)
    {
        if (_binaryWriter == null)
        {
            _binaryWriter = new BinaryWriter(File.Open(_filePath, FileMode.Append));
        }

        _binaryWriter.Write(data);
        _binaryWriter.Flush();

        // If we've reached the end of the file.
        if (_downloadPosition == _wavContainer.FileLength * (int)_wavContainer.BytesPerFrame)
        {
            _binaryWriter.Close();

            // Read it
            _binaryReader = new BinaryReader(File.OpenRead(_filePath));
            byte[] bytes = _binaryReader.ReadBytes((_wavContainer.FileLength * (int)_wavContainer.BytesPerFrame) + 2048);

            // Compute checksum and save it in a file
            ushort computed = CrcConvertor.ComputeChecksum(bytes);
            File.Create(Path.GetDirectoryName(_filePath) + computed.ToString("x2"));
            bytes = null;
        }
    }

    private void OnApplicationQuit()
    {
        OnDestroy();
    }

    private void OnDestroy()
    {
        if (_saveCache && _binaryWriter != null)
        {
            _binaryWriter.Close();
        }

        if(_useCache && _binaryReader != null)
        {
            _binaryReader.Close();
        }
    }

    [Serializable]
    protected class WavContainer
    {
        private bool _failed = false;
        private ushort _bitDepth;
        private int _channels;
        private int _sampleRate;
        private int _byteRate;
        private int _fileLength;
        private float _bytesPerFrame; // Bytes required per float value. This is the bitrate / 8.
        private int _format;

        public int Format
        {
            get { return _format; }
        }

        public bool Failed
        {
            get { return _failed; }
        }

        public ushort BitDepth
        {
            get { return _bitDepth; }
        }

        public int ChannelCount
        {
            get { return _channels; }
        }

        public int SampleRate
        {
            get { return _sampleRate; }
        }

        public int ByteRate
        {
            get { return _byteRate; }
            set { _byteRate = value; }
        }

        public int FileLength
        {
            get { return _fileLength; }
            set { _fileLength = value; }
        }

        public float BytesPerFrame
        {
            get { return _bytesPerFrame; }
        }

        public WavContainer(bool failed)
        {
            _failed = failed;
        }

        public WavContainer(ushort bitdepth, int channels, int sampleRate, int byteRate, int fileLength, int format)
        {
            _bitDepth = bitdepth;
            _channels = channels;
            _sampleRate = sampleRate;
            _byteRate = byteRate;
            _fileLength = fileLength;
            _format = format;

            _bytesPerFrame = (bitdepth / 8f);
        }
    }
}