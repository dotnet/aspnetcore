// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.AspNetCore.Components.Endpoints.FormMapping;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Components.Endpoints.Tests.FormMapping;

public class NullableConverterTests
{
    [Fact]
    public void TryConvertValue_ForDateOnlyReturnsTrueWithNullForEmptyString()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var nullableConverter = new NullableConverter<DateOnly>(new ParsableConverter<DateOnly>());
        var reader = new FormDataReader(default, culture, default);

        var returnValue = nullableConverter.TryConvertValue(ref reader, string.Empty, out var result);

        Assert.True(returnValue);
        Assert.Null(result);
    }

    [Fact]
    public void TryConvertValue_ForDateOnlyReturnsTrueWithDateForRealDateValue()
    {
        var date = new DateOnly(2023, 11, 30);
        var culture = CultureInfo.GetCultureInfo("en-US");

        var nullableConverter = new NullableConverter<DateOnly>(new ParsableConverter<DateOnly>());
        var reader = new FormDataReader(default, culture, default);

        var returnValue = nullableConverter.TryConvertValue(ref reader, date.ToString(culture), out var result);

        Assert.True(returnValue);
        Assert.Equal(date, result);
    }

    [Fact]
    public void TryConvertValue_ForDateOnlyReturnsFalseWithNullForBadDateValue()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var nullableConverter = new NullableConverter<DateOnly>(new ParsableConverter<DateOnly>());
        var reader = new FormDataReader(default, culture, default)
        {
            ErrorHandler = (_, __, ___) => { }
        };

        var returnValue = nullableConverter.TryConvertValue(ref reader, "bad date", out var result);

        Assert.False(returnValue);
        Assert.Null(result);
    }

    [Fact]
    public void TryRead_ForDateOnlyReturnsFalseWithNullForNoValue()
    {
        const string prefixName = "field";
        var culture = CultureInfo.GetCultureInfo("en-US");

        var dictionary = new Dictionary<FormKey, StringValues>();
        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(dictionary, culture, buffer);
        reader.PushPrefix(prefixName);

        var nullableConverter = new NullableConverter<DateOnly>(new ParsableConverter<DateOnly>());

        var returnValue = nullableConverter.TryRead(ref reader, typeof(DateOnly?), default, out var result, out var found);

        Assert.False(found);
        Assert.True(returnValue);
        Assert.Null(result);
    }

    [Fact]
    public void TryRead_ForDateOnlyReturnsTrueWithNullForEmptyString()
    {
        const string prefixName = "field";
        var culture = CultureInfo.GetCultureInfo("en-US");

        var dictionary = new Dictionary<FormKey, StringValues>()
        {
            { new FormKey(prefixName.AsMemory()), (StringValues)string.Empty }
        };
        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(dictionary, culture, buffer);
        reader.PushPrefix(prefixName);

        var nullableConverter = new NullableConverter<DateOnly>(new ParsableConverter<DateOnly>());

        var returnValue = nullableConverter.TryRead(ref reader, typeof(DateOnly?), default, out var result, out var found);

        Assert.True(found);
        Assert.True(returnValue);
        Assert.Null(result);
    }

    [Fact]
    public void TryRead_ForDateOnlyReturnsTrueWithDateForRealDateValue()
    {
        const string prefixName = "field";
        var date = new DateOnly(2023, 11, 30);
        var culture = CultureInfo.GetCultureInfo("en-US");

        var dictionary = new Dictionary<FormKey, StringValues>()
        {
            { new FormKey(prefixName.AsMemory()), (StringValues)date.ToString(culture) }
        };
        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(dictionary, culture, buffer);
        reader.PushPrefix(prefixName);

        var nullableConverter = new NullableConverter<DateOnly>(new ParsableConverter<DateOnly>());

        var returnValue = nullableConverter.TryRead(ref reader, typeof(DateOnly?), default, out var result, out var found);

        Assert.True(found);
        Assert.True(returnValue);
        Assert.Equal(date, result);
    }

    [Fact]
    public void TryRead_ForDateOnlyReturnsFalseWithNullForBadDateValue()
    {
        const string prefixName = "field";
        var culture = CultureInfo.GetCultureInfo("en-US");

        var dictionary = new Dictionary<FormKey, StringValues>()
        {
            { new FormKey(prefixName.AsMemory()), (StringValues)"bad date" }
        };
        var buffer = prefixName.ToCharArray().AsMemory();
        var reader = new FormDataReader(dictionary, culture, buffer)
        {
            ErrorHandler = (_, __, ___) => { }
        };
        reader.PushPrefix(prefixName);

        var nullableConverter = new NullableConverter<DateOnly>(new ParsableConverter<DateOnly>());

        var returnValue = nullableConverter.TryRead(ref reader, typeof(DateOnly?), default, out var result, out var found);

        Assert.True(found);
        Assert.False(returnValue);
        Assert.Null(result);
    }

    [Fact]
    public void TryConvertValue_ForCustomParsableStruct_UsesParsableImplementation_ForEmptyString()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var nullableConverter = new NullableConverter<ParsableTestStruct>(new ParsableConverter<ParsableTestStruct>());
        var reader = new FormDataReader(default, culture, default);

        var returnValue = nullableConverter.TryConvertValue(ref reader, string.Empty, out var result);

        Assert.True(returnValue);
        Assert.NotNull(result);
        Assert.True(result.Value.WasEmptyOrNull);
    }

    [Fact]
    public void TryConvertValue_ForCustomParsableStruct_UsesParsableImplementation_ForNull()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var nullableConverter = new NullableConverter<ParsableTestStruct>(new ParsableConverter<ParsableTestStruct>());
        var reader = new FormDataReader(default, culture, default);

        var returnValue = nullableConverter.TryConvertValue(ref reader, null, out var result);

        Assert.True(returnValue);
        Assert.NotNull(result);
        Assert.True(result.Value.WasEmptyOrNull);
    }

    [Fact]
    public void TryConvertValue_ForCustomParsableStruct_UsesParsableImplementation_ForGoodValue()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var nullableConverter = new NullableConverter<ParsableTestStruct>(new ParsableConverter<ParsableTestStruct>());
        var reader = new FormDataReader(default, culture, default)
        {
            ErrorHandler = (_, __, ___) => { }
        };

        var returnValue = nullableConverter.TryConvertValue(ref reader, "good value", out var result);

        Assert.True(returnValue);
        Assert.False(result.Value.WasEmptyOrNull);
    }

    [Fact]
    public void TryConvertValue_ForCustomParsableStruct_UsesParsableImplementation_ForBadValue()
    {
        var culture = CultureInfo.GetCultureInfo("en-US");

        var nullableConverter = new NullableConverter<ParsableTestStruct>(new ParsableConverter<ParsableTestStruct>());
        var reader = new FormDataReader(default, culture, default)
        {
            ErrorHandler = (_, __, ___) => { }
        };

        var returnValue = nullableConverter.TryConvertValue(ref reader, "bad value", out var result);

        Assert.False(returnValue);
    }

    private struct ParsableTestStruct : IParsable<ParsableTestStruct>
    {
        public bool WasEmptyOrNull { get; set; }

        public static ParsableTestStruct Parse(string s, IFormatProvider provider) => throw new NotImplementedException();

        public static bool TryParse([NotNullWhen(true)] string s, IFormatProvider provider, [MaybeNullWhen(false)] out ParsableTestStruct result)
        {
            if (string.IsNullOrEmpty(s))
            {
                result = new ParsableTestStruct { WasEmptyOrNull = true };
                return true;
            }
            else if (s == "good value")
            {
                result = new ParsableTestStruct { WasEmptyOrNull = false };
                return true;
            }
            else
            {
                result = new();
                return false;
            }
        }
    }
}
