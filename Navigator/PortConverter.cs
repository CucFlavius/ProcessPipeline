// Serialization/PortConverter.cs
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessPipeline.Nodes;

namespace ProcessPipeline.Serialization
{
    public class PortConverter : JsonConverter<NodePort>
    {
        public override NodePort Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Load the JSON document
            using (var jsonDoc = JsonDocument.ParseValue(ref reader))
            {
                var root = jsonDoc.RootElement;

                // Read the discriminator
                if (!root.TryGetProperty("portType", out var portTypeProperty))
                {
                    throw new JsonException("Missing portType discriminator.");
                }

                string portTypeDiscriminator = portTypeProperty.GetString();

                // Determine the type based on the discriminator
                NodePort port = portTypeDiscriminator switch
                {
                    "Input" => JsonSerializer.Deserialize<InputPort>(root.GetRawText(), options),
                    "Output" => JsonSerializer.Deserialize<OutputPort>(root.GetRawText(), options),
                    _ => throw new NotSupportedException($"Port type '{portTypeDiscriminator}' is not supported.")
                };

                return port;
            }
        }

        public override void Write(Utf8JsonWriter writer, NodePort value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write the discriminator
            writer.WriteString("portType", value.PType.ToString());

            // Write common properties
            writer.WriteNumber("id", value.ID);
            writer.WriteString("name", value.Name);
            writer.WriteString("dataType", value.DType.ToString());

            // Optionally, write derived class-specific properties
            switch (value)
            {
                case InputPort inputPort:
                    // No additional properties for InputPort in this example
                    break;
                case OutputPort outputPort:
                    // No additional properties for OutputPort in this example
                    break;
                default:
                    throw new NotSupportedException($"Port type '{value.PType}' is not supported.");
            }

            writer.WriteEndObject();
        }

        public static void UpdateIDCounter(uint maxPortId)
        {
            NodePort.UpdatePortIDCounter(maxPortId);
        }
    }
}
