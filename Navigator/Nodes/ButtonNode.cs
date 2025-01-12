using ImGuiNET;
using System.Numerics;

namespace ProcessPipeline.Nodes
{
    public class ButtonNode : Node
    {
        private string _buttonLabel;

        public ButtonNode(string label, Vector2 pos, PortClickedHandler portClickedHandler) : base(pos, portClickedHandler)
        {
            _title = "Button Node";
            _buttonLabel = label;

            // Add one input and one output port
            AddInput("Input1");
            AddOutput("Output1");
        }

        protected override void RenderContent(ImDrawListPtr drawList, Vector2 contentMin, Vector2 contentMax, float zoomLevel)
        {
            // Calculate position for the button
            Vector2 buttonSize = new Vector2(100, 30) * zoomLevel;
            Vector2 buttonPos = contentMin + (new Vector2(contentMax.X - contentMin.X, contentMax.Y - contentMin.Y) - buttonSize) / 2.0f;

            // Define unique ID for the button
            string buttonID = $"Button_Node_{ID}";

            // Render the button using ImGui
            ImGui.SetCursorScreenPos(buttonPos);
            if (ImGui.Button(buttonID, buttonSize))
            {
                // Handle button click
                // For example, toggle a state or trigger an event
                // This is a placeholder; implement as needed
                Console.WriteLine($"{_buttonLabel} button clicked.");
            }
        }
    }
}