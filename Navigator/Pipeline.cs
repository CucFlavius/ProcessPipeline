using ImGuiNET;
using Silk.NET.OpenGL;
using System.Numerics;

namespace ProcessPipeline;
using Nodes;

public class Pipeline
{
    private readonly InfiniteGrid _infiniteGrid;
    
    // Static counters for generating unique IDs
    public static uint NodeIdCounter = 1;
    public static uint PortIdCounter = 1001;
    
    private Dictionary<uint, Node> Nodes { get; }
    private List<Connection> Connections;

    private Vector2 _gridPosition;
    private float _zoomLevel;
    private bool _initialSetup;

    // Dragging connection state
    private NodePort _draggingPort;
    private Node _draggingNodePort;
    private bool _isDragging;
    private Vector2 _draggingPos;

    public Pipeline(GL gl)
    {
        Nodes = new Dictionary<uint, Node>();
        _infiniteGrid = new InfiniteGrid();
        _gridPosition = Vector2.Zero;
        _zoomLevel = 1.0f;
        _initialSetup = true;

        Connections = new List<Connection>();

        // Initialize nodes with custom content for debug purposes
        //Nodes.Add(Node.GenerateNodeID(), new LabelNode("Hello World", new Vector2(-325, -125), OnPortClicked));
        //Nodes.Add(Node.GenerateNodeID(), new ButtonNode("Click Me", new Vector2(100, 100), OnPortClicked));
        // var nodeA = new TextInputNode(new Vector2(-325, -125), OnPortClicked);
        // nodeA.Text = "aaaaaaaaaa";
        // Nodes.Add(nodeA.ID, nodeA);
        // var nodeB = new TextInputNode(new Vector2(-325, 125), OnPortClicked);
        // nodeB.Text = "bbbbbbbbbb";
        // Nodes.Add(nodeB.ID, nodeB);
        // var nodeC = new LabelNode("Hello World", new Vector2(100, -100), OnPortClicked);
        // Nodes.Add(nodeC.ID, nodeC);
    }
    
    /// <summary>
    /// Callback invoked when a port is clicked.
    /// </summary>
    private void OnPortClicked(NodePort port, Node node)
    {
        if (!_isDragging)
        {
            // Start dragging from this port
            _isDragging = true;
            _draggingPort = port;
            _draggingNodePort = node;
            _draggingPos = ImGui.GetIO().MousePos;
        }
        else
        {
            // Finish dragging and attempt to create a connection
            if (IsCompatible(_draggingPort, port))
            {
                if (_draggingPort.PType == PortType.Output && port.PType == PortType.Input)
                {
                    // Check if the input port is already connected
                    var existingConnection = Connections.FirstOrDefault(c => c.To == port);
                    if (existingConnection != null)
                    {
                        // Remove the existing connection
                        Connections.Remove(existingConnection);
                        existingConnection.From.RemoveConnection(existingConnection.To);
                        existingConnection.To.RemoveConnection(existingConnection.From);
                    }

                    // Establish the new connection
                    var newConnection = new Connection((OutputPort)_draggingPort, (InputPort)port);
                    Connections.Add(newConnection);

                    // Update the port connections
                    _draggingPort.AddConnection(port);
                    port.AddConnection(_draggingPort);
                }
                else if (_draggingPort.PType == PortType.Input && port.PType == PortType.Output)
                {
                    // Check if the input port is already connected
                    var existingConnection = Connections.FirstOrDefault(c => c.To == _draggingPort);
                    if (existingConnection != null)
                    {
                        // Remove the existing connection
                        Connections.Remove(existingConnection);
                        existingConnection.From.RemoveConnection(existingConnection.To);
                        existingConnection.To.RemoveConnection(existingConnection.From);
                    }

                    // Establish the new connection
                    var newConnection = new Connection((OutputPort)port, (InputPort)_draggingPort);
                    Connections.Add(newConnection);

                    // Update the port connections
                    port.AddConnection(_draggingPort);
                    _draggingPort.AddConnection(port);
                }
            }

            // Reset dragging state
            _isDragging = false;
            _draggingPort = null;
        }
    }

    /// <summary>
    /// Determines if two ports are compatible for connection.
    /// </summary>
    private bool IsCompatible(NodePort from, NodePort to)
    {
        // Define DataType compatibility rules, e.g., String to String only
        if (from.DType != to.DType)
            return false;
        
        // Define compatibility rules, e.g., Output to Input only
        if (from.PType == PortType.Output && to.PType == PortType.Input)
            return true;
        if (from.PType == PortType.Input && to.PType == PortType.Output)
            return true;
        return false;
    }

    public void Render()
    {
        ImGui.Begin("Pipeline");

        // Set font
        var fonts = ImGui.GetIO().Fonts;
        ImGui.PushFont(fonts.Fonts[1]); // Assuming Font[1] is the secondary font

        // Define the drawable area using a child region for better control
        Vector2 contentSize = ImGui.GetContentRegionAvail();

        // Begin child region
        ImGui.BeginChild("Canvas", contentSize, ImGuiChildFlags.None, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
        
        // Get the top-left position of the drawable area
        Vector2 canvasPos = ImGui.GetCursorScreenPos();

        // Draw background
        var drawList = ImGui.GetWindowDrawList();
        drawList.AddRectFilled(canvasPos, canvasPos + contentSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)));

        // Initialize grid position to center the origin on the first render
        if (_initialSetup)
        {
            _gridPosition = contentSize / 2.0f;
            _initialSetup = false;
        }

        // Check if the child window is hovered for input handling
        bool isHovered = ImGui.IsWindowHovered();

        var io = ImGui.GetIO();

        // Handle panning
        if (isHovered && ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
        {
            var mouseDelta = io.MouseDelta;
            _gridPosition += mouseDelta; // Adjust panning based on zoom if desired
        }

        // Handle zooming
        if (isHovered && io.MouseWheel != 0.0f)
        {
            var mousePos = io.MousePos;

            // Calculate mouse position relative to the canvas
            var mousePosInGrid = (mousePos - canvasPos - _gridPosition) / _zoomLevel;

            // Update zoom level with clamping
            var scaledMouseWheel = io.MouseWheel * 0.1f * _zoomLevel;
            _zoomLevel = Math.Clamp(_zoomLevel + scaledMouseWheel, 0.1f, 10.0f);

            // Scale the secondary font
            fonts.Fonts[1].Scale = _zoomLevel;

            // Recalculate grid position to keep the zoom centered on the mouse position
            var newMousePosInGrid = (mousePos - canvasPos - _gridPosition) / _zoomLevel;
            _gridPosition += (newMousePosInGrid - mousePosInGrid) * _zoomLevel;
        }

        uint backgroundColor = ImGui.ColorConvertFloat4ToU32(new Vector4(0.6f, 0.6f, 0.6f, 1.0f)); // Light gray background

        // Draw the background
        drawList.AddRectFilled(canvasPos, canvasPos + contentSize, backgroundColor);
        
        // Render the grid
        _infiniteGrid.Render(canvasPos, _gridPosition, _zoomLevel, contentSize);
        
        // Render all nodes
        foreach (var node in Nodes.Values)
        {
            node.Render(canvasPos, _gridPosition, _zoomLevel);
        }

        // Render temporary connection line if dragging
        if (_isDragging && _draggingPort != null)
        {
            int index = -1;
            if (_draggingPort is InputPort)
            {
                index = _draggingNodePort.Inputs.IndexOf(_draggingPort as InputPort);
            }
            else if (_draggingPort is OutputPort)
            {
                index = _draggingNodePort.Outputs.IndexOf(_draggingPort as OutputPort);
            }
            Vector2 startPos = _draggingPort.GetScreenPosition(canvasPos,
                _gridPosition,
                _zoomLevel,
                new Vector2(200,
                    300) *
                _zoomLevel,
                index); // Placeholder indices

            Vector2 endPos = ImGui.GetIO().MousePos;

            drawList.AddBezierCubic(
                startPos,
                startPos + new Vector2(50 * _zoomLevel, 0),
                endPos + new Vector2(-50 * _zoomLevel, 0),
                endPos,
                ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 0.0f, 1.0f)),
                2.0f,
                0
            );
        }
        
        // Render existing connections
        foreach (var connection in Connections)
        {
            connection.Render(drawList, canvasPos, _gridPosition, _zoomLevel);
        }

        ImGui.EndChild();
        
        // Reset font
        ImGui.PopFont();
        
        // Implement context menu
        if (ImGui.BeginPopupContextItem($"Popup_Canvas"))
        {
            if (ImGui.MenuItem("Add Text Input Node"))
            {
                // Calculate mouse position relative to the canvas
                var mousePosInGrid = (io.MousePos - canvasPos - _gridPosition) / _zoomLevel;
                
                var node = new TextInputNode(mousePosInGrid, OnPortClicked);
                Nodes.Add(node.ID, node);
            }
            if (ImGui.MenuItem("Add Label Node"))
            {
                // Calculate mouse position relative to the canvas
                var mousePosInGrid = (io.MousePos - canvasPos - _gridPosition) / _zoomLevel;
                
                var node = new LabelNode(string.Empty, mousePosInGrid, OnPortClicked);
                Nodes.Add(node.ID, node);
            }
            ImGui.EndPopup();
        }

        ImGui.End();
    }

    public void Update(float deltaTime)
    {
        foreach (var node in Nodes.Values)
        {
            node.Update(deltaTime);
        }
        
        if (_isDragging)
        {
            // stop on rmb
            if (ImGui.IsMouseDown(ImGuiMouseButton.Right))
            {
                _isDragging = false;
                _draggingPort = null;
            }
        }
        
        // DEBUG //
        Process();
    }

    public void Process()
    {
        // Determine root nodes
        var rootNodes = Nodes.Values.Where(n => n.Inputs.Count == 0).ToList();
        
        // Process root nodes
        foreach (var rootNode in rootNodes)
        {
            rootNode.Process();
        }
    }

    public void Serialize(string path)
    {
        if (File.Exists(path))
            File.Delete(path);
        
        using var sw = new StreamWriter(path);
        
        // Serialize nodes
        sw.WriteLine($"Nodes:{Nodes.Count}");
        foreach (var node in Nodes.Values)
        {
            var t = node.GetType();
            sw.WriteLine($"Node:{t}");
            sw.WriteLine($"Node.ID:{node.ID}");
            sw.WriteLine($"Node.Title:{node.Title}");
            sw.WriteLine($"Node.Position:{node.Position.X},{node.Position.Y}");
            sw.WriteLine($"Node.Size:{node.Size.X},{node.Size.Y}");
            var nodeData = node.GetData();
            if (nodeData != null)
                sw.WriteLine($"Node.Data:{nodeData}");
            sw.WriteLine($"Node.Inputs:{node.Inputs.Count}");
            foreach (var input in node.Inputs)
            {
                sw.WriteLine($"Input:{input.Name},{input.DType},{input.ID}");
            }
            sw.WriteLine($"Node.Outputs:{node.Outputs.Count}");
            foreach (var output in node.Outputs)
            {
                sw.WriteLine($"Output:{output.Name},{output.DType},{output.ID}");
            }
        }
        
        // Serialize connections
        sw.WriteLine($"Connections:{Connections.Count}");
        foreach (var connection in Connections)
        {
            sw.WriteLine($"Connection:{connection.From.ID},{connection.To.ID},{connection.From.ParentNode.ID},{connection.To.ParentNode.ID}");
        }
    }

    public void Deserialize(string path)
    {
        if (!File.Exists(path))
        {
            Console.WriteLine("File not found.");
            return;
        }

        using var sr = new StreamReader(path);

        // Clear existing nodes
        Nodes.Clear();
        Node? bufferNode = null;
        
        Connections.Clear();

        // Read and process each line
        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var lineParts = line.Split(':');
            var lineType = lineParts[0];

            switch (lineType)
            {
                case "Nodes":
                    var totalNodes = int.Parse(lineParts[1]);
                    break;
                case "Node":
                {
                    var typeName = lineParts[1];
                    var elementType = Type.GetType(typeName);

                    if (elementType == null)
                        continue;
                    
                    bufferNode = typeName switch
                    {
                        "ProcessPipeline.Nodes.TextInputNode" => new TextInputNode(),
                        "ProcessPipeline.Nodes.LabelNode" => new LabelNode(),
                        _ => (Node)Activator.CreateInstance(elementType)!
                    };
                    
                    bufferNode.PortClickedHandler = OnPortClicked;
                    break;
                }
                case "Node.ID" when bufferNode != null:
                    bufferNode.ID = uint.Parse(lineParts[1]);
                    Nodes.Add(bufferNode.ID, bufferNode);
                    break;
                case "Node.Title" when bufferNode != null:
                    bufferNode.Title = lineParts[1];
                    break;
                case "Node.Position" when bufferNode != null:
                {
                    var positionParts = lineParts[1].Split(',');
                    bufferNode.Position = new Vector2(
                        float.Parse(positionParts[0]),
                        float.Parse(positionParts[1])
                    );
                    break;
                }
                case "Node.Size" when bufferNode != null:
                {
                    var sizeParts = lineParts[1].Split(',');
                    bufferNode.Size = new Vector2(
                        float.Parse(sizeParts[0]),
                        float.Parse(sizeParts[1])
                    );
                    break;
                }
                case "Node.Data" when bufferNode != null:
                    bufferNode.SetData(lineParts[1]);
                    break;
                case "Node.Inputs" when bufferNode != null:
                {
                    var inputCount = int.Parse(lineParts[1]);
                    for (var i = 0; i < inputCount; i++)
                    {
                        var inputLine = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(inputLine))
                            continue;
                        var inputParts = inputLine.Split(',');
                        bufferNode.Inputs[i].ID = uint.Parse(inputParts[2]);
                        NodePort.UpdatePortIDCounter(bufferNode.Inputs[i].ID);
                    }

                    break;
                }
                case "Node.Outputs" when bufferNode != null:
                {
                    var outputCount = int.Parse(lineParts[1]);
                    for (var i = 0; i < outputCount; i++)
                    {
                        var outputLine = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(outputLine))
                            continue;
                        var outputParts = outputLine.Split(',');
                        bufferNode.Outputs[i].ID = uint.Parse(outputParts[2]);
                        NodePort.UpdatePortIDCounter(bufferNode.Outputs[i].ID);
                    }

                    break;
                }
                case "Connections":
                    var totalConnections = int.Parse(lineParts[1]);
                    break;
                case "Connection":
                {
                    var connectionParts = lineParts[1].Split(',');
                    var fromId = uint.Parse(connectionParts[0]);
                    var toId = uint.Parse(connectionParts[1]);
                    var fromNode = uint.Parse(connectionParts[2]);
                    var toNode = uint.Parse(connectionParts[3]);

                    OutputPort? fromPort = null;
                    if (Nodes.TryGetValue(fromNode, out var onode))
                    {
                        fromPort = onode.Outputs.FirstOrDefault(p => p.ID == fromId);
                    }
                    InputPort? toPort = null;
                    if (Nodes.TryGetValue(toNode, out var inode))
                    {
                        toPort = inode.Inputs.FirstOrDefault(p => p.ID == toId);
                    }

                    if (fromPort != null && toPort != null)
                    {
                        // Establish the new connection
                        var newConnection = new Connection((OutputPort)fromPort, (InputPort)toPort);
                        Connections.Add(newConnection);

                        // Update the port connections
                        fromPort.AddConnection(toPort);
                        toPort.AddConnection(fromPort);
                    }
                    break;
                }
            }
        }
    }
}