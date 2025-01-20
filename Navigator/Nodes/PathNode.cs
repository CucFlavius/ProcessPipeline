using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using ImGuiNET;
using NativeFileDialogSharp;

namespace ProcessPipeline.Nodes;

public class PathNode : Node
{
    public string? Path { get; set; }
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
        // Render the text input field
        var inputSize = new Vector2(contentMax.X - contentMin.X, 30 * zoomLevel);
        var inputPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - inputSize) / 2.0f;
        ImGui.SetCursorScreenPos(inputPos);
        
        // push item width
        ImGui.PushItemWidth(inputSize.X - 100 * zoomLevel);
        var text = Path ?? string.Empty;
        
        if (ImGui.Button("Browse"))
        {
            try
            {
                // Open folder picker
                var result = Dialog.FileOpen();
                if (result.IsOk && !string.IsNullOrEmpty(result.Path))
                {
                    Path = result.Path;
                    text = result.Path;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error opening folder picker: {ex.Message}");
            }
        }
        
        ImGui.SameLine();
        if (ImGui.InputText($"##TextInput_Node_{Id}", ref text, 1000))
        {
            Path = text;
        }
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