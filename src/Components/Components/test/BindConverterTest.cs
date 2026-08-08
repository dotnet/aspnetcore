// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.DotNet.RemoteExecutor;

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

    [Theory]
    [InlineData("A", SomeLetters.A)]
    [InlineData("Q", SomeLetters.Q)]
    public void ConvertToEnumDynamicCodeSafe_ParsesDefinedValues(string text, SomeLetters expected)
    {
        var successfullyConverted = BindConverter.ConvertToEnumDynamicCodeSafe<SomeLetters>(text, CultureInfo.InvariantCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ConvertToEnumDynamicCodeSafe_TreatsEmptyAsDefault(string text)
    {
        var successfullyConverted = BindConverter.ConvertToEnumDynamicCodeSafe<SomeLetters>(text, CultureInfo.InvariantCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(default, actual);
    }

    [Theory]
    [InlineData("Z")]
    [InlineData("42")]
    public void ConvertToEnumDynamicCodeSafe_RejectsUndefinedValues(string text)
    {
        var successfullyConverted = BindConverter.ConvertToEnumDynamicCodeSafe<SomeLetters>(text, CultureInfo.InvariantCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Equal(default, actual);
    }

    [Fact]
    public void ConvertToEnumDynamicCodeSafe_MatchesTheDynamicCodePath()
    {
        foreach (var text in new[] { "A", "B", "C", "Q", "Z", "", "3" })
        {
            var expectedSuccess = BindConverter.TryConvertTo<SomeLetters>(text, CultureInfo.InvariantCulture, out var expected);
            var actualSuccess = BindConverter.ConvertToEnumDynamicCodeSafe<SomeLetters>(text, CultureInfo.InvariantCulture, out var actual);

            Assert.Equal(expectedSuccess, actualSuccess);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ConvertToNullableEnumDynamicCodeSafe_ParsesDefinedValues()
    {
        var successfullyConverted = BindConverter.ConvertToNullableEnumDynamicCodeSafe<SomeLetters?>("C", CultureInfo.InvariantCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Equal(SomeLetters.C, actual);
    }

    [Fact]
    public void ConvertToNullableEnumDynamicCodeSafe_TreatsEmptyAsNull()
    {
        var successfullyConverted = BindConverter.ConvertToNullableEnumDynamicCodeSafe<SomeLetters?>("", CultureInfo.InvariantCulture, out var actual);

        Assert.True(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void ConvertToNullableEnumDynamicCodeSafe_RejectsUndefinedValues()
    {
        var successfullyConverted = BindConverter.ConvertToNullableEnumDynamicCodeSafe<SomeLetters?>("Z", CultureInfo.InvariantCulture, out var actual);

        Assert.False(successfullyConverted);
        Assert.Null(actual);
    }

    [Fact]
    public void ConvertToNullableEnumDynamicCodeSafe_MatchesTheDynamicCodePath()
    {
        foreach (var text in new[] { "A", "B", "C", "Q", "Z", "", "3" })
        {
            var expectedSuccess = BindConverter.TryConvertTo<SomeLetters?>(text, CultureInfo.InvariantCulture, out var expected);
            var actualSuccess = BindConverter.ConvertToNullableEnumDynamicCodeSafe<SomeLetters?>(text, CultureInfo.InvariantCulture, out var actual);

            Assert.Equal(expectedSuccess, actualSuccess);
            Assert.Equal(expected, actual);
        }
    }

    [Fact]
    public void ArrayConversionAndFormatting_PreserveExistingSemantics()
    {
        Assert.True(BindConverter.TryConvertTo<int[]>(
            new[] { "1", "2" },
            CultureInfo.InvariantCulture,
            out var numbers));
        Assert.Equal([1, 2], numbers);
        Assert.Equal("[\"1\", \"2\"]", BindConverter.FormatValue(numbers, CultureInfo.InvariantCulture));

        Assert.True(BindConverter.TryConvertTo<int?[]>(
            new[] { "1", "" },
            CultureInfo.InvariantCulture,
            out var nullableNumbers));
        Assert.Equal([1, null], nullableNumbers);

        Assert.False(BindConverter.TryConvertTo<int[]>(
            new[] { "1", "not-a-number" },
            CultureInfo.InvariantCulture,
            out _));
        Assert.False(BindConverter.TryConvertTo<int[]>(
            "not-an-array",
            CultureInfo.InvariantCulture,
            out _));
    }

    [ConditionalFact]
    [RemoteExecutionSupported]
    public void ArrayConversionAndFormatting_WorkWithoutDynamicCode()
    {
        var options = new RemoteInvokeOptions();
        options.RuntimeConfigurationOptions.Add(
            "System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported",
            false.ToString());

        using var remoteHandle = RemoteExecutor.Invoke(static () =>
        {
            Assert.False(RuntimeFeature.IsDynamicCodeSupported);
            TypeDescriptor.RegisterType<Person>();

            Assert.True(BindConverter.TryConvertTo<int[]>(
                new[] { "1", "2" },
                CultureInfo.InvariantCulture,
                out var numbers));
            Assert.Equal([1, 2], numbers);
            Assert.Equal("[\"1\", \"2\"]", BindConverter.FormatValue(numbers, CultureInfo.InvariantCulture));

            Assert.True(BindConverter.TryConvertTo<int?[]>(
                new[] { "1", "" },
                CultureInfo.InvariantCulture,
                out var nullableNumbers));
            Assert.Equal([1, null], nullableNumbers);

            Assert.True(BindConverter.TryConvertTo<SomeLetters[]>(
                new[] { "A", "Q" },
                CultureInfo.InvariantCulture,
                out var letters));
            Assert.Equal([SomeLetters.A, SomeLetters.Q], letters);
            Assert.False(BindConverter.TryConvertTo<SomeLetters[]>(
                new[] { "A", "Z" },
                CultureInfo.InvariantCulture,
                out _));

            Assert.True(BindConverter.TryConvertTo<Person[]>(
                new[] { """{"Name":"Ada","Age":36}""" },
                CultureInfo.InvariantCulture,
                out var people));
            Assert.Equal("Ada", Assert.Single(people).Name);
            Assert.Contains("Ada", Assert.IsType<string>(BindConverter.FormatValue(people, CultureInfo.InvariantCulture)));

            Assert.True(BindConverter.TryConvertTo<int[][]>(
                new[] { new[] { "1", "2" }, new[] { "3" } },
                CultureInfo.InvariantCulture,
                out var nested));
            Assert.Equal([1, 2], nested[0]);
            Assert.Equal([3], nested[1]);
            Assert.NotNull(BindConverter.FormatValue(nested, CultureInfo.InvariantCulture));

            Assert.False(BindConverter.TryConvertTo<int[]>(
                new[] { "invalid" },
                CultureInfo.InvariantCulture,
                out _));
            Assert.False(BindConverter.TryConvertTo<int[]>(
                "not-an-array",
                CultureInfo.InvariantCulture,
                out _));

            Assert.Throws<InvalidOperationException>(() =>
                BindConverter.TryConvertTo<Unconvertible[]>(
                    Array.Empty<string>(),
                    CultureInfo.InvariantCulture,
                    out _));
            Assert.Throws<InvalidOperationException>(() =>
                BindConverter.FormatValue(Array.Empty<Unconvertible>(), CultureInfo.InvariantCulture));
        }, options);
    }

    public enum SomeLetters
    {
        A,
        B,
        C,
        Q,
    }

    private sealed class Unconvertible
    {
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
