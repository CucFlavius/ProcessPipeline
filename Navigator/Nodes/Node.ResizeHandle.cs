using System.Numerics;

namespace ProcessPipeline.Nodes;

public abstract partial class Node
{
    public struct ResizeHandle
    {
        public Type IdSuffix; // Unique identifier suffix
        public Vector2 Size;    // Size of the handle
        public Vector2 Direction; // Direction vector indicating resize axes

        public enum Type
        {
            Left,
            Right,
            Bottom,
            BottomLeft,
            BottomRight
        }
        
        public ResizeHandle(Type idSuffix, Vector2 size, Vector2 direction)
        {
            IdSuffix = idSuffix;
            Size = size;
            Direction = direction;
        }
    }
    
    // Define a fixed size for handles to minimize per-frame calculations
    private const float HANDLE_THICKNESS = 10.0f;
    private Vector2 _resizeMouseStartPos;
    private Vector2 _resizeFrameStartPos;
    private Vector2 _resizeFrameStartSize;

    // Predefined resize handles (excluding top and overlapping corners)
    private static readonly ResizeHandle[] ResizeHandles =
    [
        // Side Handles
        new ResizeHandle(ResizeHandle.Type.Left, Vector2.Zero, new Vector2(-1, 0)), // Height adjusted dynamically
        new ResizeHandle(ResizeHandle.Type.Right, Vector2.Zero, new Vector2(1, 0)), // Height adjusted dynamically
        new ResizeHandle(ResizeHandle.Type.Bottom, Vector2.Zero, new Vector2(0, 1)), // Width adjusted dynamically

        // Corner Handles
        new ResizeHandle(ResizeHandle.Type.BottomLeft, new Vector2(HANDLE_THICKNESS, HANDLE_THICKNESS), new Vector2(-1, 1)),
        new ResizeHandle(ResizeHandle.Type.BottomRight, new Vector2(HANDLE_THICKNESS, HANDLE_THICKNESS), new Vector2(1, 1))
    ];
}