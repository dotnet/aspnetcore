// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;

namespace Microsoft.AspNetCore.Mvc.NewtonsoftJson;

public class BsonTempDataSerializerTest : TempDataSerializerTestBase
{
    protected override TempDataSerializer GetTempDataSerializer() => new BsonTempDataSerializer();

    public static TheoryData<object, Type> InvalidTypes
    {
        get
        {
            return new TheoryData<object, Type>
                {
                    { new object(), typeof(object) },
                    { new object[3], typeof(object) },
                    { new TestItem(), typeof(TestItem) },
                    { new List<TestItem>(), typeof(TestItem) },
                    { new Dictionary<string, TestItem>(), typeof(TestItem) },
                };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidTypes))]
    public void EnsureObjectCanBeSerialized_ThrowsException_OnInvalidType(object value, Type type)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            BsonTempDataSerializer.EnsureObjectCanBeSerialized(value);
        });
        Assert.Equal($"The '{typeof(BsonTempDataSerializer).FullName}' cannot serialize " +
            $"an object of type '{type}'.",
            exception.Message);
    }

    public static TheoryData<object, Type> InvalidDictionaryTypes
    {
        get
        {
            return new TheoryData<object, Type>
                {
                    { new Dictionary<int, string>(), typeof(int) },
                    { new Dictionary<Uri, Guid>(), typeof(Uri) },
                    { new Dictionary<object, string>(), typeof(object) },
                    { new Dictionary<TestItem, TestItem>(), typeof(TestItem) }
                };
        }
    }

    [Theory]
    [MemberData(nameof(InvalidDictionaryTypes))]
    public void EnsureObjectCanBeSerialized_ThrowsException_OnInvalidDictionaryType(object value, Type type)
    {
        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
        {
            BsonTempDataSerializer.EnsureObjectCanBeSerialized(value);
        });
        Assert.Equal($"The '{typeof(BsonTempDataSerializer).FullName}' cannot serialize a dictionary " +
            $"with a key of type '{type}'. The key must be of type 'System.String'.",
            exception.Message);
    }

    public static TheoryData<object> ValidTypes
    {
        get
        {
            return new TheoryData<object>
                {
                    { 10 },
                    { new int[]{ 10, 20 } },
                    { "FooValue" },
                    { new Uri("http://Foo") },
                    { Guid.NewGuid() },
                    { new List<string> { "foo", "bar" } },
                    { new DateTimeOffset() },
                    { 100.1m },
                    { new Dictionary<string, int>() },
                    { new Uri[] { new Uri("http://Foo"), new Uri("http://Bar") } },
                    { DayOfWeek.Sunday },
                };
        }
    }

    [Fact]
    public void RoundTripTest_ArrayOfIntegers()
    {
        // Arrange
        var key = "test-key";
        var value = new[] { 1, -2, 3 };
        var testProvider = GetTempDataSerializer();
        var input = new Dictionary<string, object>
            {
                { key, value },
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<int[]>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_DateTime()
    {
        // Arrange
        var key = "test-key";
        var value = new DateTime(2007, 1, 1);
        var testProvider = GetTempDataSerializer();
        var input = new Dictionary<string, object>
            {
                { key, value },
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<DateTime>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_Guid()
    {
        // Arrange
        var key = "test-key";
        var value = Guid.NewGuid();
        var testProvider = GetTempDataSerializer();
        var input = new Dictionary<string, object>
            {
                { key, value },
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<Guid>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Theory]
    [InlineData(2147483648)]
    [InlineData(-2147483649)]
    public void RoundTripTest_LongValue(long value)
    {
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var input = new Dictionary<string, object>
            {
                { key, value },
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<long>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_Double()
    {
        // Arrange
        var key = "test-key";
        var value = 10d;
        var testProvider = GetTempDataSerializer();
        var input = new Dictionary<string, object>
            {
                { key, value },
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = (double)values[key];
        Assert.Equal(value, roundTripValue);
    }

    [Theory]
    [MemberData(nameof(ValidTypes))]
    public void EnsureObjectCanBeSerialized_DoesNotThrow_OnValidType(object value)
    {
        // Act & Assert (Does not throw)
        BsonTempDataSerializer.EnsureObjectCanBeSerialized(value);
    }

    [Fact]
    public override void RoundTripTest_GuidToString()
    {
        // Documents the behavior of round-tripping a Guid value as a string
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = Guid.NewGuid();
        var input = new Dictionary<string, object>
            {
                { key, value.ToString() }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<string>(values[key]);
        Assert.Equal(value.ToString(), roundTripValue);
    }

    [Fact]
    public void RoundTripTest_ListOfDateTime()
    {
        // Arrange
        var key = "test-key";
        var dateTime = new DateTime(2007, 1, 1);
        var testProvider = GetTempDataSerializer();
        var value = new List<DateTime>
            {
                dateTime,
                dateTime.AddDays(3),
            };

        var input = new Dictionary<string, object>
            {
                { key, value }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<DateTime[]>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    private class TestItem
    {
        public int DummyInt { get; set; }
    }
}
