using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace ProcessPipeline;

public class Ui
{
    private readonly GL _gl;
    private readonly ImGuiController? _controller;
    private readonly Pipeline _pipeline;

    public Ui(GL gl, IWindow window, IInputContext inputContext, Pipeline pipeline)
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
    
    const float toolbarSize = 30;
    const float menuBarHeight = 0;
    
    void ToolbarUI()
    {
        ImGuiViewportPtr viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new Vector2(viewport.Pos.X, viewport.Pos.Y + menuBarHeight));
        ImGui.SetNextWindowSize(new Vector2(viewport.Size.X, toolbarSize));
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGuiWindowFlags window_flags = 0
                                        | ImGuiWindowFlags.NoDocking 
                                        | ImGuiWindowFlags.NoTitleBar 
                                        | ImGuiWindowFlags.NoResize 
                                        | ImGuiWindowFlags.NoMove 
                                        | ImGuiWindowFlags.NoScrollbar 
                                        | ImGuiWindowFlags.NoSavedSettings;
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(1, 1));
        ImGui.Begin("TOOLBAR", window_flags);
        ImGui.PopStyleVar();
  
        
        ImGui.SameLine();
        if (ImGui.Button("Run", new Vector2(100, toolbarSize)))
        {
            _pipeline.Process();
        }
        
        ImGui.SameLine();
        if (ImGui.Button("Save Pipeline", new Vector2(0, toolbarSize)))
        {
            _pipeline.Serialize("pipeline.txt");
        }

        ImGui.SameLine();
        if (ImGui.Button("Load Pipeline", new Vector2(0, toolbarSize)))
        {
            _pipeline.Deserialize("pipeline.txt");
        }
        
        ImGui.End();
    }
    
    public void Render()
    {
        ToolbarUI();
        _pipeline.Render();
        _controller?.Render();
    }
    
    public void Dispose()
    {
        _controller?.Dispose();
    }
}