using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes
{
    public class Connection
    {
        public OutputPort? From { get; init; }
        public InputPort? To { get; init; }

        public Connection(OutputPort? from, InputPort? to)
        {
            From = from;
            To = to;
        }
        
        public Connection()
        {
        }

        /// <summary>
        /// Renders the connection as a Bézier curve between two ports.
        /// </summary>
        public void Render(ImDrawListPtr drawList, Vector2 canvasPos, Vector2 gridPosition, float zoomLevel)
        {
            // Calculate node size (assume same as Node class)
            var nodeSize = new Vector2(200, 300) * zoomLevel;

            if (From == null || To == null)
            {
                return;
            }
            
            // Get screen positions of the ports
            var fromIndex = From.ParentNode.Outputs.IndexOf(From);
            var toIndex = To.ParentNode.Inputs.IndexOf(To);
            var fromPos = From.GetScreenPosition(canvasPos, gridPosition, zoomLevel, nodeSize, fromIndex); // index and total not needed here
            var toPos = To.GetScreenPosition(canvasPos, gridPosition, zoomLevel, nodeSize, toIndex);

            // Draw a Bézier curve between fromPos and toPos
            drawList.AddBezierCubic(
                fromPos,
                fromPos + new Vector2(50 * zoomLevel, 0),
                toPos + new Vector2(-50 * zoomLevel, 0),
                toPos,
                ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 1.0f)), // Yellow color
                2.0f, // Thickness
                0 // Num segments (default)
            );
        }
    }
}