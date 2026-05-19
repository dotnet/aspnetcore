// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace Microsoft.AspNetCore.JsonPatch.SystemTextJson.IntegrationTests;

public class HeterogenousCollectionTests
{
    [Fact]
    public void AddItemToList()
    {
        // Arrange
        var targetObject = new Canvas()
        {
            Items = new List<Shape>()
        };

        var circleJObject = JsonObject.Parse(@"{
                      ""Type"": ""Circle"",
                      ""ShapeProperty"": ""Shape property"",
                      ""CircleProperty"": ""Circle property""
                    }");

        var serializerOptions = new JsonSerializerOptions();
        serializerOptions.TypeInfoResolver = new CanvasContractResolver();

        var patchDocument = new JsonPatchDocument
        {
            SerializerOptions = serializerOptions
        };

        patchDocument.Add("/Items/-", circleJObject);

        // Act
        patchDocument.ApplyTo(targetObject);

        // Assert
        var circle = targetObject.Items[0] as Circle;
        Assert.NotNull(circle);
        Assert.Equal("Shape property", circle.ShapeProperty);
        Assert.Equal("Circle property", circle.CircleProperty);
    }
}

public class CanvasContractResolver : DefaultJsonTypeInfoResolver
{
    public override JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
    {
        // Get the default metadata for the type.
        var jsonTypeInfo = base.GetTypeInfo(type, options);

        // Check if the type is Shape or derives from it.
        if (jsonTypeInfo.Type == typeof(Shape))
        {
            // Configure polymorphism options if they haven't been set yet.
            if (jsonTypeInfo.PolymorphismOptions == null)
            {
                jsonTypeInfo.PolymorphismOptions = new JsonPolymorphismOptions
                {
                    TypeDiscriminatorPropertyName = "Type",
                    IgnoreUnrecognizedTypeDiscriminators = true,
                    UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FailSerialization,
                    DerivedTypes = {
                        new JsonDerivedType(typeof(Circle), "Circle"),
                        new JsonDerivedType(typeof(Rectangle), "Rectangle")
                    }
                };
            }
        }

        return jsonTypeInfo;
    }
}

public class ShapeJsonConverter : JsonConverter<Shape>
{
    public override Shape Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException();
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        if (root.TryGetProperty("CircleProperty", out _))
        {
            return JsonSerializer.Deserialize<Circle>(root.GetRawText(), options);
        }
        else if (root.TryGetProperty("RectangleProperty", out _))
        {
            return JsonSerializer.Deserialize<Rectangle>(root.GetRawText(), options);
        }
        else
        {
            throw new JsonException("Unknown shape type");
        }
    }

    public override void Write(Utf8JsonWriter writer, Shape value, JsonSerializerOptions options)
    {
        if (value is Circle circle)
        {
            JsonSerializer.Serialize(writer, circle, options);
        }
        else if (value is Rectangle rectangle)
        {
            JsonSerializer.Serialize(writer, rectangle, options);
        }
        else
        {
            throw new JsonException("Unknown shape type");
        }
    }
}
