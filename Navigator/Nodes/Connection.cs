using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes
{
    public class Connection
    {
        public OutputPort From { get; set; }
        public InputPort To { get; set; }

        public Connection(OutputPort from, InputPort to)
        {
            From = from;
            To = to;
        }

        /// <summary>
        /// Renders the connection as a bezier curve between two ports.
        /// </summary>
        public void Render(ImDrawListPtr drawList, Vector2 canvasPos, Vector2 gridPosition, float zoomLevel)
        {
            // Calculate node size (assume same as Node class)
            Vector2 nodeSize = new Vector2(200, 300) * zoomLevel;

            // Get screen positions of the ports
            Vector2 fromPos = From.GetScreenPosition(canvasPos, gridPosition, zoomLevel, nodeSize, 0, 0); // index and total not needed here
            Vector2 toPos = To.GetScreenPosition(canvasPos, gridPosition, zoomLevel, nodeSize, 0, 0);

            // Draw a bezier curve between fromPos and toPos
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