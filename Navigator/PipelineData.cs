using ProcessPipeline.Nodes;

namespace ProcessPipeline;

public class PipelineData
{
    public List<Node> Nodes { get; set; }
    public List<Connection> Connections { get; set; }
}