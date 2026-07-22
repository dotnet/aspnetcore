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

    [Theory]
    [InlineData("1e-6", 1e-6f)]
    [InlineData("1E-6", 1E-6f)]
    [InlineData("2E-06", 2E-06f)]
    [InlineData("3.5e4", 3.5e4f)]
    [InlineData("-4.25E-03", -4.25E-03f)]
    [InlineData("4E+8", 4E8f)]
    [InlineData("+4E8", 4E8f)]
    public void TryConvertToFloat_AcceptsScientificNotation(string input, float expected)
    {
        var result = BindConverter.TryConvertTo<float>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1e-6", 1e-6)]
    [InlineData("1E-6", 1E-6)]
    [InlineData("2E-06", 2E-06)]
    [InlineData("2.0e-6", 2.0e-6)]
    [InlineData("3.5e10", 3.5e10)]
    [InlineData("-3.2E-04", -3.2E-04)]
    [InlineData("4E8", 4E8)]
    [InlineData("4E+8", 4E8)]
    [InlineData("+4E8", 4E8)]
    public void TryConvertToDouble_AcceptsScientificNotation(string input, double expected)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,234", 1234d)]
    [InlineData("1,234.56", 1234.56d)]
    [InlineData("12,345.678", 12345.678d)]
    [InlineData("1,234,567.89", 1234567.89d)]
    [InlineData("-1,234.56", -1234.56d)]
    public void TryConvertToDouble_AcceptsGroupSeparators_EnUS(
        string input,
        double expected)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.GetCultureInfo("en-US"),
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,234.56.78")]
    [InlineData("1,234E")]
    [InlineData("1,234E-")]
    [InlineData("1,234E+")]
    public void TryConvertToDouble_RejectsInvalidGroupSeparatorOrExponentInput(
    string input)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.GetCultureInfo("en-US"),
            out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Fact]
    public void TryConvertToDouble_AcceptsScientificNotationWithGroupSeparators_FrFR()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var nfi = culture.NumberFormat;

        var input = $"1{nfi.NumberGroupSeparator}234{nfi.NumberDecimalSeparator}56E2";

        var result = BindConverter.TryConvertTo<double>(
            input,
            culture,
            out var value);

        Assert.True(result);
        Assert.Equal(123456d, value);
    }

    [Fact]
    public void TryConvertToDouble_AcceptsGroupSeparators_FrFR()
    {
        var culture = CultureInfo.GetCultureInfo("fr-FR");
        var nfi = culture.NumberFormat;

        var input = $"1{nfi.NumberGroupSeparator}234{nfi.NumberDecimalSeparator}56";

        var result = BindConverter.TryConvertTo<double>(
            input,
            culture,
            out var value);

        Assert.True(result);
        Assert.Equal(1234.56d, value);
    }

    [Theory]
    [InlineData("1.234,56", 1234.56d)]
    [InlineData("1.234,56E2", 123456d)]
    [InlineData("1.234,56e-2", 12.3456d)]
    public void TryConvertToDouble_AcceptsGroupSeparators_DeDE(
        string input,
        double expected)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.GetCultureInfo("de-DE"),
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }
// #nullable enable
//     [Theory]
//     [InlineData("")]
//     [InlineData(null)]
//     public void TryConvertToNullableFloat_AllowsEmptyValue(string? input)
//     {
//         var result = BindConverter.TryConvertTo<float?>(
//             input,
//             CultureInfo.InvariantCulture,
//             out var value);

//         Assert.True(result);
//         Assert.Null(value);
//     }

//     [Theory]
//     [InlineData("")]
//     [InlineData(null)]
//     public void TryConvertToNullableDouble_AllowsEmptyValue(string? input)
//     {
//         var result = BindConverter.TryConvertTo<double?>(
//             input,
//             CultureInfo.InvariantCulture,
//             out var value);

//         Assert.True(result);
//         Assert.Null(value);
//     }

    [Theory]
    [InlineData("1,234.5", 1234.5f)]
    [InlineData("1,234E2", 123400f)]
    [InlineData("1,234.5e-2", 12.345f)]
    public void TryConvertToNullableFloat_AcceptsGroupSeparators_EnUS(
        string input,
        float expected)
    {
        var result = BindConverter.TryConvertTo<float?>(
            input,
            CultureInfo.GetCultureInfo("en-US"),
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,234E2", 123400d)]
    [InlineData("1,234.56E2", 123456d)]
    [InlineData("1,234.56e-2", 12.3456d)]
    [InlineData("-1,234.56E2", -123456d)]
    public void TryConvertToDouble_AcceptsScientificNotationWithGroupSeparators_EnUS(
        string input,
        double expected)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.GetCultureInfo("en-US"),
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,234.56", 1234.56d)]
    [InlineData("1,234E2", 123400d)]
    [InlineData("1,234.56e-2", 12.3456d)]
    public void TryConvertToNullableDouble_AcceptsGroupSeparators_EnUS(
        string input,
        double expected)
    {
        var result = BindConverter.TryConvertTo<double?>(
            input,
            CultureInfo.GetCultureInfo("en-US"),
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
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

    [Theory]
    [InlineData("1e-6", 1e-6)]
    [InlineData("1E-6", 1E-6)]
    [InlineData("2E-06", 2E-06)]
    [InlineData("2.0e-6", 2.0e-6)]
    [InlineData("3.5e10", 3.5e10)]
    [InlineData("-3.2E-04", -3.2E-04)]
    [InlineData("4E8", 4E8)]
    [InlineData("4E+8", 4E8)]
    [InlineData("+4E8", 4E8)]
    public void TryConvertToNullableDouble_AcceptsScientificNotation(
        string input,
        double expected)
    {
        var result = BindConverter.TryConvertTo<double?>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1e-6", 1e-6f)]
    [InlineData("1E-6", 1E-6f)]
    [InlineData("2E-06", 2E-06f)]
    [InlineData("3.5e4", 3.5e4f)]
    [InlineData("-4.25E-03", -4.25E-03f)]
    [InlineData("4E8", 4E8f)]
    [InlineData("4E+8", 4E8f)]
    [InlineData("+4E8", 4E8f)]
    public void TryConvertToNullableFloat_AcceptsScientificNotation(
        string input,
        float expected)
    {
        var result = BindConverter.TryConvertTo<float?>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TryConvertToNullableDouble_AllowsEmptyValue(object input)
    {
        var result = BindConverter.TryConvertTo<double?>(
            (string)input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Null(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TryConvertToNullableFloat_AllowsEmptyValue(object input)
    {
        var result = BindConverter.TryConvertTo<float?>(
            (string)input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Null(value);
    }

    [Theory]
    [InlineData("0", 0d)]
    [InlineData("1", 1d)]
    [InlineData("-1", -1d)]
    [InlineData("123", 123d)]
    [InlineData("123.45", 123.45d)]
    [InlineData("-123.45", -123.45d)]
    [InlineData("0.000002", 0.000002d)]
    public void TryConvertToDouble_AcceptsNormalDecimalValues(
        string input,
        double expected)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("0", 0f)]
    [InlineData("1", 1f)]
    [InlineData("-1", -1f)]
    [InlineData("123", 123f)]
    [InlineData("123.45", 123.45f)]
    [InlineData("-123.45", -123.45f)]
    [InlineData("0.000002", 0.000002f)]
    public void TryConvertToFloat_AcceptsNormalDecimalValues(
        string input,
        float expected)
    {
        var result = BindConverter.TryConvertTo<float>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,234.5", 1234.5f)]
    [InlineData("1,234.56", 1234.56f)]
    [InlineData("12,345.5", 12345.5f)]
    [InlineData("-1,234.25", -1234.25f)]
    public void TryConvertToFloat_AcceptsGroupSeparators_EnUS(
        string input,
        float expected)
    {
        var result = BindConverter.TryConvertTo<float>(
            input,
            CultureInfo.GetCultureInfo("en-US"),
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1,234E2", 123400f)]
    [InlineData("1,234.5E2", 123450f)]
    [InlineData("1,234.5e-2", 12.345f)]
    [InlineData("-1,234.5E2", -123450f)]
    public void TryConvertToFloat_AcceptsScientificNotationWithGroupSeparators_EnUS(
        string input,
        float expected)
    {
        var result = BindConverter.TryConvertTo<float>(
            input,
            CultureInfo.GetCultureInfo("en-US"),
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("1.234,56", 1234.56d)]
    [InlineData("1.234,56E2", 123456d)]
    [InlineData("1.234,56e-2", 12.3456d)]
    [InlineData("-1.234,56E2", -123456d)]
    public void TryConvertToDouble_AcceptsGroupSeparators_DeDE_Extended(
        string input,
        double expected)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.GetCultureInfo("de-DE"),
            out var value);

        Assert.True(result);
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("2E")]
    [InlineData("2E-")]
    [InlineData("2E+")]
    [InlineData("2e")]
    [InlineData("2e-")]
    [InlineData("2e+")]
    [InlineData("E10")]
    [InlineData("e10")]
    [InlineData("1ee6")]
    [InlineData("1e--6")]
    [InlineData("1e++6")]
    public void TryConvertToDouble_RejectsIncompleteOrInvalidScientificNotation(
        string input)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData("2E")]
    [InlineData("2E-")]
    [InlineData("2E+")]
    [InlineData("2e")]
    [InlineData("2e-")]
    [InlineData("2e+")]
    [InlineData("E10")]
    [InlineData("e10")]
    [InlineData("1ee6")]
    [InlineData("1e--6")]
    [InlineData("1e++6")]
    public void TryConvertToFloat_RejectsIncompleteOrInvalidScientificNotation(
        string input)
    {
        var result = BindConverter.TryConvertTo<float>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("hello")]
    [InlineData("1.2.3")]
    [InlineData("--1")]
    [InlineData("++1")]
    [InlineData("1ee6")]
    public void TryConvertToDouble_RejectedClearlyInvalidInput(string input)
    {
        var result = BindConverter.TryConvertTo<double>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.False(result);
        Assert.Equal(default, value);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("hello")]
    [InlineData("1.2.3")]
    [InlineData("--1")]
    [InlineData("++1")]
    [InlineData("1ee6")]
    public void TryConvertToFloat_RejectedClearlyInvalidInput(string input)
    {
        var result = BindConverter.TryConvertTo<float>(
            input,
            CultureInfo.InvariantCulture,
            out var value);

        Assert.False(result);
        Assert.Equal(default, value);
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
}
