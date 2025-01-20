using ImGuiNET;
using System.Numerics;
using NativeFileDialogSharp;

namespace ProcessPipeline.Nodes
{
    public delegate void PortClickedHandler(NodePort? port, Node node);
    
    public abstract partial class Node
    {
        private static uint _idCounter = 1; // Static counter for unique node IDs
        public uint Id { get; set; }
        public Vector2 Position { get; set; } // Position of the node
        public Vector2 Size { get; set; } // Size of the node
        public abstract string? Title { get; set; } // Title of the node
        public abstract Flags flags { get; }
        public List<InputPort?> Inputs { get; set; }
        public List<OutputPort?> Outputs { get; set; }
        public abstract Vector2 DefaultSize { get; }
        public virtual Vector2 MinSize => new Vector2(100, 100);
        public virtual Vector2 MaxSize => new Vector2(500, 500);
        public PortClickedHandler? PortClickedHandler;

        protected Node(Vector2 pos, PortClickedHandler? portClickedHandler)
        {
            Position = pos;
            Inputs = [];
            Outputs = [];
            Id = GenerateNodeId();

            PortClickedHandler = portClickedHandler;
        }

        /// <summary>
        /// Adds an input port to the node.
        /// </summary>
        protected void AddInput(string name, DataType dataType, Action<object> setData)
        {
            Inputs.Add(new InputPort(name, dataType, this, setData));
        }

        /// <summary>
        /// Adds an output port to the node.
        /// </summary>
        protected void AddOutput(string name, DataType dataType, Func<object?> getData)
        {
            Outputs.Add(new OutputPort(name, dataType, this, getData));
        }

        /// <summary>
        /// Renders the node, including its ports and content.
        /// </summary>
        public virtual void Render(Vector2 canvasPos, Vector2 gridPosition, float zoomLevel)
        {
            var drawList = ImGui.GetWindowDrawList();
            
            var nodeSize = Size * zoomLevel; // Adjust size based on zoom level
            var nodeDarkColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f); // Main content area color
            var nodeBrightColor = new Vector4(0.3f, 0.3f, 0.3f, 1.0f); // Brighter color for selected nodes
            
            // Calculate the node's position relative to the grid and canvas
            Vector2 adjustedPos = canvasPos + gridPosition + Position * zoomLevel;
            
            // Define the title bar height
            float titleBarHeight = 30.0f * zoomLevel; // Adjust height based on zoom level
            
            // Define the rectangles for title bar and content
            Vector2 titleBarRectMin = adjustedPos;
            Vector2 titleBarRectMax = adjustedPos + new Vector2(nodeSize.X, titleBarHeight);
            Vector2 contentRectMin = titleBarRectMin + new Vector2(0, titleBarHeight);
            Vector2 contentRectMax = adjustedPos + nodeSize;
            
            // Define unique ID for title bar
            string titleBarId = $"TitleBar_Node_{Id}";
            
            // Create an invisible button over the title bar area to capture interactions
            ImGui.SetCursorScreenPos(titleBarRectMin);
            ImGui.InvisibleButton(titleBarId, new Vector2(nodeSize.X, titleBarHeight));
            
            // Handle dragging via title bar
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                var dragDelta = ImGui.GetIO().MouseDelta;
                Position += dragDelta / zoomLevel; // Adjust movement based on zoom
            }
            
            drawList.AddRectFilled(
                titleBarRectMin,
                titleBarRectMax,
                ImGui.ColorConvertFloat4ToU32(nodeBrightColor),
                5.0f, ImDrawFlags.RoundCornersTop); // Rounded top corners
            
            // Draw node title text in the title bar with scalable font
            var titleText = $"{Title}_{Id}";
            Vector2 titleTextSize = ImGui.CalcTextSize(titleText);
            Vector2 titleTextPos = titleBarRectMin + (new Vector2(nodeSize.X, titleBarHeight) - titleTextSize) / 2.0f;
            drawList.AddText(titleTextPos, ImGui.ColorConvertFloat4ToU32(Vector4.One), titleText);
            
            // Redraw the content area with modified color
            drawList.AddRectFilled(
                contentRectMin,
                contentRectMax,
                ImGui.ColorConvertFloat4ToU32(nodeDarkColor),
                5.0f, ImDrawFlags.RoundCornersBottom); // Rounded bottom corners
            
            DrawContextMenu();

            // Render input and output ports (existing code)
            RenderInputPorts(canvasPos, gridPosition, zoomLevel, nodeSize, drawList);
            RenderOutputPorts(canvasPos, gridPosition, zoomLevel, nodeSize, drawList);
            
            // Render resize handles
            RenderResizeHandles(adjustedPos, nodeSize, zoomLevel, titleBarHeight);
            
            // Call the RenderContent method for custom content
            RenderContent(drawList, contentRectMin, contentRectMax, zoomLevel);
        }

        private void DrawContextMenu()
        {
            // Implement context menu (existing code)
            if (ImGui.BeginPopupContextItem($"Popup_Node_{Id}"))
            {
                if (ImGui.MenuItem("Delete Node"))
                {
                    // Implement node deletion logic, possibly via a callback or event
                }
                if (ImGui.MenuItem("Rename Node"))
                {
                    // Implement node renaming logic, possibly via a callback or event
                }
                ImGui.EndPopup();
            }

            // Open the context menu on right-click
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            {
                ImGui.OpenPopup($"Popup_Node_{Id}");
            }
        }

        private void RenderResizeHandles(Vector2 nodePos, Vector2 nodeSize, float zoomLevel, float titleBarHeight)
        {
            // Adjust handle size based on zoom level
            var handleThickness = HANDLE_THICKNESS * zoomLevel;

            var drawList = ImGui.GetWindowDrawList();

            foreach (var handle in ResizeHandles)
            {
                switch (handle.IdSuffix)
                {
                    // Skip handles based on flags
                    case ResizeHandle.Type.Left or ResizeHandle.Type.Right or
                        ResizeHandle.Type.BottomLeft or ResizeHandle.Type.BottomRight when
                        !flags.HasFlag(Flags.ResizableX):
                    case ResizeHandle.Type.Bottom or ResizeHandle.Type.BottomLeft or
                        ResizeHandle.Type.BottomRight when
                        !flags.HasFlag(Flags.ResizableY):
                        continue;
                }

                Vector2 handlePos;
                Vector2 handleSize;

                switch (handle.IdSuffix)
                {
                    case ResizeHandle.Type.Left:
                        handlePos = nodePos + new Vector2(0, titleBarHeight);
                        handleSize = new Vector2(handleThickness, nodeSize.Y - titleBarHeight - handleThickness);
                        break;
                    case ResizeHandle.Type.Right:
                        handlePos = nodePos + new Vector2(nodeSize.X - handleThickness, titleBarHeight);
                        handleSize = new Vector2(handleThickness, nodeSize.Y - titleBarHeight - handleThickness);
                        break;
                    case ResizeHandle.Type.Bottom:
                        handlePos = nodePos + new Vector2(handleThickness, nodeSize.Y - handleThickness);
                        handleSize = new Vector2(nodeSize.X - 2 * handleThickness, handleThickness);
                        break;
                    case ResizeHandle.Type.BottomLeft:
                        handlePos = nodePos + new Vector2(0, nodeSize.Y - handleThickness);
                        handleSize = new Vector2(handleThickness, handleThickness);
                        break;
                    case ResizeHandle.Type.BottomRight:
                        handlePos = nodePos + new Vector2(nodeSize.X - handleThickness, nodeSize.Y - handleThickness);
                        handleSize = new Vector2(handleThickness, handleThickness);
                        break;
                    default:
                        continue;
                }

                // Define unique ID for the handle
                var handleId = $"ResizeHandle_{Id}_{handle.IdSuffix}";

                // Create an invisible button over the handle area to capture interactions
                ImGui.SetCursorScreenPos(handlePos);
                ImGui.InvisibleButton(handleId, handleSize);

                if (ImGui.IsItemClicked())
                {
                    _resizeMouseStartPos = ImGui.GetMousePos();
                    _resizeFrameStartPos = Position;
                    _resizeFrameStartSize = Size;
                }
                
                // Optional: Visual Indicator (e.g., semi-transparent overlay)
                if (ImGui.IsItemHovered())
                {
                    // Draw a semi-transparent rectangle to indicate the handle
                    drawList.AddRectFilled(handlePos, handlePos + handleSize, ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 0.1f)));

                    // Optionally, change the cursor icon
                    ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeAll); // Adjust based on handle type
                }

                // Handle dragging
                if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var sizeDiff = (ImGui.GetMousePos() - _resizeMouseStartPos) / zoomLevel;

                    var newSize = Size;
                    var newPosition = Position;

                    // Adjust size and position based on handle direction
                    if (handle.Direction.X != 0)
                    {
                        newSize.X = _resizeFrameStartSize.X + (sizeDiff.X * handle.Direction.X);

                        if (handle.Direction.X < 0)
                        {
                            newPosition.X = _resizeFrameStartPos.X + sizeDiff.X;
                        }
                    }

                    if (handle.Direction.Y != 0)
                    {
                        newSize.Y = _resizeFrameStartSize.Y + sizeDiff.Y;
                    }

                    // Enforce minimum and maximum size constraints
                    if (newSize.X < MinSize.X || newSize.Y < MinSize.Y)
                        continue;
                    
                    if (newSize.X > MaxSize.X || newSize.Y > MaxSize.Y)
                        continue;
                    
                    // Update the node's size and position
                    Size = newSize;
                    Position = newPosition;
                }
            }
        }
        
        private void RenderOutputPorts(Vector2 canvasPos, Vector2 gridPosition, float zoomLevel, Vector2 nodeSize,
            ImDrawListPtr drawList)
        {
            var outputIndex = 0;
            foreach (var output in Outputs)
            {
                if (output == null) continue;
                
                // Calculate position for the output port
                var portPos = output.GetScreenPosition(canvasPos, gridPosition, zoomLevel, nodeSize, outputIndex);
                // Render the output port as a small circle
                var portRadius = 5.0f * zoomLevel;
                var portColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // Green for inputs
                //drawList.AddCircle(portPos, portRadius, portColor);
                drawList.AddCircleFilled(portPos, portRadius - 2, portColor);
                var contentTextSize = ImGui.CalcTextSize(output.GetPortName());
                drawList.AddText(portPos + new Vector2(-(10 * zoomLevel) - contentTextSize.X, -8 * zoomLevel), portColor, output.GetPortName());

                // Handle interaction for port (e.g., initiating a connection)
                ImGui.SetCursorScreenPos(portPos - new Vector2(portRadius, portRadius));
                var portButtonId = $"Port_{Id}_{output.PType}_{output.Id}";
                ImGui.InvisibleButton(portButtonId, new Vector2(portRadius * 2, portRadius * 2));

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    // Invoke the port clicked handler
                    PortClickedHandler?.Invoke(output, this);
                }

                outputIndex++;
            }
        }

        private void RenderInputPorts(Vector2 canvasPos, Vector2 gridPosition, float zoomLevel, Vector2 nodeSize,
            ImDrawListPtr drawList)
        {
            var inputIndex = 0;
            foreach (var input in Inputs)
            {
                if (input == null) continue;
                
                // Calculate position for the input port
                var portPos = input.GetScreenPosition(
                    canvasPos, gridPosition, zoomLevel, nodeSize, inputIndex);
                // Render the input port as a small circle
                var portRadius = 5.0f * zoomLevel;
                var portColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // Green for inputs
                //drawList.AddCircle(portPos, portRadius, portColor);
                drawList.AddCircleFilled(portPos, portRadius - 2, portColor);
                drawList.AddText(portPos + new Vector2(10 * zoomLevel, -8 * zoomLevel), portColor, input.GetPortName());

                // Handle interaction for port (e.g., initiating a connection)
                // Use ImGui's InvisibleButton to detect clicks
                ImGui.SetCursorScreenPos(portPos - new Vector2(portRadius, portRadius));
                var portButtonId = $"Port_{Id}_{input.PType}_{input.Id}";
                ImGui.InvisibleButton(portButtonId, new Vector2(portRadius * 2, portRadius * 2));

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    // Invoke the port clicked handler
                    PortClickedHandler?.Invoke(input, this);
                }

                inputIndex++;
            }
        }

        /// <summary>
        /// Virtual method to be overridden by derived classes for custom content.
        /// </summary>
        protected virtual void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
        {
            // Default implementation: Display placeholder text
            const string nodeContent = "Node Content"; // Replace with actual content
            var contentTextSize = ImGui.CalcTextSize(nodeContent);
            var contentTextPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - contentTextSize) / 2.0f;
            
            // Fallback to default font if the specified font is not found
            drawList.AddText(contentTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), nodeContent);
        }

        /// <summary>
        /// Generates a unique node ID.
        /// </summary>
        private static uint GenerateNodeId()
        {
            return _idCounter++;
        }

        public virtual void Update(float deltaTime)
        {

        }

        public virtual void GetInput()
        {
            foreach (var t in Inputs)
            {
                if (t?.ConnectedPorts == null) continue;
                
                foreach (var p in t.ConnectedPorts)
                {
                    t?.SetData((p as OutputPort)!.GetData()!);
                }
            }
        }
        
        public virtual void SetOutput()
        {
            // Set the output data
            foreach (var t in Outputs)
            {
                if (t?.ConnectedPorts == null) continue;
                
                foreach (var p in t.ConnectedPorts)
                {
                    (p as InputPort)!.SetData(t?.GetData()!);
                }
            }
        }

        public virtual void Process()
        {
            // Set the output data
            foreach (var t in Outputs)
            {
                if (t?.ConnectedPorts == null) continue;
                
                foreach (var p in t.ConnectedPorts)
                {
                    var inputPort = p as InputPort;
                    inputPort?.SetData(t?.GetData()!);
                    inputPort?.ParentNode.Process();
                }
            }
        }

        public virtual string? GetData()
        {
            return null;
        }

        public virtual void SetData(string? data)
        {
            
        }
        
        protected void DrawPathInput(Vector2 position, float width, float zoomLevel, ref string? path)
        {
            // Render the text input field
            ImGui.SetCursorScreenPos(position);
            
            // push item width
            var text = path ?? string.Empty;
        
            if (ImGui.Button("Browse"))
            {
                try
                {
                    // Open folder picker
                    var result = Dialog.FileOpen();
                    if (result.IsOk && !string.IsNullOrEmpty(result.Path))
                    {
                        path = result.Path;
                        text = result.Path;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening folder picker: {ex.Message}");
                }
            }

            var buttonWidth = ImGui.GetItemRectSize().X;
            var paddingWidth = ImGui.GetStyle().FramePadding.X;
        
            ImGui.SameLine();
            ImGui.PushItemWidth(width - (buttonWidth + paddingWidth));
            if (ImGui.InputText($"##TextInput_Node_{Id}", ref text, 1000))
            {
                path = text;
            }
        }
    }
}
