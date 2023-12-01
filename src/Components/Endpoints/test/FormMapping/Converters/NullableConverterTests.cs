// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
