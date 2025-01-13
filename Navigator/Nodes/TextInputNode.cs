using System.Numerics;
using System.Text.Json;
using ImGuiNET;

namespace ProcessPipeline.Nodes;

public class TextInputNode : Node
{
    public string? Text { get; set; }
    public sealed override Vector2 DefaultSize => new Vector2(200, 100);
    public override string? Title { get; set; } = "Text Input Node";

    public TextInputNode(Vector2 pos, PortClickedHandler portClickedHandler) : base(pos, portClickedHandler)
    {
        Text = string.Empty;
        Size = DefaultSize;

        AddOutput("Output", DataType.String, () => Text);
    }
    
    // Parameterless constructor for deserialization
    public TextInputNode() : base(Vector2.Zero, null)
    {
        AddOutput("Output", DataType.String, () => Text);
    }
    
    protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
    {
        // Render the text input field
        var inputSize = new Vector2(200, 30) * zoomLevel;
        var inputPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - inputSize) / 2.0f;
        ImGui.SetCursorScreenPos(inputPos);
        
        // push item width
        ImGui.PushItemWidth(inputSize.X);
        var text = Text ?? string.Empty;
        if (ImGui.InputText($"##TextInput_Node_{Id}", ref text, 1000))
        {
            Text = text;
        }
    }

    public override string GetData()
    {
        return JsonSerializer.Serialize(Text);
    }

    public override void SetData(string? data)
    {
        if (data != null)
        {
            // Deserialize the JSON string back into a string
            Text = JsonSerializer.Deserialize<string>(data);
        }
        else
        {
            throw new ArgumentException("Invalid data format. Expected a JSON string.");
        }
    }
}