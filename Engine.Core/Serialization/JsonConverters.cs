using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace Engine.Core.Serialization;

// Conversor para manter o Vector3 limpo no JSON: { "X": 0.0, "Y": 5.0, "Z": 0.0 }
public class Vector3Converter : JsonConverter<Vector3>
{
    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        var jo = new JObject
        {
            { "X", value.X },
            { "Y", value.Y },
            { "Z", value.Z }
        };
        jo.WriteTo(writer);
    }

    public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        return new Vector3(
            jo["X"]?.Value<float>() ?? 0f,
            jo["Y"]?.Value<float>() ?? 0f,
            jo["Z"]?.Value<float>() ?? 0f
        );
    }
}

// Conversor para manter a Cor limpa no JSON: { "R": 255, "G": 0, "B": 0, "A": 255 }
public class ColorConverter : JsonConverter<Color>
{
    public override void WriteJson(JsonWriter writer, Color value, JsonSerializer serializer)
    {
        var jo = new JObject
        {
            { "R", value.R },
            { "G", value.G },
            { "B", value.B },
            { "A", value.A }
        };
        jo.WriteTo(writer);
    }

    public override Color ReadJson(JsonReader reader, Type objectType, Color existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        return new Color(
            jo["R"]?.Value<int>() ?? 255,
            jo["G"]?.Value<int>() ?? 255,
            jo["B"]?.Value<int>() ?? 255,
            jo["A"]?.Value<int>() ?? 255
        );
    }
}

// Adicione isso junto aos outros conversores de Vector3 e Color
public class QuaternionConverter : JsonConverter<Quaternion>
{
    public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
    {
        var jo = new JObject { { "X", value.X }, { "Y", value.Y }, { "Z", value.Z }, { "W", value.W } };
        jo.WriteTo(writer);
    }

    public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var jo = JObject.Load(reader);
        return new Quaternion(
            jo["X"]?.Value<float>() ?? 0f,
            jo["Y"]?.Value<float>() ?? 0f,
            jo["Z"]?.Value<float>() ?? 0f,
            jo["W"]?.Value<float>() ?? 1f // Importante: W deve ser 1 por padrão para não gerar matriz vazia!
        );
    }
}