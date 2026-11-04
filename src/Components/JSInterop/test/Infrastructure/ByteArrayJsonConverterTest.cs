// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.JSInterop.Infrastructure;

public class ByteArrayJsonConverterTest
{
    private readonly JSRuntime JSRuntime;
    private JsonSerializerOptions JsonSerializerOptions => JSRuntime.JsonSerializerOptions;

    public ByteArrayJsonConverterTest()
    {
        JSRuntime = new TestJSRuntime();
    }

    [Fact]
    public void Read_Throws_IfByteArraysToBeRevivedIsEmpty()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));
        Assert.Equal("JSON serialization is attempting to deserialize an unexpected byte array.", ex.Message);
    }

    [Fact]
    public void Read_Throws_IfJsonIsMissingByteArraysProperty()
    {
        // Arrange
        JSRuntime.ByteArraysToBeRevived.Append(new byte[] { 1, 2 });

        var json = "{}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));
        Assert.Equal("Unexpected JSON Token EndObject, expected 'PropertyName'.", ex.Message);
    }

    [Fact]
    public void Read_Throws_IfJsonContainsUnknownContent()
    {
        // Arrange
        JSRuntime.ByteArraysToBeRevived.Append(new byte[] { 1, 2 });

        var json = "{\"foo\":2}";

        // Act & Assert
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));
        Assert.Equal("Unexpected JSON Property foo.", ex.Message);
    }

    [Fact]
    public void Read_Throws_IfJsonIsIncomplete()
    {
        // Arrange
        JSRuntime.ByteArraysToBeRevived.Append(new byte[] { 1, 2 });

        var json = $"{{\"__byte[]\":0";

        // Act & Assert
        var ex = Record.Exception(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));
        Assert.IsAssignableFrom<JsonException>(ex);
    }

    [Fact]
    public void Read_ReadsBase64EncodedStrings()
    {
        // Arrange
        var expected = new byte[] { 1, 5, 8 };
        var json = JsonSerializer.Serialize(expected);

        // Act
        var deserialized = JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions)!;

        // Assert
        Assert.Equal(expected, deserialized);
    }

    [Fact]
    public void Read_ThrowsIfTheInputIsNotAValidBase64String()
    {
        // Arrange
        var json = "\"Hello world\"";

        // Act
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));

        // Assert
        Assert.Equal("JSON serialization is attempting to deserialize an unexpected byte array.", ex.Message);
    }

    [Fact]
    public void Read_ReadsJson()
    {
        // Arrange
        var byteArray = new byte[] { 1, 5, 7 };
        JSRuntime.ByteArraysToBeRevived.Append(byteArray);

        var json = $"{{\"__byte[]\":0}}";

        // Act
        var deserialized = JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions)!;

        // Assert
        Assert.Equal(byteArray, deserialized);
    }

    [Fact]
    public void Read_ByteArraysIdAppearsMultipleTimesThrows()
    {
        // Arrange
        var byteArray = new byte[] { 1, 5, 7 };
        JSRuntime.ByteArraysToBeRevived.Append(byteArray);

        var json = $"{{\"__byte[]\":9120,\"__byte[]\":0}}";

        // Act
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));

        // Assert
        Assert.Equal("Unexpected JSON Token PropertyName, expected 'EndObject'.", ex.Message);
    }

    [Fact]
    public void Read_ByteArraysIdValueInvalidStringThrows()
    {
        // Arrange
        var byteArray = new byte[] { 1, 5, 7 };
        JSRuntime.ByteArraysToBeRevived.Append(byteArray);

        var json = $"{{\"__byte[]\":\"something\"}}";

        // Act
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));

        // Assert
        Assert.Equal("Unexpected JSON Token String, expected 'Number'.", ex.Message);
    }

    [Fact]
    public void Read_ByteArraysIdValueLargeNumberThrows()
    {
        // Arrange
        var byteArray = new byte[] { 1, 5, 7 };
        JSRuntime.ByteArraysToBeRevived.Append(byteArray);

        var json = $"{{\"__byte[]\":5000000000}}";

        // Act
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));

        // Assert
        Assert.Equal("Unexpected number, expected 32-bit integer.", ex.Message);
    }

    [Fact]
    public void Read_ByteArraysIdValueNegativeNumberThrows()
    {
        // Arrange
        var byteArray = new byte[] { 1, 5, 7 };
        JSRuntime.ByteArraysToBeRevived.Append(byteArray);

        var json = $"{{\"__byte[]\":-5}}";

        // Act
        var ex = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions));

        // Assert
        Assert.Equal("Byte array -5 not found.", ex.Message);
    }

    [Fact]
    public void Read_ReadsJson_WithFormatting()
    {
        // Arrange
        var byteArray = new byte[] { 1, 5, 7 };
        JSRuntime.ByteArraysToBeRevived.Append(byteArray);

        var json =
@$"{{
            ""__byte[]"": 0
        }}";

        // Act
        var deserialized = JsonSerializer.Deserialize<byte[]>(json, JsonSerializerOptions)!;

        // Assert
        Assert.Equal(byteArray, deserialized);
    }

    [Fact]
    public void Read_ReturnsTheCorrectInstance()
    {
        // Arrange
        // Track a few arrays and verify that the deserialized value returns the correct value.
        var byteArray1 = new byte[] { 1, 5, 7 };
        var byteArray2 = new byte[] { 2, 6, 8 };
        var byteArray3 = new byte[] { 2, 6, 8 };

        JSRuntime.ByteArraysToBeRevived.Append(byteArray1);
        JSRuntime.ByteArraysToBeRevived.Append(byteArray2);
        JSRuntime.ByteArraysToBeRevived.Append(byteArray3);

        var json = $"[{{\"__byte[]\":2}},{{\"__byte[]\":1}}]";

        // Act
        var deserialized = JsonSerializer.Deserialize<byte[][]>(json, JsonSerializerOptions)!;

        // Assert
        Assert.Same(byteArray3, deserialized[0]);
        Assert.Same(byteArray2, deserialized[1]);
    }

    [Fact]
    public void WriteJsonMultipleTimes_IncrementsByteArrayId()
    {
        // Arrange
        var byteArray = new byte[] { 1, 5, 7 };

        // Act & Assert
        for (var i = 0; i < 10; i++)
        {
            var json = JsonSerializer.Serialize(byteArray, JsonSerializerOptions);
            Assert.Equal($"{{\"__byte[]\":{i + 1}}}", json);
        }
    }
}
