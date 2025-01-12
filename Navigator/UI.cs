using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace ProcessPipeline;

public class UI
{
    private readonly GL _gl;
    private readonly ImGuiController? _controller;
    private Texture? _testTexture;
    private readonly Pipeline _pipeline;

    public UI(GL gl, IWindow window, IInputContext inputContext, Pipeline pipeline)
    {
        _gl = gl;
        var baseFont = new ImGuiFontConfig(@".\Font\Roboto-Medium.ttf", 14);
        var canvasFont = new ImGuiFontConfig(@".\Font\Roboto-Medium.ttf", 14);
        _controller = new ImGuiController(gl, window, inputContext, baseFont, () =>
        {
            // Create second font to use with the canvas
            var io = ImGui.GetIO();
            var glyphRange = canvasFont.GetGlyphRange?.Invoke(io) ?? default;

            io.Fonts.AddFontFromFileTTF(canvasFont.FontPath, canvasFont.FontSize, null, glyphRange);
        });
        //_controller = new ImGuiController(gl, window, inputContext);
        
        _pipeline = pipeline;
    }

    public void Update(float deltaTime)
    {
        // Feed the input events to our ImGui controller, which passes them through to ImGui.
        _controller?.Update(deltaTime);
    }
    
    public void Render()
    {
        _pipeline.Render();
        _controller?.Render();
    }
    
    public void Dispose()
    {
        _controller?.Dispose();
    }
}