using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using ImGuiNET;
using NativeFileDialogSharp;

namespace ProcessPipeline.Nodes;

public class PathNode : Node
{
    public string? Path;
    public sealed override Vector2 DefaultSize => new Vector2(300, 100);
    public override string? Title { get; set; } = "Text Input Node";
    public override Flags flags => Flags.ResizableX;

    public PathNode(Vector2 pos, PortClickedHandler portClickedHandler) : base(pos, portClickedHandler)
    {
        Path = string.Empty;
        Size = DefaultSize;

        AddOutput("Output", DataType.String, () => Path);
    }
    
    // Parameterless constructor for deserialization
    public PathNode() : base(Vector2.Zero, null)
    {
        AddOutput("Output", DataType.String, () => Path);
    }
    
    protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
    {
        var contentPos = contentMin + new Vector2(10 * zoomLevel, 0);
        var contentWidth = contentMax.X - contentMin.X - 20 * zoomLevel;
        DrawPathInput(contentPos + new Vector2(0, 20 * zoomLevel), contentWidth, zoomLevel, ref Path);
    }

    public override string GetData()
    {
        return JsonSerializer.Serialize(Path);
    }

    public override void SetData(string? data)
    {
        if (data != null)
        {
            // Deserialize the JSON string back into a string
            Path = JsonSerializer.Deserialize<string>(data);
        }
        else
        {
            throw new ArgumentException("Invalid data format. Expected a JSON string.");
        }
    }
}