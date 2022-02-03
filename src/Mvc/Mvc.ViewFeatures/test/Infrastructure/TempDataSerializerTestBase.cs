// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace Microsoft.AspNetCore.Mvc.ViewFeatures.Infrastructure;

public abstract class TempDataSerializerTestBase
{
    [Fact]
    public void DeserializeTempData_ReturnsEmptyDictionary_DataIsEmpty()
    {
        // Arrange
        var serializer = GetTempDataSerializer();

        // Act
        var tempDataDictionary = serializer.Deserialize(new byte[0]);

        // Assert
        Assert.NotNull(tempDataDictionary);
        Assert.Empty(tempDataDictionary);
    }

    [Fact]
    public void RoundTripTest_NullValue()
    {
        // Arrange
        var key = "NullKey";
        var testProvider = GetTempDataSerializer();
        var input = new Dictionary<string, object>
            {
                { key, null }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        Assert.True(values.ContainsKey(key));
        Assert.Null(values[key]);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(3340)]
    [InlineData(int.MaxValue)]
    [InlineData(int.MinValue)]
    public void RoundTripTest_IntValue(int value)
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
        var roundTripValue = Assert.IsType<int>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(10)]
    public void RoundTripTest_NullableInt(int? value)
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
        var roundTripValue = (int?)values[key];
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_StringValue()
    {
        // Arrange
        var key = "test-key";
        var value = "test-value";
        var testProvider = GetTempDataSerializer();
        var input = new Dictionary<string, object>
            {
                { key, value },
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<string>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_Enum()
    {
        // Arrange
        var key = "test-key";
        var value = DayOfWeek.Friday;
        var testProvider = GetTempDataSerializer();
        var input = new Dictionary<string, object>
            {
                { key, value },
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = (DayOfWeek)values[key];
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_DateTimeValue()
    {
        // Arrange
        var key = "test-key";
        var value = new DateTime(2009, 1, 1, 12, 37, 43);
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
    public void RoundTripTest_GuidValue()
    {
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = Guid.NewGuid();
        var input = new Dictionary<string, object>
            {
                { key, value }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<Guid>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public virtual void RoundTripTest_DateTimeToString()
    {
        // Documents the behavior of round-tripping a DateTime value as a string
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = new DateTime(2009, 1, 1, 12, 37, 43);
        var input = new Dictionary<string, object>
            {
                { key, value.ToString(CultureInfo.InvariantCulture) }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<string>(values[key]);
        Assert.Equal(value.ToString(CultureInfo.InvariantCulture), roundTripValue);
    }

    [Fact]
    public virtual void RoundTripTest_StringThatIsNotCompliantGuid()
    {
        // Documents the behavior of round-tripping a Guid with a non-default format specifier
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = Guid.NewGuid();
        var input = new Dictionary<string, object>
            {
                { key, value.ToString("N") }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<string>(values[key]);
        Assert.Equal(value.ToString("N"), roundTripValue);
    }

    [Fact]
    public virtual void RoundTripTest_GuidToString()
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
        var roundTripValue = Assert.IsType<Guid>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_CollectionOfInts()
    {
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = new[] { 1, 2, 4, 3 };
        var input = new Dictionary<string, object>
            {
                { key, value }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<int[]>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_ArrayOfStringss()
    {
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = new[] { "Hello", "world" };
        var input = new Dictionary<string, object>
            {
                { key, value }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<string[]>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_ListOfStringss()
    {
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = new List<string> { "Hello", "world" };
        var input = new Dictionary<string, object>
            {
                { key, value }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<string[]>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public void RoundTripTest_DictionaryOfString()
    {
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = new Dictionary<string, string>
            {
                { "Key1", "Value1" },
                { "Key2", "Value2" },
            };
        var input = new Dictionary<string, object>
            {
                { key, value }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<Dictionary<string, string>>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    [Fact]
    public virtual void RoundTripTest_DictionaryOfInt()
    {
        // Arrange
        var key = "test-key";
        var testProvider = GetTempDataSerializer();
        var value = new Dictionary<string, int>
            {
                { "Key1", 7 },
                { "Key2", 24 },
            };
        var input = new Dictionary<string, object>
            {
                { key, value }
            };

        // Act
        var bytes = testProvider.Serialize(input);
        var values = testProvider.Deserialize(bytes);

        // Assert
        var roundTripValue = Assert.IsType<Dictionary<string, int>>(values[key]);
        Assert.Equal(value, roundTripValue);
    }

    protected abstract TempDataSerializer GetTempDataSerializer();
}

