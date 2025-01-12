using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes
{
    public class LabelNode : Node
    {
        private string _label;

        public LabelNode(string label, Vector2 pos, PortClickedHandler portClickedHandler) : base(pos, portClickedHandler)
        {
            _title = "Label Node";
            _label = label;

            // Add one input and one output port
            AddInput("Input1");
            AddOutput("Output1");
            AddInput("Input2");
            AddOutput("Output2");
        }

        protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
        {
            // Calculate position for the label text
            string nodeContent = _label;
            Vector2 contentTextSize = ImGui.CalcTextSize(nodeContent);
            Vector2 contentTextPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - contentTextSize) / 2.0f;
            
            // Fallback to default font if the specified font is not found
            drawList.AddText(contentTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), nodeContent);
        }
    }
}