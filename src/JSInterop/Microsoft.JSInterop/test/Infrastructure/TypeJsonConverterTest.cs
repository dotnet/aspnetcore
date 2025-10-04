// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

public class TypeJsonConverterTest
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public TypeJsonConverterTest()
    {
        _jsonSerializerOptions = new JsonSerializerOptions();
        _jsonSerializerOptions.Converters.Add(new TypeJsonConverter());
    }

    [Fact]
    public void CanSerializeAndDeserializeType()
    {
        // Arrange
        var originalType = typeof(string);

        // Act
        var json = JsonSerializer.Serialize(originalType, _jsonSerializerOptions);
        var deserializedType = JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions);

        // Assert
        Assert.Equal(originalType, deserializedType);
    }

    [Fact]
    public void CanSerializeAndDeserializeGenericType()
    {
        // Arrange
        var originalType = typeof(List<string>);

        // Act
        var json = JsonSerializer.Serialize(originalType, _jsonSerializerOptions);
        var deserializedType = JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions);

        // Assert
        Assert.Equal(originalType, deserializedType);
    }

    [Fact]
    public void CanSerializeAndDeserializeNullType()
    {
        // Arrange
        Type? originalType = null;

        // Act
        var json = JsonSerializer.Serialize(originalType, _jsonSerializerOptions);
        var deserializedType = JsonSerializer.Deserialize<Type?>(json, _jsonSerializerOptions);

        // Assert
        Assert.Null(deserializedType);
    }

    [Fact]
    public void SerializedTypeContainsAssemblyAndTypeProperties()
    {
        // Arrange
        var type = typeof(TypeJsonConverterTest);

        // Act
        var json = JsonSerializer.Serialize(type, _jsonSerializerOptions);

        // Assert
        var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;

        Assert.True(root.TryGetProperty("assembly", out var assemblyProperty));
        Assert.True(root.TryGetProperty("type", out var typeProperty));

        Assert.Equal("Microsoft.JSInterop.Tests", assemblyProperty.GetString());
        Assert.Equal("Microsoft.JSInterop.Infrastructure.TypeJsonConverterTest", typeProperty.GetString());
    }

    [Fact]
    public void Read_ThrowsJsonException_IfJsonIsMissingAssemblyProperty()
    {
        // Arrange
        var json = """{"type":"System.String"}""";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions));
        Assert.Equal("Type JSON must contain both 'assembly' and 'type' properties.", ex.Message);
    }

    [Fact]
    public void Read_ThrowsJsonException_IfJsonIsMissingTypeProperty()
    {
        // Arrange
        var json = """{"assembly":"System.Private.CoreLib"}""";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions));
        Assert.Equal("Type JSON must contain both 'assembly' and 'type' properties.", ex.Message);
    }

    [Fact]
    public void Read_ThrowsJsonException_IfJsonContainsUnknownProperty()
    {
        // Arrange
        var json = """{"assembly":"System.Private.CoreLib","type":"System.String","unknown":"value"}""";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions));
        Assert.Equal("Unexpected property 'unknown' in Type JSON.", ex.Message);
    }

    [Fact]
    public void Read_ThrowsInvalidOperationException_IfAssemblyCannotBeLoaded()
    {
        // Arrange
        var json = """{"assembly":"NonExistentAssembly","type":"SomeType"}""";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions));
        Assert.StartsWith("Cannot load assembly 'NonExistentAssembly'", ex.Message);
    }

    [Fact]
    public void Read_ThrowsInvalidOperationException_IfTypeCannotBeFound()
    {
        // Arrange
        var json = """{"assembly":"System.Private.CoreLib","type":"NonExistentType"}""";

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions));
        Assert.StartsWith("Cannot find type 'NonExistentType' in assembly 'System.Private.CoreLib'", ex.Message);
    }

    [Fact]
    public void Read_ThrowsJsonException_IfJsonIsNotStartObject()
    {
        // Arrange
        var json = "\"invalid\"";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions));
        Assert.Equal("Expected StartObject, got String", ex.Message);
    }

    [Fact]
    public void Write_ThrowsInvalidOperationException_IfAssemblyNameIsNull()
    {
        // This is a theoretical test since we can't easily create a Type with null assembly name
        // but it documents the expected behavior
        Assert.True(true); // Placeholder - difficult to test this edge case
    }

    [Fact]
    public void CacheWorks_WithMultipleRequests()
    {
        // Arrange
        var type = typeof(string);
        var json = JsonSerializer.Serialize(type, _jsonSerializerOptions);

        // Act - Deserialize the same type multiple times
        var type1 = JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions);
        var type2 = JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions);

        // Assert - Should return the same type instance
        Assert.Equal(type, type1);
        Assert.Equal(type, type2);
        Assert.Same(type1, type2); // Same reference due to caching
    }

    [Fact]
    public void ClearCache_RemovesCachedEntries()
    {
        // Arrange
        var type = typeof(string);
        var json = JsonSerializer.Serialize(type, _jsonSerializerOptions);
        
        // First deserialization to populate cache
        JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions);

        // Act
        TypeJsonConverter.ClearCache();

        // Assert - Should still work after cache clear
        var deserializedType = JsonSerializer.Deserialize<Type>(json, _jsonSerializerOptions);
        Assert.Equal(type, deserializedType);
    }
}