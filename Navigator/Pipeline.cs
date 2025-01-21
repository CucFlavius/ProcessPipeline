using ImGuiNET;
using Silk.NET.OpenGL;
using System.Numerics;

namespace ProcessPipeline;
using Nodes;

public class Pipeline
{
    private Dictionary<uint, Node> Nodes { get; }
    private readonly List<Connection> _connections;

    private Vector2 _gridPosition;
    private float _zoomLevel;
    private bool _initialSetup;

    // Dragging connection state
    private NodePort? _draggingPort;
    private Node? _draggingNodePort;
    private bool _isDragging;
    private readonly GL? _gl;

    public Pipeline(GL? gl)
    {
        Nodes = new Dictionary<uint, Node>();
        _gridPosition = Vector2.Zero;
        _zoomLevel = 1.0f;
        _initialSetup = true;
        _gl = gl;

        _connections = [];

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
        // var pathNode = new TextInputNode(new Vector2(-325, -125), OnPortClicked)
        // {
        //     Text = "Lichtenstein_img_processing_test.png"
        // };
        // Nodes.Add(pathNode.Id, pathNode);
        // var imageNode = new LoadImageNode(new Vector2(100, -125), OnPortClicked);
        // (imageNode as IOpenGlNode).Gl = gl;
        // Nodes.Add(imageNode.Id, imageNode);
    }
    
    /// <summary>
    /// Callback invoked when a port is clicked.
    /// </summary>
    private void OnPortClicked(NodePort? port, Node? node)
    {
        if (!_isDragging)
        {
            // Start dragging from this port
            _isDragging = true;
            _draggingPort = port;
            _draggingNodePort = node;
        }
        else
        {
            // Finish dragging and attempt to create a connection
            if (IsCompatible(_draggingPort, port))
            {
                if (_draggingPort != null)
                {
                    switch (_draggingPort.PType)
                    {
                        case PortType.Output when port is { PType: PortType.Input }:
                        {
                            // Check if the input port is already connected
                            var existingConnection = _connections.FirstOrDefault(c => c.To == port);
                            if (existingConnection != null)
                            {
                                // Remove the existing connection
                                _connections.Remove(existingConnection);
                                existingConnection.From?.RemoveConnection(existingConnection.To);
                                existingConnection.To?.RemoveConnection(existingConnection.From);
                            }

                            // Establish the new connection
                            var newConnection = new Connection((OutputPort)_draggingPort, (InputPort)port);
                            _connections.Add(newConnection);

                            // Update the port connections
                            _draggingPort.AddConnection(port);
                            port.AddConnection(_draggingPort);
                            break;
                        }
                        case PortType.Input when port is { PType: PortType.Output }:
                        {
                            // Check if the input port is already connected
                            var existingConnection = _connections.FirstOrDefault(c => c.To == _draggingPort);
                            if (existingConnection != null)
                            {
                                // Remove the existing connection
                                _connections.Remove(existingConnection);
                                existingConnection.From?.RemoveConnection(existingConnection.To);
                                existingConnection.To?.RemoveConnection(existingConnection.From);
                            }

                            // Establish the new connection
                            var newConnection = new Connection((OutputPort)port, (InputPort)_draggingPort);
                            _connections.Add(newConnection);

                            // Update the port connections
                            port.AddConnection(_draggingPort);
                            _draggingPort.AddConnection(port);
                            break;
                        }
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
    private static bool IsCompatible(NodePort? from, NodePort? to)
    {
        // Define DataType compatibility rules, e.g., String to String only
        if (to != null && from != null && from.DType != to.DType)
            return false;

        switch (from)
        {
            // Define compatibility rules, e.g., Output to Input only
            case { PType: PortType.Output } when to is { PType: PortType.Input }:
            case { PType: PortType.Input } when to is { PType: PortType.Output }:
                return true;
            default:
                return false;
        }
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
        InfiniteGrid.Render(canvasPos, _gridPosition, _zoomLevel, contentSize);
        
        // Render all nodes
        foreach (var node in Nodes.Values)
        {
            node.Render(canvasPos, _gridPosition, _zoomLevel);
        }

        // Render temporary connection line if dragging
        if (_isDragging && _draggingPort != null)
        {
            if (_draggingNodePort != null)
            {
                var index = _draggingPort switch
                {
                    InputPort port => _draggingNodePort.Inputs.IndexOf(port),
                    OutputPort port => _draggingNodePort.Outputs.IndexOf(port),
                    _ => -1
                };
                var nodeSize = _draggingPort.ParentNode.Size * _zoomLevel;
                var startPos = _draggingPort.GetScreenPosition(canvasPos,
                    _gridPosition,
                    _zoomLevel,
                    nodeSize,
                    index); // Placeholder indices

                var endPos = ImGui.GetIO().MousePos;

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
        }
        
        // Render existing connections
        foreach (var connection in _connections)
        {
            connection.Render(drawList, canvasPos, _gridPosition, _zoomLevel);
        }

        ImGui.EndChild();
        
        // Reset font
        ImGui.PopFont();
        
        // Implement context menu
        if (ImGui.BeginPopupContextItem($"Popup_Canvas"))
        {
            // Calculate mouse position relative to the canvas
            var mousePosInGrid = (io.MousePos - canvasPos - _gridPosition) / _zoomLevel;
            
            if (ImGui.MenuItem("Add Text Input Node"))
            {
                var node = new TextInputNode(mousePosInGrid, OnPortClicked);
                Nodes.Add(node.Id, node);
            }
            if (ImGui.MenuItem("Add Label Node"))
            {
                var node = new LabelNode(string.Empty, mousePosInGrid, OnPortClicked);
                Nodes.Add(node.Id, node);
            }
            if (ImGui.MenuItem("Add Load Image Node"))
            {
                var node = new LoadImageNode(mousePosInGrid, OnPortClicked);
                (node as IOpenGlNode).Gl = _gl;
                Nodes.Add(node.Id, node);
            }
            if (ImGui.MenuItem("Add Path Node"))
            {
                var node = new PathNode(mousePosInGrid, OnPortClicked);
                Nodes.Add(node.Id, node);
            }

            if (ImGui.MenuItem("Add Video Input Node"))
            {
                var node = new VideoInputNode(mousePosInGrid, OnPortClicked);
                (node as IOpenGlNode).Gl = _gl;
                Nodes.Add(node.Id, node);
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
            sw.WriteLine($"Node.ID:{node.Id}");
            sw.WriteLine($"Node.Title:{node.Title}");
            sw.WriteLine($"Node.Position:{node.Position.X},{node.Position.Y}");
            sw.WriteLine($"Node.Size:{node.Size.X},{node.Size.Y}");
            var nodeData = node.GetData();
            if (nodeData != null)
                sw.WriteLine($"Node.Data:{nodeData}");
            sw.WriteLine($"Node.Inputs:{node.Inputs.Count}");
            foreach (var input in node.Inputs)
            {
                sw.WriteLine($"Input:{input?.Name},{input?.DType},{input?.Id}");
            }
            sw.WriteLine($"Node.Outputs:{node.Outputs.Count}");
            foreach (var output in node.Outputs)
            {
                sw.WriteLine($"Output:{output?.Name},{output?.DType},{output?.Id}");
            }
        }
        
        // Serialize connections
        sw.WriteLine($"Connections:{_connections.Count}");
        foreach (var connection in _connections)
        {
            if (connection is { From: not null, To: not null })
            {
                sw.WriteLine(
                    $"Connection:{connection.From.Id},{connection.To.Id}," +
                    $"{connection.From.ParentNode.Id},{connection.To.ParentNode.Id}");
            }
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
        
        _connections.Clear();

        // Read and process each line
        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            string?[] lineParts = line.Split(':');
            var lineType = lineParts[0];

            if (lineParts[1] == null)
                continue;
            
            if (lineParts.Length > 2)
            {
                for (var i = 2; i < lineParts.Length; i++)
                {
                    lineParts[1] += $":{lineParts[i]}";
                }
            }
            
            switch (lineType)
            {
                case "Nodes":
                {
                    var _ = int.Parse(lineParts[1] ?? string.Empty);
                    break;
                }
                case "Node":
                {
                    var typeName = lineParts[1];
                    if (typeName != null)
                    {
                        var elementType = Type.GetType(typeName);

                        if (elementType == null)
                            continue;
                    
                        bufferNode = typeName switch
                        {
                            "ProcessPipeline.Nodes.TextInputNode" => new TextInputNode(),
                            "ProcessPipeline.Nodes.LabelNode" => new LabelNode(),
                            "ProcessPipeline.Nodes.LoadImageNode" => new LoadImageNode(),
                            _ => (Node)Activator.CreateInstance(elementType)!
                        };
                    }

                    if (bufferNode != null)
                    {
                        bufferNode.PortClickedHandler = OnPortClicked;

                        if (bufferNode is IOpenGlNode openGlNode)
                            openGlNode.Gl = _gl;
                    }

                    break;
                }
                case "Node.ID" when bufferNode != null:
                    bufferNode.Id = uint.Parse(lineParts[1] ?? string.Empty);
                    Nodes.Add(bufferNode.Id, bufferNode);
                    break;
                case "Node.Title" when bufferNode != null:
                    bufferNode.Title = lineParts[1];
                    break;
                case "Node.Position" when bufferNode != null:
                {
                    var positionParts = lineParts[1]?.Split(',');
                    if (positionParts == null)
                        break;
                    bufferNode.Position = new Vector2(
                        float.Parse(positionParts[0]),
                        float.Parse(positionParts[1])
                    );
                    break;
                }
                case "Node.Size" when bufferNode != null:
                {
                    var sizeParts = lineParts[1]?.Split(',');
                    if (sizeParts == null)
                        break;
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
                    var inputCount = int.Parse(lineParts[1] ?? string.Empty);
                    for (var i = 0; i < inputCount; i++)
                    {
                        var inputLine = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(inputLine))
                            continue;
                        var inputParts = inputLine.Split(',');
                        bufferNode.Inputs[i]!.Id = uint.Parse(inputParts[2]);
                        NodePort.UpdatePortIdCounter(bufferNode.Inputs[i]!.Id);
                    }

                    break;
                }
                case "Node.Outputs" when bufferNode != null:
                {
                    var outputCount = int.Parse(lineParts[1] ?? string.Empty);
                    for (var i = 0; i < outputCount; i++)
                    {
                        var outputLine = sr.ReadLine();
                        if (string.IsNullOrWhiteSpace(outputLine))
                            continue;
                        var outputParts = outputLine.Split(',');
                        bufferNode.Outputs[i]!.Id = uint.Parse(outputParts[2]);
                        NodePort.UpdatePortIdCounter(bufferNode.Outputs[i]!.Id);
                    }

                    break;
                }
                case "Connections":
                {
                    var _ = int.Parse(lineParts[1] ?? string.Empty);
                    break;
                }
                case "Connection":
                {
                    var connectionParts = lineParts[1]?.Split(',');
                    if (connectionParts != null)
                    {
                        var fromId = uint.Parse(connectionParts[0]);
                        var toId = uint.Parse(connectionParts[1]);
                        var fromNode = uint.Parse(connectionParts[2]);
                        var toNode = uint.Parse(connectionParts[3]);

                        OutputPort? fromPort = null;
                        if (Nodes.TryGetValue(fromNode, out var onNode))
                        {
                            fromPort = onNode.Outputs.FirstOrDefault(p => p != null && p.Id == fromId);
                        }
                        InputPort? toPort = null;
                        if (Nodes.TryGetValue(toNode, out var inNode))
                        {
                            toPort = inNode.Inputs.FirstOrDefault(p => p != null && p.Id == toId);
                        }

                        if (fromPort != null && toPort != null)
                        {
                            // Establish the new connection
                            var newConnection = new Connection(fromPort, toPort);
                            _connections.Add(newConnection);

                            // Update the port connections
                            fromPort.AddConnection(toPort);
                            toPort.AddConnection(fromPort);
                        }
                    }

                    break;
                }
            }
        }
    }
}