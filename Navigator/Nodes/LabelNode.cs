﻿using ImGuiNET;
using System.Numerics;
using System.Text.Json.Serialization;

namespace ProcessPipeline.Nodes
{
    public class LabelNode : Node
    {
        [JsonPropertyName("label")]
        public string? Text { get; set; }

        public LabelNode(string? text, Vector2 pos, PortClickedHandler portClickedHandler) : base(pos, portClickedHandler)
        {
            Title = "Label Node";
            Text = text;

            // Add one input and one output port
            AddInput("Input", DataType.String, (data) => { Text = data as string; });
            AddOutput("Output", DataType.String, () => Text);
        }
        
        // Parameterless constructor for deserialization
        public LabelNode() : base(Vector2.Zero, null)
        {
        }

        protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
        {
            // Calculate position for the label text
            string? nodeContent = Text;
            Vector2 contentTextSize = ImGui.CalcTextSize(nodeContent);
            Vector2 contentTextPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - contentTextSize) / 2.0f;
            
            // Fallback to default font if the specified font is not found
            drawList.AddText(contentTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), nodeContent);
        }
        
        public override void Process()
        {
            //Text = "hehe";
            base.Process();
        }
    }
}