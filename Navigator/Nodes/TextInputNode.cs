using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using ImGuiNET;

namespace ProcessPipeline.Nodes;

public class TextInputNode : Node
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
    
    public TextInputNode(Vector2 pos, PortClickedHandler portClickedHandler) : base(pos, portClickedHandler)
    {
        Title = "Text Input Node";
        Text = string.Empty;
        NodeSize = new Vector2(200, 100);
        
        AddOutput("Output", DataType.String, () => Text);
    }
    
    // Parameterless constructor for deserialization
    public TextInputNode() : base(Vector2.Zero, null)
    {
    }
    
    protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
    {
        unsafe
        {
            // Render the text input field
            Vector2 inputSize = new Vector2(200, 30) * zoomLevel;
            Vector2 inputPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - inputSize) / 2.0f;
            ImGui.SetCursorScreenPos(inputPos);
        
            // push item width
            ImGui.PushItemWidth(inputSize.X);
            var text = Text;
            if (ImGui.InputText($"##TextInput_Node_{ID}", ref text, 1000))
            {
                Text = text;
            }
        }
    }
    
    public override void Process()
    {
        base.Process();
    }
}