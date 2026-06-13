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

    [Fact]
    public void TryConvertTo_UInt_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<uint>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(uint), actual);
    }

    [Fact]
    public void TryConvertTo_NullableUInt_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<uint?>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_UInt_ValidValue()
    {
        var incomingValue = "42";
        var expected = 42u;

        var successfullyConverted = BindConverter.TryConvertTo<uint>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_UInt_NegativeValueReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<uint>("-42", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(uint), actual);
    }

    [Fact]
    public void TryConvertTo_UInt_ZeroValue()
    {
        var incomingValue = "0";
        var expected = 0u;

        var successfullyConverted = BindConverter.TryConvertTo<uint>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_UInt_MaxValue()
    {
        var incomingValue = uint.MaxValue.ToString(CultureInfo.InvariantCulture);
        var expected = uint.MaxValue;

        var successfullyConverted = BindConverter.TryConvertTo<uint>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TryConvertTo_UInt_EmptyOrNullReturnsFalse(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<uint>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(uint), actual);
    }

    [Fact]
    public void TryConvertTo_UInt_OverflowReturnsFalse()
    {
        var incomingValue = "4294967296"; // uint.MaxValue + 1

        var successfullyConverted = BindConverter.TryConvertTo<uint>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(uint), actual);
    }

    [Fact]
    public void TryConvertTo_NullableUInt_ValidValue()
    {
        var incomingValue = "42";
        var expected = 42u;

        var successfullyConverted = BindConverter.TryConvertTo<uint?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_NullableUInt_NegativeValueReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<uint?>("-42", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryConvertTo_NullableUInt_ValidEmptyOrNull(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<uint?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_NullableUInt_OverflowReturnsFalse()
    {
        var incomingValue = "4294967296"; // uint.MaxValue + 1

        var successfullyConverted = BindConverter.TryConvertTo<uint?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_UShort_ValidValue()
    {
        var incomingValue = "42";
        var expected = (ushort)42;

        var successfullyConverted = BindConverter.TryConvertTo<ushort>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_UShort_NegativeValueReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<ushort>("-42", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(ushort), actual);
    }

    [Fact]
    public void TryConvertTo_UShort_MaxValue()
    {
        var incomingValue = ushort.MaxValue.ToString(CultureInfo.InvariantCulture);
        var expected = ushort.MaxValue;

        var successfullyConverted = BindConverter.TryConvertTo<ushort>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_UShort_OverflowReturnsFalse()
    {
        var incomingValue = "65536"; // ushort.MaxValue + 1

        var successfullyConverted = BindConverter.TryConvertTo<ushort>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(ushort), actual);
    }

    [Fact]
    public void TryConvertTo_UShort_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<ushort>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(ushort), actual);
    }

    [Fact]
    public void TryConvertTo_NullableUShort_ValidValue()
    {
        var incomingValue = "42";
        var expected = (ushort)42;

        var successfullyConverted = BindConverter.TryConvertTo<ushort?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_NullableUShort_NegativeValueReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<ushort?>("-42", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryConvertTo_NullableUShort_ValidEmptyOrNull(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<ushort?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_NullableUShort_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<ushort?>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_ULong_ValidValue()
    {
        var incomingValue = "42";
        var expected = 42ul;

        var successfullyConverted = BindConverter.TryConvertTo<ulong>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_ULong_NegativeValueReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<ulong>("-42", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(ulong), actual);
    }

    [Fact]
    public void TryConvertTo_ULong_MaxValue()
    {
        var incomingValue = ulong.MaxValue.ToString(CultureInfo.InvariantCulture);
        var expected = ulong.MaxValue;

        var successfullyConverted = BindConverter.TryConvertTo<ulong>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_ULong_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<ulong>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(ulong), actual);
    }

    [Fact]
    public void TryConvertTo_NullableULong_ValidValue()
    {
        var incomingValue = "42";
        var expected = 42ul;

        var successfullyConverted = BindConverter.TryConvertTo<ulong?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_NullableULong_NegativeValueReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<ulong?>("-42", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryConvertTo_NullableULong_ValidEmptyOrNull(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<ulong?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_NullableULong_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<ulong?>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_Byte_ValidValue()
    {
        var incomingValue = "42";
        var expected = (byte)42;

        var successfullyConverted = BindConverter.TryConvertTo<byte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_Byte_NegativeValueReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<byte>("-42", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(byte), actual);
    }

    [Fact]
    public void TryConvertTo_Byte_MaxValue()
    {
        var incomingValue = byte.MaxValue.ToString(CultureInfo.InvariantCulture);
        var expected = byte.MaxValue;

        var successfullyConverted = BindConverter.TryConvertTo<byte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_Byte_OverflowReturnsFalse()
    {
        var incomingValue = "256"; // byte.MaxValue + 1

        var successfullyConverted = BindConverter.TryConvertTo<byte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(byte), actual);
    }

    [Fact]
    public void TryConvertTo_Byte_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<byte>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(byte), actual);
    }

    [Fact]
    public void TryConvertTo_NullableByte_ValidValue()
    {
        var incomingValue = "42";
        var expected = (byte)42;

        var successfullyConverted = BindConverter.TryConvertTo<byte?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_NullableByte_NegativeValueReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<byte?>("-42", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryConvertTo_NullableByte_ValidEmptyOrNull(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<byte?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_NullableByte_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<byte?>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_SByte_ValidPositiveValue()
    {
        var incomingValue = "42";
        var expected = (sbyte)42;

        var successfullyConverted = BindConverter.TryConvertTo<sbyte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_SByte_ValidNegativeValue()
    {
        var incomingValue = "-42";
        var expected = (sbyte)-42;

        var successfullyConverted = BindConverter.TryConvertTo<sbyte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_SByte_MaxValue()
    {
        var incomingValue = sbyte.MaxValue.ToString(CultureInfo.InvariantCulture);
        var expected = sbyte.MaxValue;

        var successfullyConverted = BindConverter.TryConvertTo<sbyte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_SByte_MinValue()
    {
        var incomingValue = sbyte.MinValue.ToString(CultureInfo.InvariantCulture);
        var expected = sbyte.MinValue;

        var successfullyConverted = BindConverter.TryConvertTo<sbyte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void TryConvertTo_SByte_OverflowPositiveReturnsFalse()
    {
        var incomingValue = "128"; // sbyte.MaxValue + 1

        var successfullyConverted = BindConverter.TryConvertTo<sbyte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(sbyte), actual);
    }

    [Fact]
    public void TryConvertTo_SByte_OverflowNegativeReturnsFalse()
    {
        var incomingValue = "-129"; // sbyte.MinValue - 1

        var successfullyConverted = BindConverter.TryConvertTo<sbyte>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(sbyte), actual);
    }

    [Fact]
    public void TryConvertTo_SByte_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<sbyte>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default(sbyte), actual);
    }

    [Fact]
    public void TryConvertTo_NullableSByte_ValidValue()
    {
        var incomingValue = "-42";
        var expected = (sbyte)-42;

        var successfullyConverted = BindConverter.TryConvertTo<sbyte?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryConvertTo_NullableSByte_ValidEmptyOrNull(string incomingValue)
    {
        var successfullyConverted = BindConverter.TryConvertTo<sbyte?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_NullableSByte_OverflowReturnsFalse()
    {
        var incomingValue = "128"; // sbyte.MaxValue + 1

        var successfullyConverted = BindConverter.TryConvertTo<sbyte?>(incomingValue, CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_NullableSByte_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<sbyte?>("not a number", CultureInfo.CurrentCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void TryConvertTo_TypeConverter_InvalidStringReturnsFalse()
    {
        var successfullyConverted = BindConverter.TryConvertTo<Person>("not valid json", CultureInfo.CurrentCulture, out var actual);

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
}
