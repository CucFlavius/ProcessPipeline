using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes
{
    public enum PortType
    {
        Input,
        Output
    }

    public enum DataType
    {
        String,
    }

    public class NodePort
    {
        public string Name { get; set; }
        public PortType PType { get; set; }
        public DataType DType { get; set; }
        public Node ParentNode { get; set; }
        
        public bool IsConnected { get; set; }
        public NodePort? ConnectedPort { get; set; }
        
        public uint ID { get; set; } // Unique identifier
        
        public NodePort(string name, PortType pType, DataType dType, Node parent)
        {
            Name = name;
            PType = pType;
            DType = dType;
            ParentNode = parent;
            ID = GeneratePortID();
        }

        private static uint _portIdCounter = 1;

        private static uint GeneratePortID()
        {
            return _portIdCounter++;
        }

        /// <summary>
        /// Calculates the screen position of the port based on the node's position, canvas position, zoom level, and port index.
        /// </summary>
        public Vector2 GetScreenPosition(Vector2 canvasPos, Vector2 gridPosition, float zoomLevel, Vector2 nodeSize, int index)
        {
            // Calculate the position of the port on the node
            // For input ports, place on the left; for output, on the right
            // index and total help distribute multiple ports

            //float spacing = nodeSize.Y / (total + 1);
            //float yPos = ParentNode._nodePos.Y * zoomLevel + spacing * (index + 1) + canvasPos.Y + gridPosition.Y;
            float nodeSpacing = 20 * zoomLevel;
            float yPos = ParentNode.NodePos.Y * zoomLevel + (index * nodeSpacing) + canvasPos.Y + gridPosition.Y + (40 * zoomLevel);
            
            float xPos;
            if (PType == PortType.Input)
                xPos = canvasPos.X + gridPosition.X + ParentNode.NodePos.X * zoomLevel + (10 * zoomLevel);
            else // Output
                xPos = canvasPos.X + gridPosition.X + ParentNode.NodePos.X * zoomLevel - (10 * zoomLevel) + nodeSize.X;

            return new Vector2(xPos, yPos);
        }
        
        public ReadOnlySpan<char> GetPortName()
        {
            return $"{Name} {ID}".AsSpan();
        }
    }

    public class InputPort : NodePort
    {
        public Action<object> setData { get; set; }
        
        public InputPort(string name, DataType dType, Node parent, Action<object> setData) : base(name, PortType.Input, dType, parent)
        {
            this.setData = setData;
        }
    }

    public class OutputPort : NodePort
    {
        public Func<object?> getData { get; set; }
        
        public OutputPort(string name, DataType dType, Node parent, Func<object?> data) : base(name, PortType.Output, dType, parent)
        {
            getData = data;
        }
    }
}
