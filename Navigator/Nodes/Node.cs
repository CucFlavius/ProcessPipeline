using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes
{
    public delegate void PortClickedHandler(NodePort? port, Node node);
    
    public abstract class Node
    {
        private static uint _idCounter = 1; // Static counter for unique node IDs
        public uint Id { get; set; }
        public Vector2 Position { get; set; } // Position of the node
        public Vector2 Size { get; set; } // Size of the node
        public abstract string? Title { get; set; } // Title of the node
        public List<InputPort?> Inputs { get; set; }
        public List<OutputPort?> Outputs { get; set; }
        public abstract Vector2 DefaultSize { get; }
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

            // Draw background for the node
            drawList.AddRectFilled(
                adjustedPos,
                adjustedPos + nodeSize,
                ImGui.ColorConvertFloat4ToU32(nodeDarkColor),
                5.0f);
            
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
                5.0f, ImDrawFlags.RoundCornersTop); // Radius: 10.0f for rounded corners

            // Draw node title text in the title bar with scalable font
            var titleText = $"{Title}_{Id}";
            Vector2 titleTextSize = ImGui.CalcTextSize(titleText);
            Vector2 titleTextPos = titleBarRectMin + (new Vector2(nodeSize.X, titleBarHeight) - titleTextSize) / 2.0f;
            // Fallback to default font if the specified font is not found
            drawList.AddText(titleTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), titleText);
            
            // Redraw the content area with modified color
            drawList.AddRectFilled(
                contentRectMin,
                contentRectMax,
                ImGui.ColorConvertFloat4ToU32(nodeDarkColor),
                5.0f, ImDrawFlags.RoundCornersBottom); // Radius: 10.0f for rounded corners

            // Implement context menu
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

            // Render input ports
            RenderInputPorts(canvasPos, gridPosition, zoomLevel, nodeSize, drawList);

            // Render output ports
            RenderOutputPorts(canvasPos, gridPosition, zoomLevel, nodeSize, drawList);

            // Call the RenderContent method for custom content
            RenderContent(drawList, contentRectMin, contentRectMax, zoomLevel);
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
    }
}
