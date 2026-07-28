// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
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
                    { typeof(long) },
                    { typeof(long[]) },
                    { typeof(double) },
                    { typeof(double[]) },
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
                    { typeof(Guid) },
                    { typeof(List<string>) },
                    { typeof(Dictionary<string, int>) },
                    { typeof(DateTime) },
                    { typeof(bool) },
                    { typeof(Guid[]) }
                };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidTypes))]
    public void CanSerialize_ReturnsFalse_OnInvalidType(Type type)
    {
        // Arrange
        var serializer = CreateSerializer();

        // Act
        var result = serializer.CanSerialize(type);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(InvalidDictionaryKeyTypes))]
    public void CanSerialize_ReturnsFalse_OnInvalidDictionaryKeyType(Type type)
    {
        // Arrange
        var serializer = CreateSerializer();

        // Act
        var result = serializer.CanSerialize(type);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(ValidTypes))]
    public void CanSerialize_ReturnsTrue_OnValidType(Type type)
    {
        // Arrange
        var serializer = CreateSerializer();

        // Act
        var result = serializer.CanSerialize(type);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Deserialize_Int()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", 42 }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        Assert.Equal(42, result["key"]);
    }

    [Fact]
    public void Deserialize_Bool()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", true }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        Assert.Equal(true, result["key"]);
    }

    [Fact]
    public void Deserialize_String()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", "hello" }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        Assert.Equal("hello", result["key"]);
    }

    [Fact]
    public void Deserialize_Guid()
    {
        // Arrange
        var serializer = CreateSerializer();
        var guid = Guid.NewGuid();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", guid }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        Assert.Equal(guid, result["key"]);
    }

    [Fact]
    public void Deserialize_DateTime()
    {
        // Arrange
        var serializer = CreateSerializer();
        var dateTime = new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", dateTime }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        Assert.IsType<DateTime>(result["key"]);
        Assert.Equal(dateTime, result["key"]);
    }

    [Fact]
    public void Deserialize_Null()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", null }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        Assert.Null(result["key"]);
    }

    [Fact]
    public void Deserialize_Array()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", new int[] { 1, 2, 3 } }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var array = Assert.IsType<int[]>(result["key"]);
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
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", Array.Empty<int>() }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var array = Assert.IsType<object[]>(result["key"]);
        Assert.Empty(array);
    }

    [Fact]
    public void Deserialize_Dictionary()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", new Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } } }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var dictionary = Assert.IsType<Dictionary<string, object>>(result["key"]);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal(1, dictionary["key1"]);
        Assert.Equal(2, dictionary["key2"]);
    }

    [Fact]
    public void Deserialize_StringArray()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", new string[] { "foo", "bar" } }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var array = Assert.IsType<string[]>(result["key"]);
        Assert.Equal(2, array.Length);
        Assert.Equal("foo", array[0]);
        Assert.Equal("bar", array[1]);
    }

    [Fact]
    public void Deserialize_DictionaryWithStringValues()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } } }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var dictionary = Assert.IsType<Dictionary<string, object>>(result["key"]);
        Assert.Equal(2, dictionary.Count);
        Assert.Equal("value1", dictionary["key1"]);
        Assert.Equal("value2", dictionary["key2"]);
    }

    [Fact]
    public void Deserialize_NestedArrays()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", new object[] { new int[] { 1 }, new int[] { 2, 3, 4 } } }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var array = Assert.IsType<object[]>(result["key"]);
        Assert.Equal(new int[] { 1 }, array[0]);
        Assert.Equal(new int[] { 2, 3, 4 }, array[1]);
    }

    [Fact]
    public void Deserialize_BoolArray()
    {
        // Arrange
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", new bool[] { true, false } }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var array = Assert.IsType<bool[]>(result["key"]);
        Assert.True(array[0]);
        Assert.False(array[1]);
    }

    [Fact]
    public void Deserialize_DateTimeArray()
    {
        // Arrange
        var serializer = CreateSerializer();
        var dateTime1 = new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var dateTime2 = new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", new DateTime[] { dateTime1, dateTime2 } }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var array = Assert.IsType<DateTime[]>(result["key"]);
        Assert.Equal(dateTime1, array[0]);
        Assert.Equal(dateTime2, array[1]);
    }

    [Fact]
    public void Deserialize_GuidArray()
    {
        // Arrange
        var serializer = CreateSerializer();
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();
        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", new Guid[] { guid1, guid2 } }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        var array = Assert.IsType<Guid[]>(result["key"]);
        Assert.Equal(guid1, array[0]);
        Assert.Equal(guid2, array[1]);
    }

    [Fact]
    public void Deserialize_Enum()
    {
        // Arrange
        var serializer = CreateSerializer();

        var serialized = serializer.SerializeData(new Dictionary<string, object>
        {
            { "key", TestEnum.Value2 }
        });

        // Act
        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument);

        // Assert
        Assert.Equal(1, result["key"]);
    }

    private class TestItem
    {
        public int DummyInt { get; set; }
    }

    private enum TestEnum
    {
        Value1,
        Value2
    }
}
