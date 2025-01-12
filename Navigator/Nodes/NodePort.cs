using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes
{
    public enum PortType
    {
        Input,
        Output
    }

    public class NodePort
    {
        public string Name { get; set; }
        public PortType Type { get; set; }
        public Node ParentNode { get; set; }
        public uint ID { get; set; } // Unique identifier
        
        public NodePort(string name, PortType type, Node parent)
        {
            Name = name;
            Type = type;
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
            float yPos = ParentNode._nodePos.Y * zoomLevel + (index * nodeSpacing) + canvasPos.Y + gridPosition.Y + (40 * zoomLevel);
            
            float xPos;
            if (Type == PortType.Input)
                xPos = canvasPos.X + gridPosition.X + ParentNode._nodePos.X * zoomLevel + (10 * zoomLevel);
            else // Output
                xPos = canvasPos.X + gridPosition.X + ParentNode._nodePos.X * zoomLevel - (10 * zoomLevel) + nodeSize.X;

            return new Vector2(xPos, yPos);
        }
        
        public ReadOnlySpan<char> GetPortName()
        {
            return $"{Name} {ID}".AsSpan();
        }
    }

    public class InputPort : NodePort
    {
        public InputPort(string name, Node parent) : base(name, PortType.Input, parent)
        {
        }
    }

    public class OutputPort : NodePort
    {
        public OutputPort(string name, Node parent) : base(name, PortType.Output, parent)
        {
        }
    }
}
