using System.Numerics;
using ImGuiNET;

namespace ProcessPipeline;

public class InfiniteGrid
{
    public static void Render(Vector2 canvasPos, Vector2 position, float zoom, Vector2 contentSize)
    {
        var drawList = ImGui.GetWindowDrawList();
        var gridSize = 50.0f * zoom; // Grid size adjusted by zoom
        var gridColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)); // Gray grid lines
        
        // Calculate panning offsets
        var offsetX = position.X % gridSize;
        var offsetY = position.Y % gridSize;

        // Apply clipping to the drawable area for performance
        drawList.PushClipRect(canvasPos, canvasPos + contentSize, true);

        // Draw vertical grid lines
        for (var x = canvasPos.X + offsetX; x < canvasPos.X + contentSize.X; x += gridSize)
        {
            drawList.AddLine(new Vector2(x, canvasPos.Y), new Vector2(x, canvasPos.Y + contentSize.Y), gridColor);
        }

        // Draw horizontal grid lines
        for (var y = canvasPos.Y + offsetY; y < canvasPos.Y + contentSize.Y; y += gridSize)
        {
            drawList.AddLine(new Vector2(canvasPos.X, y), new Vector2(canvasPos.X + contentSize.X, y), gridColor);
        }

        // Draw the origin circle at (0,0)
        var originScreenPos = canvasPos + position;
        const float originRadius = 5.0f; // Fixed radius in pixels
        var originColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f)); // Red color
        const int numSegments = 16;
        const float thickness = 2.0f;

        drawList.AddCircle(originScreenPos, originRadius, originColor, numSegments, thickness);

        // Remove clipping
        drawList.PopClipRect();
    }
}