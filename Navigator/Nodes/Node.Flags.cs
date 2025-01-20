namespace ProcessPipeline.Nodes;

public abstract partial class Node
{
    [Flags]
    public enum Flags
    {
        None = 0,
        ResizableX = 1 << 0,
        ResizableY = 1 << 1,
    }
}