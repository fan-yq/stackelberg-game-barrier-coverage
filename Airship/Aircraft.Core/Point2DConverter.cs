using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MathNet.Spatial.Euclidean;

namespace Aircraft.Core;


public class Point2DConverter : JsonConverter<Point2D> {
    public override Point2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        // 确保当前 token 是数组的开始 '['
        if (reader.TokenType != JsonTokenType.StartArray) {
            throw new JsonException("Expected StartArray token for Point2D");
        }

        // 读取 X 坐标
        reader.Read();
        double x = reader.GetDouble();

        // 读取 Y 坐标
        reader.Read();
        double y = reader.GetDouble();

        // 确保当前 token 是数组的结束 ']'
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray) {
            throw new JsonException("Expected EndArray token for Point2D");
        }

        // 根据 MathNet.Point2D 的实际构造函数进行实例化
        return new Point2D(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Point2D value, JsonSerializerOptions options) {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

public class Vector2DConverter : JsonConverter<Vector2D> {
    public override Vector2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        // 确保当前 token 是数组的开始 '['
        if (reader.TokenType != JsonTokenType.StartArray) {
            throw new JsonException("Expected StartArray token for Point2D");
        }

        // 读取 X 坐标
        reader.Read();
        double x = reader.GetDouble();

        // 读取 Y 坐标
        reader.Read();
        double y = reader.GetDouble();

        // 确保当前 token 是数组的结束 ']'
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray) {
            throw new JsonException("Expected EndArray token for Point2D");
        }

        // 根据 MathNet.Point2D 的实际构造函数进行实例化
        return new Vector2D(x, y);
    }

    public override void Write(Utf8JsonWriter writer, Vector2D value, JsonSerializerOptions options) {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

public class Vector3DConverter : JsonConverter<Vector3D> {
    public override Vector3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        // 确保当前 token 是数组的开始 '['
        if (reader.TokenType != JsonTokenType.StartArray) {
            throw new JsonException("Expected StartArray token for Point2D");
        }

        // 读取 X 坐标
        reader.Read();
        double x = reader.GetDouble();

        // 读取 Y 坐标
        reader.Read();
        double y = reader.GetDouble();

        reader.Read();
        double z = reader.GetDouble();

        // 确保当前 token 是数组的结束 ']'
        reader.Read();
        if (reader.TokenType != JsonTokenType.EndArray) {
            throw new JsonException("Expected EndArray token for Point2D");
        }

        // 根据 MathNet.Point2D 的实际构造函数进行实例化
        return new Vector3D(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3D value, JsonSerializerOptions options) {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}