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
        Image,
    }
    
    public class NodePort
    {
        public uint Id { get; set; } // Unique identifier
        public string Name { get; set; }
        public PortType PType { get; set; }
        public DataType DType { get; set; }
        public Node ParentNode { get; set; }
        public List<NodePort?>? ConnectedPorts { get; set; } // Only for OutputPorts
        private NodePort? ConnectedPort { get; set; } // Only for InputPorts

        protected NodePort(string name, PortType pType, DataType dType, Node parent)
        {
            Name = name;
            PType = pType;
            DType = dType;
            ParentNode = parent;
            Id = GeneratePortId();

            if (PType == PortType.Output)
                ConnectedPorts = [];
            else
                ConnectedPort = null;
        }

        private static uint _portIdCounter = 1;

        private static uint GeneratePortId()
        {
            return _portIdCounter++;
        }

        /// <summary>
        /// Adds a connection to this port.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void AddConnection(NodePort? port)
        {
            switch (PType)
            {
                case PortType.Output:
                {
                    if (ConnectedPorts != null && !ConnectedPorts.Contains(port))
                        ConnectedPorts.Add(port);
                    break;
                }
                case PortType.Input:
                {
                    ConnectedPort ??= port;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Removes a connection from this port.
        /// </summary>
        public void RemoveConnection(NodePort? port)
        {
            switch (PType)
            {
                case PortType.Output:
                {
                    if (ConnectedPorts != null && ConnectedPorts.Contains(port))
                        ConnectedPorts.Remove(port);
                    break;
                }
                case PortType.Input:
                {
                    if (ConnectedPort == port)
                        ConnectedPort = null;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
            var nodeSpacing = 20 * zoomLevel;
            var yPos = ParentNode.Position.Y * zoomLevel + (index * nodeSpacing) + canvasPos.Y + gridPosition.Y + (40 * zoomLevel);
            
            float xPos;
            if (PType == PortType.Input)
                xPos = canvasPos.X + gridPosition.X + ParentNode.Position.X * zoomLevel + (10 * zoomLevel);
            else // Output
                xPos = canvasPos.X + gridPosition.X + ParentNode.Position.X * zoomLevel - (10 * zoomLevel) + nodeSize.X;

            return new Vector2(xPos, yPos);
        }
        
        public ReadOnlySpan<char> GetPortName()
        {
            return $"{Name} {Id}".AsSpan();
        }

        public static void UpdatePortIdCounter(uint maxPortId)
        {
            _portIdCounter = Math.Max(_portIdCounter, maxPortId);
        }
    }
    
    public class InputPort : NodePort
    {
        public Action<object> SetData { get; set; }
        
        public InputPort(string name, DataType dType, Node parent, Action<object> setData) : base(name, PortType.Input, dType, parent)
        {
            this.SetData = setData;
        }
    }
    
    public class OutputPort : NodePort
    {
        public Func<object?> GetData { get; set; }
        
        public OutputPort(string name, DataType dType, Node parent, Func<object?> data) : base(name, PortType.Output, dType, parent)
        {
            GetData = data;
        }
    }
}
