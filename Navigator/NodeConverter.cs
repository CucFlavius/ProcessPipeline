using System.Text.Json;
using System.Text.Json.Serialization;
using ProcessPipeline.Nodes;

namespace ProcessPipeline.Serialization;

public class NodeConverter : JsonConverter<Node>
{
    public override Node? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions? options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        
        if (!jsonDoc.RootElement.TryGetProperty("type", out var typeProperty))
        {
            throw new JsonException("Missing type discriminator.");
        }

        var typeDiscriminator = typeProperty.GetString();
        Node? node = typeDiscriminator switch
        {
            "TextInputNode" => JsonSerializer.Deserialize<TextInputNode>(jsonDoc.RootElement.GetRawText(), options),
            "LabelNode" => JsonSerializer.Deserialize<LabelNode>(jsonDoc.RootElement.GetRawText(), options),
            _ => throw new NotSupportedException($"Node type '{typeDiscriminator}' is not supported.")
        };
        return node;
    }

    public override void Write(Utf8JsonWriter writer, Node value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Write type discriminator
        writer.WriteString("type", value.GetType().Name);

        // Write common properties
        writer.WriteNumber("id", value.ID);
        writer.WritePropertyName("position");
        JsonSerializer.Serialize(writer, value.NodePos, options);
        writer.WritePropertyName("size");
        JsonSerializer.Serialize(writer, value.NodeSize, options);
        writer.WritePropertyName("title");
        JsonSerializer.Serialize(writer, value.Title, options);

        // Write inputs
        writer.WritePropertyName("inputs");
        JsonSerializer.Serialize(writer, value.Inputs, options);

        // Write outputs
        writer.WritePropertyName("outputs");
        JsonSerializer.Serialize(writer, value.Outputs, options);

        // Write type-specific properties
        switch (value)
        {
            case TextInputNode textInputNode:
                writer.WriteString("text", textInputNode.Text);
                break;
            case LabelNode labelNode:
                writer.WriteString("label", labelNode.Text);
                break;
            // Handle other node types...
        }

        writer.WriteEndObject();
    }

    public static void UpdateIDCounter(uint maxNodeId)
    {
        Node.IDCounter = maxNodeId;
    }
}