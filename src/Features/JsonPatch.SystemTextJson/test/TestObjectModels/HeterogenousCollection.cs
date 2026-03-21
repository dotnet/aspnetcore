// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson;

public abstract class Shape
{
    public string ShapeProperty { get; set; }
}

public class Circle : Shape
{
    public string CircleProperty { get; set; }
}

public class Rectangle : Shape
{
    public string RectangleProperty { get; set; }
}

public class Square : Shape
{
    public Rectangle Rectangle { get; set; }
}

public class Canvas
{
    public IList<Shape> Items { get; set; }
}

public class RectangleContractResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        if (type == typeof(Rectangle))
        {
            JsonTypeInfo<Rectangle> jsonTypeInfo = (JsonTypeInfo<Rectangle>)base.GetTypeInfo(type, options);
            jsonTypeInfo.CreateObject = () => new Rectangle();

            var stringComparison = options.PropertyNameCaseInsensitive ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            foreach (var property in jsonTypeInfo.Properties)
            {
                if (nameof(Rectangle.ShapeProperty).Equals(property.Name, stringComparison))
                {
                    property.Get = (obj) => ((Rectangle)obj).ShapeProperty;
                    property.Set = (obj, value) => ((Rectangle)obj).ShapeProperty = (string)value;
                }
                else if (nameof(Rectangle.RectangleProperty).Equals(property.Name, stringComparison))
                {
                    property.Get = (obj) => ((Rectangle)obj).RectangleProperty;
                    property.Set = (obj, value) => ((Rectangle)obj).RectangleProperty = (string)value;
                }
            }

            return jsonTypeInfo;
        }

        return base.GetTypeInfo(type, options);
    }
}

public class RectangleJsonConverter : JsonConverter<Rectangle>
{
    public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new Rectangle { RectangleProperty = reader.GetString() };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        var rectangle = new Rectangle();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return rectangle;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException();
            }

            string propertyName = reader.GetString();

            reader.Read();

            switch (propertyName)
            {
                case nameof(Rectangle.ShapeProperty):
                    rectangle.ShapeProperty = reader.GetString();
                    break;
                case nameof(Rectangle.RectangleProperty):
                    rectangle.RectangleProperty = reader.GetString();
                    break;
                default:
                    throw new JsonException();
            }
        }

        return rectangle;
    }

    public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString(nameof(Rectangle.ShapeProperty), value.ShapeProperty);
        writer.WriteString(nameof(Rectangle.RectangleProperty), value.RectangleProperty);

        writer.WriteEndObject();
    }
}
