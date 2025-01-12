using ImGuiNET;
using System.Numerics;
using System.Collections.Generic;

namespace ProcessPipeline.Nodes
{
    public delegate void PortClickedHandler(NodePort port, Node node);
    
    public abstract class Node
    {
        public static uint IDCounter = 1; // Static counter for unique node IDs
        public uint ID { get; private set; }
        public Vector2 _nodePos;
        protected string _title; // Title of the node
        protected bool _isSelected = false; // Tracks if the node is selected

        public List<InputPort> Inputs { get; private set; }
        public List<OutputPort> Outputs { get; private set; }

        // Callback for port clicks
        private PortClickedHandler _portClickedHandler;

        public Node(Vector2 pos, PortClickedHandler portClickedHandler)
        {
            _nodePos = pos;
            _title = "Node";
            Inputs = new List<InputPort>();
            Outputs = new List<OutputPort>();
            ID = GenerateNodeID();

            _portClickedHandler = portClickedHandler;
        }

        /// <summary>
        /// Adds an input port to the node.
        /// </summary>
        public void AddInput(string name)
        {
            Inputs.Add(new InputPort(name, this));
        }

        /// <summary>
        /// Adds an output port to the node.
        /// </summary>
        public void AddOutput(string name)
        {
            Outputs.Add(new OutputPort(name, this));
        }

        /// <summary>
        /// Renders the node, including its ports and content.
        /// </summary>
        public virtual void Render(Vector2 canvasPos, Vector2 gridPosition, float zoomLevel)
        {
            var drawList = ImGui.GetWindowDrawList();
            var nodeSize = new Vector2(200, 300) * zoomLevel; // Adjust size based on zoom level
            var nodeColor = new Vector4(0.2f, 0.2f, 0.2f, 1.0f); // Main content area color

            // Calculate the node's position relative to the grid and canvas
            Vector2 adjustedPos = canvasPos + gridPosition + _nodePos * zoomLevel;

            // Define the title bar height
            float titleBarHeight = 30.0f * zoomLevel; // Adjust height based on zoom level

            // Define the rectangles for title bar and content
            Vector2 titleBarRectMin = adjustedPos;
            Vector2 titleBarRectMax = adjustedPos + new Vector2(nodeSize.X, titleBarHeight);
            Vector2 contentRectMin = titleBarRectMin + new Vector2(0, titleBarHeight);
            Vector2 contentRectMax = adjustedPos + nodeSize;

            // Define unique ID for title bar
            string titleBarId = $"TitleBar_Node_{ID}";

            // Create an invisible button over the title bar area to capture interactions
            ImGui.SetCursorScreenPos(titleBarRectMin);
            ImGui.InvisibleButton(titleBarId, new Vector2(nodeSize.X, titleBarHeight));

            // Handle dragging via title bar
            if (ImGui.IsItemActive() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                var dragDelta = ImGui.GetIO().MouseDelta;
                _nodePos += dragDelta / zoomLevel; // Adjust movement based on zoom
            }

            // Change title bar colors based on hover and active states
            Vector4 titleBarColorStart = new Vector4(0.3f, 0.3f, 0.3f, 1.0f); // Start color of gradient
            Vector4 titleBarColorEnd = new Vector4(0.1f, 0.1f, 0.1f, 1.0f);   // End color of gradient

            if (ImGui.IsItemHovered())
            {
                titleBarColorStart = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
                titleBarColorEnd = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
            }

            if (ImGui.IsItemActive())
            {
                titleBarColorStart = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
                titleBarColorEnd = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);
            }

            // Draw the title bar with gradient
            drawList.AddRectFilledMultiColor(titleBarRectMin, titleBarRectMax,
                ImGui.ColorConvertFloat4ToU32(titleBarColorStart),
                ImGui.ColorConvertFloat4ToU32(titleBarColorEnd),
                ImGui.ColorConvertFloat4ToU32(titleBarColorEnd),
                ImGui.ColorConvertFloat4ToU32(titleBarColorStart));

            // Draw node title text in the title bar with scalable font
            Vector2 titleTextSize = ImGui.CalcTextSize(_title);
            Vector2 titleTextPos = titleBarRectMin + (new Vector2(nodeSize.X, titleBarHeight) - titleTextSize) / 2.0f;
            // Fallback to default font if the specified font is not found
            drawList.AddText(titleTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), _title);

            // Handle selection via content area
            //string contentId = $"Content_Node_{ID}";
            //ImGui.SetCursorScreenPos(contentRectMin);
            //ImGui.InvisibleButton(contentId, new Vector2(nodeSize.X, nodeSize.Y - titleBarHeight));

            // Handle selection toggle
            /*
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            {
                _isSelected = !_isSelected; // Toggle selection state
            }
            */
            // Change content area color if selected
            Vector4 contentColorModified = _isSelected ? new Vector4(0.3f, 0.3f, 0.3f, 1.0f) : nodeColor;

            // Redraw the content area with modified color
            drawList.AddRectFilled(contentRectMin, contentRectMax, ImGui.ColorConvertFloat4ToU32(contentColorModified));

            // Implement context menu
            if (ImGui.BeginPopupContextItem($"Popup_Node_{ID}"))
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
                ImGui.OpenPopup($"Popup_Node_{ID}");
            }

            // Render input ports
            int inputIndex = 0;
            foreach (var input in Inputs)
            {
                // Calculate position for the input port
                Vector2 portPos = input.GetScreenPosition(canvasPos, gridPosition, zoomLevel, nodeSize, inputIndex);
                // Render the input port as a small circle
                float portRadius = 5.0f * zoomLevel;
                uint portColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // Green for inputs
                //drawList.AddCircle(portPos, portRadius, portColor);
                drawList.AddCircleFilled(portPos, portRadius - 2, portColor);
                drawList.AddText(portPos + new Vector2(10 * zoomLevel, -8 * zoomLevel), portColor, input.GetPortName());

                // Handle interaction for port (e.g., initiating a connection)
                // Use ImGui's InvisibleButton to detect clicks
                ImGui.SetCursorScreenPos(portPos - new Vector2(portRadius, portRadius));
                string portButtonID = $"Port_{ID}_{input.Type}_{input.ID}";
                ImGui.InvisibleButton(portButtonID, new Vector2(portRadius * 2, portRadius * 2));

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    // Invoke the port clicked handler
                    _portClickedHandler?.Invoke(input, this);
                }

                inputIndex++;
            }

            // Render output ports
            int outputIndex = 0;
            foreach (var output in Outputs)
            {
                // Calculate position for the output port
                Vector2 portPos = output.GetScreenPosition(canvasPos, gridPosition, zoomLevel, nodeSize, outputIndex);
                // Render the output port as a small circle
                float portRadius = 5.0f * zoomLevel;
                uint portColor = ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // Green for inputs
                //drawList.AddCircle(portPos, portRadius, portColor);
                drawList.AddCircleFilled(portPos, portRadius - 2, portColor);
                Vector2 contentTextSize = ImGui.CalcTextSize(output.GetPortName());
                drawList.AddText(portPos + new Vector2(-(10 * zoomLevel) - contentTextSize.X, -8 * zoomLevel), portColor, output.GetPortName());

                // Handle interaction for port (e.g., initiating a connection)
                ImGui.SetCursorScreenPos(portPos - new Vector2(portRadius, portRadius));
                string portButtonID = $"Port_{ID}_{output.Type}_{output.ID}";
                ImGui.InvisibleButton(portButtonID, new Vector2(portRadius * 2, portRadius * 2));

                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    // Invoke the port clicked handler
                    _portClickedHandler?.Invoke(output, this);
                }

                outputIndex++;
            }

            // Call the RenderContent method for custom content
            RenderContent(drawList, contentRectMin, contentRectMax, zoomLevel);

            // Optional: Highlight node border if selected
            if (_isSelected)
            {
                drawList.AddRect(adjustedPos, adjustedPos + nodeSize, ImGui.ColorConvertFloat4ToU32(new Vector4(0.0f, 1.0f, 0.0f, 1.0f)), 0.0f, 0, 2.0f); // Green border
            }
        }

        /// <summary>
        /// Virtual method to be overridden by derived classes for custom content.
        /// </summary>
        protected virtual void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
        {
            // Default implementation: Display placeholder text
            string nodeContent = "Node Content"; // Replace with actual content
            Vector2 contentTextSize = ImGui.CalcTextSize(nodeContent);
            Vector2 contentTextPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - contentTextSize) / 2.0f;
            
            // Fallback to default font if the specified font is not found
            drawList.AddText(contentTextPos, ImGui.ColorConvertFloat4ToU32(new Vector4(1.0f, 1.0f, 1.0f, 1.0f)), nodeContent);
        }

        /// <summary>
        /// Generates a unique node ID.
        /// </summary>
        public static uint GenerateNodeID()
        {
            return IDCounter++;
        }

        public virtual void Update(float deltaTime)
        {
            // Update logic for the node, if any
        }
    }
}
