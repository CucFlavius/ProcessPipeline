using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes;

public class InfiniteGrid
{
    public void Render(Vector2 canvasPos, Vector2 position, float zoom, Vector2 contentSize)
    {
        var drawList = ImGui.GetWindowDrawList();
        float gridSize = 50.0f * zoom; // Grid size adjusted by zoom
        uint gridColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.5f, 0.5f, 0.5f, 1.0f)); // Gray grid lines
        uint backgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, 1.0f)); // Light gray background

        // Draw the background
        drawList.AddRectFilled(canvasPos, canvasPos + contentSize, backgroundColor);

        // Calculate panning offsets
        float offsetX = position.X % gridSize;
        float offsetY = position.Y % gridSize;

        // Apply clipping to the drawable area for performance
        drawList.PushClipRect(canvasPos, canvasPos + contentSize, true);

        // Draw vertical grid lines
        for (float x = canvasPos.X + offsetX; x < canvasPos.X + contentSize.X; x += gridSize)
        {
            drawList.AddLine(new Vector2(x, canvasPos.Y), new Vector2(x, canvasPos.Y + contentSize.Y), gridColor);
        }

        // Draw horizontal grid lines
        for (float y = canvasPos.Y + offsetY; y < canvasPos.Y + contentSize.Y; y += gridSize)
        {
            drawList.AddLine(new Vector2(canvasPos.X, y), new Vector2(canvasPos.X + contentSize.X, y), gridColor);
        }

        // Draw the origin circle at (0,0)
        Vector2 originScreenPos = canvasPos + position;
        float originRadius = 5.0f; // Fixed radius in pixels
        uint originColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 0.0f, 0.0f, 1.0f)); // Red color
        int numSegments = 16;
        float thickness = 2.0f;

        drawList.AddCircle(originScreenPos, originRadius, originColor, numSegments, thickness);

        // Remove clipping
        drawList.PopClipRect();
    }
}