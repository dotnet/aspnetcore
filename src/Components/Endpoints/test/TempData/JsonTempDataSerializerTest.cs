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
                    { typeof(string[]) },
                    { typeof(Guid) },
                    { typeof(Guid[]) },
                    { typeof(Dictionary<string, int>) },
                    { typeof(Dictionary<string, string>) },
                    { typeof(Dictionary<string, bool>) },
                    { typeof(Dictionary<string, Guid>) },
                    { typeof(Dictionary<string, DateTime>) },
                    { typeof(DateTime) },
                    { typeof(DateTime[]) },
                    { typeof(bool) },
                    { typeof(bool[]) },
                    { typeof(TestEnum) },
                    { typeof(TestEnum[]) },
                    { typeof(List<int>) },
                    { typeof(List<string>) },
                    { typeof(List<bool>) },
                    { typeof(List<Guid>) },
                    { typeof(List<DateTime>) },
                    { typeof(HashSet<int>) },
                };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidTypes))]
    public void CanSerialize_ReturnsFalse_OnInvalidType(Type type)
    {
        var serializer = CreateSerializer();

        var result = serializer.CanSerialize(type);

        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(InvalidDictionaryKeyTypes))]
    public void CanSerialize_ReturnsFalse_OnInvalidDictionaryKeyType(Type type)
    {
        var serializer = CreateSerializer();

        var result = serializer.CanSerialize(type);

        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(ValidTypes))]
    public void CanSerialize_ReturnsTrue_OnValidType(Type type)
    {
        var serializer = CreateSerializer();

        var result = serializer.CanSerialize(type);

        Assert.True(result);
    }

    public static TheoryData<object, Type> RoundTripData => new()
    {
        { 42, typeof(int) },
        { true, typeof(bool) },
        { "hello", typeof(string) },
        { Guid.Parse("5fa6e1de-d0b4-4272-a629-2e1382af8b51"), typeof(Guid) },
        { new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc), typeof(DateTime) },
        { new int[] { 1, 2, 3 }, typeof(int[]) },
        { new string[] { "foo", "bar" }, typeof(string[]) },
        { new bool[] { true, false }, typeof(bool[]) },
        { new DateTime[] { new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc), new DateTime(2008, 1, 1, 0, 0, 0, DateTimeKind.Utc) }, typeof(DateTime[]) },
        { new Guid[] { Guid.Parse("5fa6e1de-d0b4-4272-a629-2e1382af8b51"), Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890") }, typeof(Guid[]) },
        { new Dictionary<string, int> { { "key1", 1 }, { "key2", 2 } }, typeof(Dictionary<string, int>) },
        { new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } }, typeof(Dictionary<string, string>) },
        { new Dictionary<string, bool> { { "key1", true }, { "key2", false } }, typeof(Dictionary<string, bool>) },
        { new Dictionary<string, Guid> { { "key1", Guid.Parse("5fa6e1de-d0b4-4272-a629-2e1382af8b51") } }, typeof(Dictionary<string, Guid>) },
        { new Dictionary<string, DateTime> { { "key1", new DateTime(2007, 1, 1, 0, 0, 0, DateTimeKind.Utc) } }, typeof(Dictionary<string, DateTime>) },
        { Array.Empty<int>(), typeof(int[]) },
    };

    [Theory]
    [MemberData(nameof(RoundTripData))]
    public void RoundTrip(object value, Type type)
    {
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, (object Value, Type Type)>
        {
            { "key", (value, type) }
        });

        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument!);

        Assert.Equal(value, result["key"].Value);
    }

    [Fact]
    public void RoundTrip_Null()
    {
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, (object Value, Type Type)>
        {
            { "key", ((object)null, (Type)null) }
        });

        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument!);

        Assert.Null(result["key"].Value);
    }

    [Fact]
    public void RoundTrip_Enum()
    {
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, (object Value, Type Type)>
        {
            { "key", (TestEnum.Value2, typeof(TestEnum)) }
        });

        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument!);

        Assert.Equal(1, result["key"].Value);
    }

    [Fact]
    public void RoundTrip_EnumArray()
    {
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, (object Value, Type Type)>
        {
            { "key", (new TestEnum[] { TestEnum.Value1, TestEnum.Value2 }, typeof(TestEnum[])) }
        });

        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument!);

        var array = Assert.IsType<int[]>(result["key"].Value);
        Assert.Equal(0, array[0]);
        Assert.Equal(1, array[1]);
    }

    [Fact]
    public void RoundTrip_ListDeserializesAsArray()
    {
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, (object Value, Type Type)>
        {
            { "key", (new List<int> { 1, 2, 3 }, typeof(List<int>)) }
        });

        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument!);

        var array = Assert.IsType<int[]>(result["key"].Value);
        Assert.Equal(3, array.Length);
        Assert.Equal(1, array[0]);
        Assert.Equal(2, array[1]);
        Assert.Equal(3, array[2]);
    }

    [Fact]
    public void RoundTrip_HashSetDeserializesAsArray()
    {
        var serializer = CreateSerializer();
        var serialized = serializer.SerializeData(new Dictionary<string, (object Value, Type Type)>
        {
            { "key", (new HashSet<int> { 1, 2, 3 }, typeof(HashSet<int>)) }
        });

        var jsonDocument = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(serialized);
        var result = serializer.DeserializeData(jsonDocument!);

        var array = Assert.IsType<int[]>(result["key"].Value);
        Assert.Equal(3, array.Length);
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
