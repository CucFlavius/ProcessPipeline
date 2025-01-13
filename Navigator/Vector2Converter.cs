using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Numerics;

namespace ProcessPipeline.Serialization
{
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            float x = 0, y = 0;
            if (reader.TokenType != JsonTokenType.StartArray)
                throw new JsonException();
    
            reader.Read();
            if (reader.TokenType == JsonTokenType.Number)
                x = reader.GetSingle();
            reader.Read();
            if (reader.TokenType == JsonTokenType.Number)
                y = reader.GetSingle();
            reader.Read();
    
            if (reader.TokenType != JsonTokenType.EndArray)
                throw new JsonException();
    
            return new Vector2(x, y);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteNumberValue(value.X);
            writer.WriteNumberValue(value.Y);
            writer.WriteEndArray();
        }
    }
}