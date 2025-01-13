using Silk.NET.OpenGL;

namespace ProcessPipeline.Nodes;

public interface IOpenGlNode
{
    public GL Gl { get; set; }
}