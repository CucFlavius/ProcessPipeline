using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using LibVLCSharp.Shared;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ProcessPipeline.Nodes;

public class VideoInputNode : Node
{
    private Texture? _bufferTexture;
    
    private readonly GL _gl;
    private readonly LibVLC? _libVLC;
    private readonly MediaPlayer? _mediaPlayer;
    private readonly BlockingCollection<Image<Rgba32>>? _framesQueue;
    private int _videoWidth;
    private int _videoHeight;
    private const int _imageWidth = 1024;
    private const int _imageHeight = 512;
    private const int _bytesPerPixel = 4;
    private byte[]? _currentFrameBuffer;
    private volatile bool _stopRequested = false;
    
    public VideoInputNode(GL gl, string path, Vector2 pos, PortClickedHandler pcl) : base(pos, pcl)
    {
        Core.Initialize();
        _gl = gl;
        _libVLC = new LibVLC();
        _framesQueue = new BlockingCollection<Image<Rgba32>>();
        
        // Create media, set options, etc.
        using var media = new Media(_libVLC, path, FromType.FromPath);
        media.AddOption(":no-video-filter");
        media.AddOption(":no-audio-filter");
        media.AddOption(":no-sub-autodetect-file");
        media.AddOption(":vout=dummy"); // Add this option to disable video output window

        media.AddOption(":no-audio");
        media.AddOption(":no-video-title-show");
        media.AddOption(":no-overlay");
        
        _mediaPlayer = new MediaPlayer(_libVLC);
        _mediaPlayer.Media = media;

        // Normal setup:
        _mediaPlayer.SetVideoFormatCallbacks(OnSetVideoFormat, OnCleanup);
        _mediaPlayer.SetVideoCallbacks(OnLock, OnUnlock, OnDisplay);
        
        _mediaPlayer.EndReached += (sender, e) =>
        {
            _stopRequested = true;
            _framesQueue.CompleteAdding();
        };
        
        _mediaPlayer.Play();
    }

    public override void Update(float deltaTime)
    {
        if (_framesQueue == null || _framesQueue.IsCompleted)
            return;
        
        if (_framesQueue.TryTake(out var frame))
        {
            _bufferTexture?.Dispose();
            
            // resize to 512x512
            frame.Mutate(x => x.Resize(_imageWidth, _imageHeight));
            _bufferTexture = new Texture(_gl, frame);
            
            frame.Dispose();
        }
    }

    /*
    public override void Render(Vector2 gridPosition, Vector2 vector2, float zoomLevel, Vector2 drawableAreaPos)
    {
        base.Render(gridPosition, vector2, zoomLevel, drawableAreaPos);

        ImGui.Begin("Video Input");

        if (_bufferTexture == null)
        {
            ImGui.Text("Loading...");
            ImGui.End();
            return;
        }
        
        var windowSize = ImGui.GetItemRectSize();
        var imageAspect = _videoWidth / (float)_videoHeight;
        var windowWidth = windowSize.X;
        
        var imageWidth = windowWidth;
        var imageHeight = imageWidth / imageAspect;
        
        ImGui.Image(_bufferTexture.Handle, new Vector2(imageWidth, imageHeight));
        
        ImGui.End();
    }
    */
    
    private uint OnSetVideoFormat(ref IntPtr opaque, IntPtr chroma, ref uint width, ref uint height, ref uint pitches, ref uint lines)
    {
        // Access the one and only instance

        // Write RV32 to 'chroma'
        var rv32Bytes = System.Text.Encoding.ASCII.GetBytes("RGBA");
        Marshal.Copy(rv32Bytes, 0, chroma, 4);

        var w = (int)width;
        var h = (int)height;
        pitches = (uint)(w * _bytesPerPixel);
        lines = (uint)h;

        // Allocate the frame buffer in the instance!
        _videoWidth = w;
        _videoHeight = h;
        _currentFrameBuffer = new byte[w * h * _bytesPerPixel];

        // Return total number of bytes
        return (uint)(w * h * _bytesPerPixel);
    }
    
    private void OnCleanup(ref IntPtr opaque)
    {
        _currentFrameBuffer = null;
    }
    
    private IntPtr OnLock(IntPtr opaque, IntPtr planes)
    {
        if (_currentFrameBuffer == null)
            return IntPtr.Zero;

        Marshal.WriteIntPtr(planes, 0, Marshal.UnsafeAddrOfPinnedArrayElement(_currentFrameBuffer, 0));
        return Marshal.UnsafeAddrOfPinnedArrayElement(_currentFrameBuffer, 0);
    }

    private void OnDisplay(IntPtr opaque, IntPtr picture)
    {
        if (_currentFrameBuffer == null) return;
        
        if (_stopRequested)
            return;

        var image = Image.LoadPixelData<Rgba32>(_currentFrameBuffer, _videoWidth, _videoHeight);
        
        if (!_framesQueue.IsAddingCompleted)
            _framesQueue.Add(image);
        else
            image.Dispose();
    }
    
    private void OnUnlock(IntPtr opaque, IntPtr picture, IntPtr planes)
    {
        // Called after LibVLC finishes writing to the buffer (but before display).
        // Typically you do minimal work here.
    }
}