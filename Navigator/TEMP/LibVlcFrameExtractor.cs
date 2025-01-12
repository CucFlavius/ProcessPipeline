namespace ProcessPipeline;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using LibVLCSharp.Shared;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

public class LibVlcFrameExtractor
{
    // A static “global” reference
    private static LibVlcFrameExtractor _currentInstance;
    
    private readonly LibVLC _libVLC;
    private readonly MediaPlayer _mediaPlayer;
    private volatile bool _stopRequested = false;

    // We'll store frames in a concurrent queue so we can yield them from another thread.
    private readonly BlockingCollection<Image<Rgba32>> _framesQueue = new BlockingCollection<Image<Rgba32>>(new ConcurrentQueue<Image<Rgba32>>());

    // Info about the video format
    private int _width;
    private int _height;
    private int _pitch;

    // A buffer for the current frame (libVLC writes into it)
    private byte[] _currentFrameBuffer;

    public LibVlcFrameExtractor(string videoPath)
    {
        Core.Initialize();
        _libVLC = new LibVLC();

        // GCHandle to reference `this`
        var handle = GCHandle.Alloc(this, GCHandleType.Normal);

        
        // Create media, set options, etc.
        using var media = new Media(_libVLC, videoPath, FromType.FromPath);
        media.AddOption(":no-video-filter");
        media.AddOption(":no-audio-filter");
        media.AddOption(":no-sub-autodetect-file");
        media.AddOption(":vout=dummy"); // Add this option to disable video output window

        media.AddOption(":no-audio");
        media.AddOption(":no-video-title-show");
        media.AddOption(":no-overlay");
        
        _mediaPlayer = new MediaPlayer(_libVLC);
        _mediaPlayer.Media = media;

        // Save “this” to the static field.
        // (Not thread-safe if you have multiple players at once!)
        _currentInstance = this;

        // Normal setup:
        _mediaPlayer.SetVideoFormatCallbacks(OnSetVideoFormat, OnCleanup);
        _mediaPlayer.SetVideoCallbacks(OnLock, OnUnlock, OnDisplay);
        
        _mediaPlayer.EndReached += (sender, e) =>
        {
            _stopRequested = true;
            _framesQueue.CompleteAdding();
        };
    }
    
    /// <summary>
    /// Call this to begin playback (and thus begin receiving frames).
    /// </summary>
    public void Start()
    {
        _mediaPlayer.Play();
    }

    /// <summary>
    /// Call this to stop playback early (optional).
    /// </summary>
    public void Stop()
    {
        if (_mediaPlayer.IsPlaying)
        {
            _mediaPlayer.Stop();
        }

        _stopRequested = true;
        _framesQueue.CompleteAdding();
    }

    /// <summary>
    /// The core "extraction" method: returns an IEnumerable of Image<Rgba32>.
    /// Each iteration yields a newly decoded frame from LibVLC.
    /// </summary>
    public IEnumerable<Image<Rgba32>> GetFrames()
    {
        // As long as we have frames, yield them.
        // Once playback ends or is stopped, the queue gets completed.
        foreach (var frame in _framesQueue.GetConsumingEnumerable())
        {
            yield return frame;
        }
    }
    
    private static uint OnSetVideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        // Access the one and only instance
        var that = _currentInstance;
        if (that == null) return 0;

        // Write RV32 to 'chroma'
        var rv32Bytes = System.Text.Encoding.ASCII.GetBytes("RV32");
        Marshal.Copy(rv32Bytes, 0, chroma, 4);

        int w = (int)width;
        int h = (int)height;
        int pitch = w * 4;
        pitches = (uint)pitch;
        lines = (uint)h;

        // Allocate the frame buffer in the instance!
        that._width = w;
        that._height = h;
        that._pitch = pitch;
        that._currentFrameBuffer = new byte[pitch * h];

        // Return total number of bytes
        return (uint)(pitch * h);
    }

    private static void OnCleanup(ref IntPtr opaque)
    {
        var that = _currentInstance;
        if (that == null) return;
        that._currentFrameBuffer = null;
    }

    private IntPtr OnLock(IntPtr opaque, IntPtr planes)
    {
        var that = _currentInstance;
        if (that._currentFrameBuffer == null) return IntPtr.Zero;

        Marshal.WriteIntPtr(planes, 0, Marshal.UnsafeAddrOfPinnedArrayElement(that._currentFrameBuffer, 0));
        return Marshal.UnsafeAddrOfPinnedArrayElement(that._currentFrameBuffer, 0);
    }

    private void OnDisplay(IntPtr opaque, IntPtr picture)
    {
        var that = _currentInstance;
        if (that._currentFrameBuffer == null) return;
        
        if (that._stopRequested || that._currentFrameBuffer == null)
            return;

        var bufferCopy = new byte[that._currentFrameBuffer.Length];
        Buffer.BlockCopy(that._currentFrameBuffer, 0, bufferCopy, 0, bufferCopy.Length);

        // Now we can create an Image from that data
        var image = Image.LoadPixelData<Rgba32>(bufferCopy, that._width, that._height);
        
        if (!that._framesQueue.IsAddingCompleted)
            that._framesQueue.Add(image);
        else
            image.Dispose();
    }


    private void OnUnlock(IntPtr opaque, IntPtr picture, IntPtr planes)
    {
        // Called after LibVLC finishes writing to the buffer (but before display).
        // Typically you do minimal work here.
    }

    public void Dispose()
    {
        Stop();
        _mediaPlayer.Dispose();
        _libVLC.Dispose();
        _framesQueue.Dispose();
    }
}