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

public class VideoInputNode : Node, IOpenGlNode
{
    public override string? Title { get; set; } = "Video Input Node";
    private Texture? _bufferTexture;
    
    private GL? _gl;
    private LibVLC? _libVLC;
    private MediaPlayer? _mediaPlayer;
    private BlockingCollection<Image<Rgba32>>? _framesQueue;
    private int _videoWidth;
    private int _videoHeight;
    private const int _imageWidth = 1024;
    private const int _imageHeight = 512;
    private const int _bytesPerPixel = 4;
    private byte[]? _currentFrameBuffer;
    private volatile bool _stopRequested = false;
    public override Flags flags => Flags.ResizableX | Flags.ResizableY;
    public GL? Gl { get; set; }
    
    public VideoInputNode(Vector2 pos, PortClickedHandler portClickedHandler) : base(pos, portClickedHandler)
    {
        InitializeVlc();
        
        AddInput("Input", DataType.String, (data) =>
        {
            if (data is string path)
            {
                InitializeMedia(path);
            }
        });

        //AddOutput("Output", DataType.String, () => Path);
    }
    
    public VideoInputNode() : base(Vector2.Zero, null)
    {
        InitializeVlc();
        
        AddInput("Input", DataType.String, (data) =>
        {
            if (data is string path)
            {
                InitializeMedia(path);
            }
        });
    }

    private void InitializeVlc()
    {
        Size = DefaultSize;
        
        Core.Initialize();
        _libVLC = new LibVLC();
        _framesQueue = new BlockingCollection<Image<Rgba32>>();
        _mediaPlayer = new MediaPlayer(_libVLC);
        
        // Normal setup:
        _mediaPlayer.SetVideoFormatCallbacks(OnSetVideoFormat, OnCleanup);
        _mediaPlayer.SetVideoCallbacks(OnLock, OnUnlock, OnDisplay);
        
        _mediaPlayer.EndReached += (sender, e) =>
        {
            _stopRequested = true;
            _framesQueue.CompleteAdding();
        };
    }

    private void InitializeMedia(string path)
    {
        // Create media, set options, etc.
        if (_libVLC == null) return;
        
        using var media = new Media(_libVLC, path, FromType.FromPath);
        media.AddOption(":no-video-filter");
        media.AddOption(":no-audio-filter");
        media.AddOption(":no-sub-autodetect-file");
        media.AddOption(":vout=dummy"); // Add this option to disable video output window

        media.AddOption(":no-audio");
        media.AddOption(":no-video-title-show");
        media.AddOption(":no-overlay");

        if (_mediaPlayer == null) return;
        
        _mediaPlayer.Media = media;
        _mediaPlayer.Play();
    }

    public override Vector2 DefaultSize { get; } = new Vector2(200, 100);

    public override void Update(float deltaTime)
    {
        if (_framesQueue == null || _framesQueue.IsCompleted)
            return;
        
        if (_framesQueue.TryTake(out var frame))
        {
            //_bufferTexture?.Dispose();
            if (_bufferTexture == null)
                _bufferTexture = new Texture(Gl, frame);
            else
                _bufferTexture.SetData(frame);
            frame.Dispose();
        }
    }

    protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
    {
        if (_bufferTexture == null)
            return;
        
        var cMin = contentMin + new Vector2(0, 20 * zoomLevel);
        var cMax = contentMax - new Vector2(0, 10 * zoomLevel);
        
        var aspect = (float)_bufferTexture.Height / _bufferTexture.Width;
        var imageWidth = cMax.X - cMin.X;
        var imageHeight = aspect * imageWidth;
        if (imageHeight > cMax.Y - cMin.Y)
        {
            imageHeight = cMax.Y - cMin.Y;
            imageWidth = imageHeight / aspect;
        }

        var imageSize = new Vector2(imageWidth, imageHeight);
        var imagePos = cMin + (new Vector2(cMax.X - cMin.X, cMax.Y - cMin.Y) - imageSize) / 2.0f;
        ImGui.SetCursorScreenPos(imagePos);
        ImGui.Image(_bufferTexture.Handle, imageSize);
    }
    
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