using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Diagnostics;

namespace ProcessPipeline;

public abstract class Program
{
    private static Pipeline? _pipeline;
    private static IWindow? _window;
    private static GL? _gl;
    private static Ui? _ui;

    private static IInputContext? _inputContext;

    private static readonly Vector4D<float> _clearColor = new (0.45f * 255, 0.55f * 255, 0.6f * 255, 1.0f * 255);

    private static int _frameCount = 0;
    private static double _elapsedTime = 0.0;

    private static void Main(string[] _)
    {
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1280, 720),
            Title = "My first Silk.NET application!"
        };
        
        _window = Window.Create(options);
        _window.Load += OnLoad;
        _window.Update += OnUpdate;
        _window.Render += OnRender;
        _window.FramebufferResize += OnFrameBufferResize;
        _window.Closing += OnWindowClosing;

        _window.Run();
    }

    private static void OnLoad()
    {
        if (_window == null)
            throw new InvalidOperationException("Window is not initialized.");

        _window.Center();
        _gl = _window.CreateOpenGL();
        _inputContext = _window.CreateInput();
        _pipeline = new Pipeline(_gl);
        _ui = new Ui(_gl, _window, _inputContext, _pipeline);

        foreach (var iKeyboard in _inputContext.Keyboards)
            iKeyboard.KeyDown += KeyDown;

        _gl.ClearColor(_clearColor);
    }

    private static void KeyDown(IKeyboard keyboard, Key key, int keyCode)
    {
        if (key == Key.Escape)
            _window?.Close();
    }

    private static void OnUpdate(double deltaTime)
    {
        _ui?.Update((float)deltaTime);
        _pipeline?.Update((float)deltaTime);

        _frameCount++;
        _elapsedTime += deltaTime;

        if (_elapsedTime >= 1.0)
        {
            var fps = _frameCount / _elapsedTime;
            _window!.Title = $"Navigator {fps:F2}";
            _frameCount = 0;
            _elapsedTime = 0.0;
        }
    }

    private static void OnRender(double deltaTime)
    {
        if (_gl == null)
            throw new InvalidOperationException("OpenGL is not initialized.");

        _gl.ClearColor(_clearColor);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        _ui?.Render();
    }

    private static void OnFrameBufferResize(Vector2D<int> size)
    {
        if (_gl == null)
            throw new InvalidOperationException("OpenGL is not initialized.");

        _gl.Viewport(size);
    }

    private static void OnWindowClosing()
    {
        _ui?.Dispose();
        _inputContext?.Dispose();
        _gl?.Dispose();
    }
}