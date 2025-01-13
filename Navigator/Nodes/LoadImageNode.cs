using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ProcessPipeline.Nodes;

public class LoadImageNode : Node, IOpenGlNode
{
    public override Vector2 DefaultSize { get; } = new Vector2(200, 200);
    public override string Title { get; set; } = "Load Image Node";
    
    public string? ImagePath;
    public Image<Rgba32>? ImageData;
    private Texture? _bufferTexture;
    
    public LoadImageNode(Vector2 pos, PortClickedHandler? portClickedHandler) : base(pos, portClickedHandler)
    {
        // Add one input and one output port
        AddInput("Input", DataType.String, (data) => { ImagePath = data as string; });
        AddOutput("Output", DataType.Image, () => ImageData);
    }
    
    public LoadImageNode() : base(Vector2.Zero, null)
    {
        AddInput("Input", DataType.String, (data) => { ImagePath = data as string; });
        AddOutput("Output", DataType.Image, () => ImageData);
    }

    protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax,
        float zoomLevel)
    {
        var imageSize = new Vector2(200, 200) * zoomLevel;
        var imagePos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - imageSize) / 2.0f;
        ImGui.SetCursorScreenPos(imagePos);
        
        // push item width
        ImGui.PushItemWidth(imageSize.X);
        
        if (_bufferTexture != null)
            ImGui.Image(_bufferTexture.Handle, imageSize);
    }
    
    public override void Process()
    {
        ImageData?.Dispose();
        _bufferTexture?.Dispose();

        if (ImagePath != null)
        {
            ImageData = Image.Load<Rgba32>(ImagePath);
            _bufferTexture = new Texture(Gl, ImageData);
        }
        
        base.Process();
    }

    public GL Gl { get; set; }
}