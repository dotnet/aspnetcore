// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace Microsoft.AspNetCore.Components;

// This is some basic coverage, it's not in depth because there are many many APIs here
// and they mostly call through to CoreFx. We don't want to test the globalization details
// of .NET in detail where we can avoid it.
//
// Instead there's a sampling of things that have somewhat unique behavior or semantics.
public class BindConverterTest
{
    [Fact]
    public void FormatValue_Bool()
    {
        // Arrange
        var value = true;
        var expected = true;

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Bool_Generic()
    {
        // Arrange
        var value = true;
        var expected = true;

        // Act
        var actual = BindConverter.FormatValue<bool>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableBool()
    {
        // Arrange
        var value = (bool?)true;
        var expected = true;

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableBool_Generic()
    {
        // Arrange
        var value = true;
        var expected = true;

        // Act
        var actual = BindConverter.FormatValue<bool?>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableBoolNull()
    {
        // Arrange
        var value = (bool?)null;
        var expected = (bool?)null;

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableBoolNull_Generic()
    {
        // Arrange
        var value = (bool?)null;
        var expected = (bool?)null;

        // Act
        var actual = BindConverter.FormatValue<bool?>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Int()
    {
        // Arrange
        var value = 17;
        var expected = "17";

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Int_Generic()
    {
        // Arrange
        var value = 17;
        var expected = "17";

        // Act
        var actual = BindConverter.FormatValue<int>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableInt()
    {
        // Arrange
        var value = (int?)17;
        var expected = "17";

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableInt_Generic()
    {
        // Arrange
        var value = 17;
        var expected = "17";

        // Act
        var actual = BindConverter.FormatValue<int?>(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_DateTime()
    {
        // Arrange
        var value = DateTime.Now;
        var expected = value.ToString(CultureInfo.CurrentCulture);

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_DateTime_Format()
    {
        // Arrange
        var value = DateTime.Now;
        var expected = value.ToString("MM-yyyy", CultureInfo.InvariantCulture);

        // Act
        var actual = BindConverter.FormatValue(value, "MM-yyyy", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_DateOnly()
    {
        // Arrange
        var value = DateOnly.FromDateTime(DateTime.Now);
        var expected = value.ToString(CultureInfo.CurrentCulture);

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_DateOnly_Format()
    {
        // Arrange
        var value = DateOnly.FromDateTime(DateTime.Now);
        var expected = value.ToString("MM-yyyy", CultureInfo.InvariantCulture);

        // Act
        var actual = BindConverter.FormatValue(value, "MM-yyyy", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_TimeOnly()
    {
        // Arrange
        var value = TimeOnly.FromDateTime(DateTime.Now);
        var expected = value.ToString(CultureInfo.CurrentCulture);

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_TimeOnly_Format()
    {
        // Arrange
        var value = TimeOnly.FromDateTime(DateTime.Now);
        var expected = value.ToString("HH:mm", CultureInfo.InvariantCulture);

        // Act
        var actual = BindConverter.FormatValue(value, "HH:mm", CultureInfo.InvariantCulture);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Enum()
    {
        // Arrange
        var value = SomeLetters.A;
        var expected = value.ToString();

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Enum_OutOfRange()
    {
        // Arrange
        var value = SomeLetters.A + 3;
        var expected = value.ToString();

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_NullableEnum()
    {
        // Arrange
        var value = (SomeLetters?)null;

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Null(actual);
    }

    [Fact]
    public void FormatValue_TypeConverter()
    {
        // Arrange
        var value = new Person()
        {
            Name = "Glenn",
            Age = 47,
        };

        var expected = JsonSerializer.Serialize(value);

        // Act
        var actual = BindConverter.FormatValue(value);

        // Assert
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_Guid_Valid()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var incomingValue = expected.ToString();

        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.Equal(expected, actual);
        Assert.True(successfullyConverted);
    }

    [Theory]
    [InlineData("invalidguid")]
    [InlineData("")]
    [InlineData(null)]
    public void TryConvertTo_Guid_Invalid(string incomingValue)
    {
        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.False(successfullyConverted);
        Assert.Equal(Guid.Empty, actual);
    }

    [Fact]
    public void TryConvertTo_NullableGuid_Valid()
    {
        // Arrange
        var expected = Guid.NewGuid();
        var incomingValue = expected.ToString();

        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryConvertTo_NullableGuid_ValidEmptyOrNull(string incomingValue)
    {
        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_NullableGuid__Invalid()
    {
        // Arrange
        var value = "invalidguid";

        // Act
        var successfullyConverted = BindConverter.TryConvertTo<Guid?>(value, CultureInfo.CurrentCulture, out var actual);

        // Assert
        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    private enum SomeLetters
    {
        A,
        B,
        C,
        Q,
    }

    [TypeConverter(typeof(PersonConverter))]
    private class Person
    {
        public string Name { get; set; }

        public int Age { get; set; }
    }

    private class PersonConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
            {
                return true;
            }

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is string text)
            {
                return JsonSerializer.Deserialize<Person>(text);
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return true;
            }

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                return JsonSerializer.Serialize((Person)value);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [Fact]
    public void FormatValue_Array_Null()
    {
        string[]? value = null;
        string? expected = null;

        var actual = BindConverter.FormatValue(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Array_Empty()
    {
        string[] value = new string[0];
        string expected = "[]";

        var actual = BindConverter.FormatValue(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Array_Values()
    {
        int[] value = new[] { 1, 2, 3 };
        string expected = "[\"1\", \"2\", \"3\"]";

        var actual = BindConverter.FormatValue(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Array_NullElement()
    {
        string?[] value = new[] { "a", null, "b" };
        string expected = "[\"a\", \"\", \"b\"]";

        var actual = BindConverter.FormatValue(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Array_NullableIntElement()
    {
        int?[] value = new int?[] { 1, null, 3 };
        string expected = "[\"1\", \"\", \"3\"]";

        var actual = BindConverter.FormatValue(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Array_JsonEncoding()
    {
        string[] value = new[] { "a\"b", "c\n" };
        var expected = "[\"" + JsonEncodedText.Encode("a\"b").Value + "\", \"" + JsonEncodedText.Encode("c\n").Value + "\"]";

        var actual = BindConverter.FormatValue(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Array_TypeConverterElements()
    {
        var p = new Person()
        {
            Name = "Glenn",
            Age = 47,
        };
        Person[] value = new[] { p };
        var encoded = JsonEncodedText.Encode(JsonSerializer.Serialize(p)).Value;
        var expected = "[\"" + encoded + "\"]";

        var actual = BindConverter.FormatValue(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void FormatValue_Array_NestedArrays()
    {
        int[][] value = new[] { new[] { 1, 2 }, new[] { 3 } };
        var inner0 = "[\"1\", \"2\"]";
        var inner1 = "[\"3\"]";
        var expected = "[\"" + JsonEncodedText.Encode(inner0).Value + "\", \"" + JsonEncodedText.Encode(inner1).Value + "\"]";

        var actual = BindConverter.FormatValue(value);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_Array_NullInput_Fails()
    {
        object? input = null;

        var success = BindConverter.TryConvertTo<int[]>(input, CultureInfo.CurrentCulture, out var actual);

        Assert.False(success);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_Array_ElementParseFailure_Fails()
    {
        object input = new object[] { "1", "invalid", "3" };

        var success = BindConverter.TryConvertTo<int[]>(input, CultureInfo.CurrentCulture, out var actual);

        Assert.False(success);
        Assert.Null(actual);
    }
}
