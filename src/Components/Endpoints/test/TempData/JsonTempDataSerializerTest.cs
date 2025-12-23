// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.Endpoints;

public class JsonTempDataSerializerTest
{
    private static JsonTempDataSerializer CreateSerializer() => new JsonTempDataSerializer();

    public static TheoryData<Type> InvalidTypes
    {
        get
        {
            return new TheoryData<Type>
                {
                    { typeof(object) },
                    { typeof(object[]) },
                    { typeof(TestItem) },
                    { typeof(List<TestItem>) },
                    { typeof(Dictionary<string, TestItem>) },
                };
        }
    }

    public static TheoryData<Type> InvalidDictionaryKeyTypes
    {
        get
        {
            return new TheoryData<Type>
                {
                    { typeof(Dictionary<int, string>) },
                    { typeof(Dictionary<Uri, Guid>) },
                    { typeof(Dictionary<object, string>) },
                    { typeof(Dictionary<TestItem, TestItem>) }
                };
        }
    }

    public static TheoryData<Type> ValidTypes
    {
        get
        {
            return new TheoryData<Type>
                {
                    { typeof(int) },
                    { typeof(int[]) },
                    { typeof(string) },
                    { typeof(Uri) },
                    { typeof(Guid) },
                    { typeof(List<string>) },
                    { typeof(DateTimeOffset) },
                    { typeof(decimal) },
                    { typeof(Dictionary<string, int>) },
                    { typeof(Uri[]) },
                    { typeof(DayOfWeek) },
                    { typeof(DateTime) },
                    { typeof(bool) },
                    { typeof(TimeSpan) },
                };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidTypes))]
    public void EnsureObjectCanBeSerialized_ReturnsFalse_OnInvalidType(Type type)
    {
        // Arrange
        var serializer = CreateSerializer();

        // Act
        var result = serializer.EnsureObjectCanBeSerialized(type);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(InvalidDictionaryKeyTypes))]
    public void EnsureObjectCanBeSerialized_ReturnsFalse_OnInvalidDictionaryKeyType(Type type)
    {
        // Arrange
        var serializer = CreateSerializer();

        // Act
        var result = serializer.EnsureObjectCanBeSerialized(type);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(ValidTypes))]
    public void EnsureObjectCanBeSerialized_ReturnsTrue_OnValidType(Type type)
    {
        // Arrange
        var serializer = CreateSerializer();

        // Act
        var result = serializer.EnsureObjectCanBeSerialized(type);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Deserialize_Int()
    {
        // Arrange
        var serializer = CreateSerializer();
        var json = "42";
        var element = JsonDocument.Parse(json).RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        Assert.Equal(42, result);
    }

    [Fact]
    public void Deserialize_Bool()
    {
        // Arrange
        var serializer = CreateSerializer();
        var element = JsonDocument.Parse("true").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        Assert.Equal(true, result);
    }

    [Fact]
    public void Deserialize_String()
    {
        // Arrange
        var serializer = CreateSerializer();
        var element = JsonDocument.Parse("\"hello\"").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        Assert.Equal("hello", result);
    }

    [Fact]
    public void Deserialize_Guid()
    {
        // Arrange
        var serializer = CreateSerializer();
        var guid = Guid.NewGuid();
        var element = JsonDocument.Parse($"\"{guid}\"").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        Assert.Equal(guid, result);
    }

    [Fact]
    public void Deserialize_DateTime()
    {
        // Arrange
        var serializer = CreateSerializer();
        var dateTime = new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var element = JsonDocument.Parse($"\"{dateTime:O}\"").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        Assert.IsType<DateTime>(result);
        Assert.Equal(dateTime, result);
    }

    [Fact]
    public void Deserialize_Null()
    {
        // Arrange
        var serializer = CreateSerializer();
        var element = JsonDocument.Parse("null").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Deserialize_Array()
    {
        // Arrange
        var serializer = CreateSerializer();
        var element = JsonDocument.Parse("[1, 2, 3]").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        var array = Assert.IsType<object?[]>(result);
        Assert.Equal(3, array.Length);
        Assert.Equal(1, array[0]);
        Assert.Equal(2, array[1]);
        Assert.Equal(3, array[2]);
    }

    [Fact]
    public void Deserialize_EmptyArray()
    {
        // Arrange
        var serializer = CreateSerializer();
        var element = JsonDocument.Parse("[]").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        var array = Assert.IsType<object?[]>(result);
        Assert.Empty(array);
    }

    [Fact]
    public void Deserialize_Dictionary()
    {
        // Arrange
        var serializer = CreateSerializer();
        var element = JsonDocument.Parse("{\"key1\": 1, \"key2\": 2}").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        var dictionary = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal(1, dictionary["key1"]);
        Assert.Equal(2, dictionary["key2"]);
    }

    [Fact]
    public void Deserialize_StringArray()
    {
        // Arrange
        var serializer = CreateSerializer();
        var element = JsonDocument.Parse("[\"foo\", \"bar\"]").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        var array = Assert.IsType<object?[]>(result);
        Assert.Equal(2, array.Length);
        Assert.Equal("foo", array[0]);
        Assert.Equal("bar", array[1]);
    }

    [Fact]
    public void Deserialize_DictionaryWithStringValues()
    {
        // Arrange
        var serializer = CreateSerializer();
        var element = JsonDocument.Parse("{\"key1\": \"value1\", \"key2\": \"value2\"}").RootElement;

        // Act
        var result = serializer.Deserialize(element);

        // Assert
        var dictionary = Assert.IsType<Dictionary<string, object?>>(result);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("value1", dictionary["key1"]);
        Assert.Equal("value2", dictionary["key2"]);
    }

    private class TestItem
    {
        public int DummyInt { get; set; }
    }
}
