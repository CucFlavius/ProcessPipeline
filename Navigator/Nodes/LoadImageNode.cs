using System.Numerics;
using ImGuiNET;
using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ProcessPipeline.Nodes;

public class LoadImageNode : Node, IOpenGlNode
{
    public sealed override Vector2 DefaultSize { get; } = new Vector2(200, 200);
    public override string? Title { get; set; } = "Load Image Node";
    public override Flags flags => Flags.ResizableX | Flags.ResizableY;

    private string? _imagePath;
    private Image<Rgba32>? _imageData;
    private Texture? _bufferTexture;
    
    public LoadImageNode(Vector2 pos, PortClickedHandler? portClickedHandler) : base(pos, portClickedHandler)
    {
        Size = DefaultSize;
        
        // Add one input and one output port
        AddInput("Input", DataType.String, (data) => { _imagePath = data as string; });
        AddOutput("Output", DataType.Image, () => _imageData);
    }
    
    public LoadImageNode() : base(Vector2.Zero, null)
    {
        AddInput("Input", DataType.String, (data) => { _imagePath = data as string; });
        AddOutput("Output", DataType.Image, () => _imageData);
    }

    protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax,
        float zoomLevel)
    {
        if (_imageData == null)
            return;
        
        if (_bufferTexture == null)
            return;

        var aspect = _bufferTexture.Height / _bufferTexture.Width;
        var imageSize = new Vector2(Math.Min(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y * aspect), Math.Min(contentMax.Y - contentMin.Y, contentMax.X - contentMin.X / aspect));
        var imagePos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - imageSize) / 2.0f;
        ImGui.SetCursorScreenPos(imagePos);
        
        // push item width
        ImGui.PushItemWidth(imageSize.X);
        
        ImGui.Image(_bufferTexture.Handle, imageSize);
    }
    
    public override void Process()
    {
        _imageData?.Dispose();
        _bufferTexture?.Dispose();

        if (_imagePath != null)
        {
            if (File.Exists(_imagePath))
            {
                _imageData = Image.Load<Rgba32>(_imagePath);
                _bufferTexture = new Texture(Gl, _imageData);
            }
        }
        
        base.Process();
    }

    public GL? Gl { get; set; }
}