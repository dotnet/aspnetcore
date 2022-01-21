// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using Moq;

namespace Microsoft.AspNetCore.Components;

public class ElementReferenceJsonConverterTest
{
    private readonly ElementReferenceContext ElementReferenceContext;
    private readonly ElementReferenceJsonConverter Converter;

    public ElementReferenceJsonConverterTest()
    {
        ElementReferenceContext = Mock.Of<ElementReferenceContext>();
        Converter = new ElementReferenceJsonConverter(ElementReferenceContext);
    }

    [Fact]
    public void Serializing_Works()
    {
        // Arrange
        var elementReference = ElementReference.CreateWithUniqueId(ElementReferenceContext);
        var expected = $"{{\"__internalId\":\"{elementReference.Id}\"}}";
        var memoryStream = new MemoryStream();
        var writer = new Utf8JsonWriter(memoryStream);

        // Act
        Converter.Write(writer, elementReference, new JsonSerializerOptions());
        writer.Flush();

        // Assert
        var json = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.Equal(expected, json);
    }

    [Fact]
    public void Deserializing_Works()
    {
        // Arrange
        var id = ElementReference.CreateWithUniqueId(ElementReferenceContext).Id;
        var json = $"{{\"__internalId\":\"{id}\"}}";
        var bytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);
        reader.Read();

        // Act
        var elementReference = Converter.Read(ref reader, typeof(ElementReference), new JsonSerializerOptions());

        // Assert
        Assert.Equal(id, elementReference.Id);
    }

    [Fact]
    public void Deserializing_WithFormatting_Works()
    {
        // Arrange
        var id = ElementReference.CreateWithUniqueId(ElementReferenceContext).Id;
        var json =
@$"{{
    ""__internalId"": ""{id}""
}}";
        var bytes = Encoding.UTF8.GetBytes(json);
        var reader = new Utf8JsonReader(bytes);
        reader.Read();

        // Act
        var elementReference = Converter.Read(ref reader, typeof(ElementReference), new JsonSerializerOptions());

        // Assert
        Assert.Equal(id, elementReference.Id);
    }

    [Fact]
    public void Deserializing_Throws_IfUnknownPropertyAppears()
    {
        // Arrange
        var json = "{\"id\":\"some-value\"}";
        var bytes = Encoding.UTF8.GetBytes(json);

        // Act
        var ex = Assert.Throws<JsonException>(() =>
        {
            var reader = new Utf8JsonReader(bytes);
            reader.Read();
            Converter.Read(ref reader, typeof(ElementReference), new JsonSerializerOptions());
        });

        // Assert
        Assert.Equal("Unexpected JSON property 'id'.", ex.Message);
    }

    [Fact]
    public void Deserializing_Throws_IfIdIsNotSpecified()
    {
        // Arrange
        var json = "{}";
        var bytes = Encoding.UTF8.GetBytes(json);

        // Act
        var ex = Assert.Throws<JsonException>(() =>
        {
            var reader = new Utf8JsonReader(bytes);
            reader.Read();
            Converter.Read(ref reader, typeof(ElementReference), new JsonSerializerOptions());
        });

        // Assert
        Assert.Equal("__internalId is required.", ex.Message);
    }
}
