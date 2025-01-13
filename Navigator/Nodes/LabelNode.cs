using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes
{
    public class LabelNode : Node
    {
        public string? Text { get; set; }
        public override Vector2 DefaultSize { get; } = new Vector2(200, 100);
        public override string Title { get; set; } = "Label Node";

        public LabelNode(string? text, Vector2 pos, PortClickedHandler portClickedHandler) : base(pos, portClickedHandler)
        {
            Text = text;

            // Add one input and one output port
            AddInput("Input", DataType.String, (data) => { Text = data as string; });
            AddOutput("Output", DataType.String, () => Text);
        }
        
        // Parameterless constructor for deserialization
        public LabelNode() : base(Vector2.Zero, null)
        {
            AddInput("Input", DataType.String, (data) => { Text = data as string; });
            AddOutput("Output", DataType.String, () => Text);
        }
        
        protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
        {
            // Calculate position for the label text
            var nodeContent = Text ?? string.Empty;
            var contentTextSize = ImGui.CalcTextSize(nodeContent);
            var contentTextPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - contentTextSize) / 2.0f;
            
            // Fallback to default font if the specified font is not found
            drawList.AddText(contentTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), nodeContent);
        }
        
        public override void Process()
        {
            //Do some work (in case of LabelNode, there is no work to be done)
            base.Process();
        }
    }
}