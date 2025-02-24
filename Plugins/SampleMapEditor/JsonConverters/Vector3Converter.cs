using Newtonsoft.Json;
using OpenTK;
using System;

namespace RedStarLibrary.JsonConverters
{
    internal class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var data = serializer.Deserialize<float[]>(reader);
            return new Vector3(data[0], data[1], data[2]);
        }

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
        {
            writer.WriteValue(value.X);
            writer.WriteValue(value.Y);
            writer.WriteValue(value.Z);
        }
    }
}
