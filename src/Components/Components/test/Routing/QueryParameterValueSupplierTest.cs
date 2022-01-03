// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Rendering;

namespace Microsoft.AspNetCore.Components.Routing;

public class QueryParameterValueSupplierTest
{
    private class NoQueryParameters : ComponentBase { }

    [Fact]
    public void ComponentWithNoQueryParametersHasNoSupplier()
    {
        Assert.Null(QueryParameterValueSupplier.ForType(typeof(NoQueryParameters)));
    }

    private class IgnorableProperties : ComponentBase
    {
        [Parameter] public string Invalid1 { get; set; }
        [SupplyParameterFromQuery] public string Invalid2 { get; set; }
        [Parameter, SupplyParameterFromQuery] public string Valid { get; set; }
        [Parameter] public object InvalidAndUnsupportedType { get; set; }
    }

    [Fact]
    public void SuppliesParametersOnlyForPropertiesWithMatchingAttributes()
    {
        var query = $"?{nameof(IgnorableProperties.Invalid1)}=a&{nameof(IgnorableProperties.Invalid2)}=b&{nameof(IgnorableProperties.Valid)}=c";
        Assert.Collection(GetSuppliedParameters<IgnorableProperties>(query),
            AssertKeyValuePair(nameof(IgnorableProperties.Valid), "c"));
    }

    private class ValidTypes : ComponentBase
    {
        [Parameter, SupplyParameterFromQuery] public bool BoolVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public DateTime DateTimeVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public decimal DecimalVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public double DoubleVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public float FloatVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public Guid GuidVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public int IntVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public long LongVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public string StringVal { get; set; }

        [Parameter, SupplyParameterFromQuery] public bool? NullableBoolVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public DateTime? NullableDateTimeVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public decimal? NullableDecimalVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public double? NullableDoubleVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public float? NullableFloatVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public Guid? NullableGuidVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public int? NullableIntVal { get; set; }
        [Parameter, SupplyParameterFromQuery] public long? NullableLongVal { get; set; }
    }

    [Fact]
    public void SupportsExpectedValueTypes()
    {
        var query =
            $"{nameof(ValidTypes.BoolVal)}=true&" +
            $"{nameof(ValidTypes.DateTimeVal)}=2020-01-02+03:04:05.678-09:00&" +
            $"{nameof(ValidTypes.DecimalVal)}=-1.234&" +
            $"{nameof(ValidTypes.DoubleVal)}=-2.345&" +
            $"{nameof(ValidTypes.FloatVal)}=-3.456&" +
            $"{nameof(ValidTypes.GuidVal)}=9e7257ad-03aa-42c7-9819-be08b177fef9&" +
            $"{nameof(ValidTypes.IntVal)}=-54321&" +
            $"{nameof(ValidTypes.LongVal)}=-99987654321&" +
            $"{nameof(ValidTypes.StringVal)}=Some+string+%26+more&" +
            $"{nameof(ValidTypes.NullableBoolVal)}=true&" +
            $"{nameof(ValidTypes.NullableDateTimeVal)}=2021-01-02+03:04:05.678Z&" +
            $"{nameof(ValidTypes.NullableDecimalVal)}=1.234&" +
            $"{nameof(ValidTypes.NullableDoubleVal)}=2.345&" +
            $"{nameof(ValidTypes.NullableFloatVal)}=3.456&" +
            $"{nameof(ValidTypes.NullableGuidVal)}=1e7257ad-03aa-42c7-9819-be08b177fef9&" +
            $"{nameof(ValidTypes.NullableIntVal)}=54321&" +
            $"{nameof(ValidTypes.NullableLongVal)}=99987654321&";

        Assert.Collection(GetSuppliedParameters<ValidTypes>(query),
            AssertKeyValuePair(nameof(ValidTypes.BoolVal), true),
            AssertKeyValuePair(nameof(ValidTypes.DateTimeVal), new DateTimeOffset(2020, 1, 2, 3, 4, 5, 678, TimeSpan.FromHours(-9)).LocalDateTime),
            AssertKeyValuePair(nameof(ValidTypes.DecimalVal), -1.234m),
            AssertKeyValuePair(nameof(ValidTypes.DoubleVal), -2.345),
            AssertKeyValuePair(nameof(ValidTypes.FloatVal), -3.456f),
            AssertKeyValuePair(nameof(ValidTypes.GuidVal), new Guid("9e7257ad-03aa-42c7-9819-be08b177fef9")),
            AssertKeyValuePair(nameof(ValidTypes.IntVal), -54321),
            AssertKeyValuePair(nameof(ValidTypes.LongVal), -99987654321),
            AssertKeyValuePair(nameof(ValidTypes.NullableBoolVal), true),
            AssertKeyValuePair(nameof(ValidTypes.NullableDateTimeVal), new DateTime(2021, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc).ToLocalTime()),
            AssertKeyValuePair(nameof(ValidTypes.NullableDecimalVal), 1.234m),
            AssertKeyValuePair(nameof(ValidTypes.NullableDoubleVal), 2.345),
            AssertKeyValuePair(nameof(ValidTypes.NullableFloatVal), 3.456f),
            AssertKeyValuePair(nameof(ValidTypes.NullableGuidVal), new Guid("1e7257ad-03aa-42c7-9819-be08b177fef9")),
            AssertKeyValuePair(nameof(ValidTypes.NullableIntVal), 54321),
            AssertKeyValuePair(nameof(ValidTypes.NullableLongVal), 99987654321),
            AssertKeyValuePair(nameof(ValidTypes.StringVal), "Some string & more"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?")]
    [InlineData("?unrelated=123")]
    public void SuppliesNullForValueTypesIfNotSpecified(string query)
    {
        // Although we could supply default(T) for missing values, there's precedent in the routing
        // system for supplying null for missing route parameters. The component is then responsible
        // for interpreting null as a blank value for the parameter, regardless of its type. To keep
        // the rules aligned, we do the same thing for querystring parameters.
        Assert.Collection(GetSuppliedParameters<ValidTypes>(query),
            AssertKeyValuePair(nameof(ValidTypes.BoolVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.DateTimeVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.DecimalVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.DoubleVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.FloatVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.GuidVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.IntVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.LongVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableBoolVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableDateTimeVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableDecimalVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableDoubleVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableFloatVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableGuidVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableIntVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableLongVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.StringVal), (object)null));
    }

    private class ValidArrayTypes : ComponentBase
    {
        [Parameter, SupplyParameterFromQuery] public bool[] BoolVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public DateTime[] DateTimeVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public decimal[] DecimalVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public double[] DoubleVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public float[] FloatVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public Guid[] GuidVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public int[] IntVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public long[] LongVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public string[] StringVals { get; set; }

        [Parameter, SupplyParameterFromQuery] public bool?[] NullableBoolVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public DateTime?[] NullableDateTimeVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public decimal?[] NullableDecimalVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public double?[] NullableDoubleVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public float?[] NullableFloatVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public Guid?[] NullableGuidVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public int?[] NullableIntVals { get; set; }
        [Parameter, SupplyParameterFromQuery] public long?[] NullableLongVals { get; set; }
    }

    [Fact]
    public void SupportsExpectedArrayTypes()
    {
        var query =
            $"{nameof(ValidArrayTypes.BoolVals)}=true&" +
            $"{nameof(ValidArrayTypes.DateTimeVals)}=2020-01-02+03:04:05.678Z&" +
            $"{nameof(ValidArrayTypes.DecimalVals)}=-1.234&" +
            $"{nameof(ValidArrayTypes.DoubleVals)}=-2.345&" +
            $"{nameof(ValidArrayTypes.FloatVals)}=-3.456&" +
            $"{nameof(ValidArrayTypes.GuidVals)}=9e7257ad-03aa-42c7-9819-be08b177fef9&" +
            $"{nameof(ValidArrayTypes.IntVals)}=-54321&" +
            $"{nameof(ValidArrayTypes.LongVals)}=-99987654321&" +
            $"{nameof(ValidArrayTypes.StringVals)}=Some+string+%26+more&" +
            $"{nameof(ValidArrayTypes.NullableBoolVals)}=true&" +
            $"{nameof(ValidArrayTypes.NullableDateTimeVals)}=2021-01-02+03:04:05.678Z&" +
            $"{nameof(ValidArrayTypes.NullableDecimalVals)}=1.234&" +
            $"{nameof(ValidArrayTypes.NullableDoubleVals)}=2.345&" +
            $"{nameof(ValidArrayTypes.NullableFloatVals)}=3.456&" +
            $"{nameof(ValidArrayTypes.NullableGuidVals)}=1e7257ad-03aa-42c7-9819-be08b177fef9&" +
            $"{nameof(ValidArrayTypes.NullableIntVals)}=54321&" +
            $"{nameof(ValidArrayTypes.NullableLongVals)}=99987654321&";

        Assert.Collection(GetSuppliedParameters<ValidArrayTypes>(query),
            AssertKeyValuePair(nameof(ValidArrayTypes.BoolVals), new[] { true }),
            AssertKeyValuePair(nameof(ValidArrayTypes.DateTimeVals), new[] { new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc).ToLocalTime() }),
            AssertKeyValuePair(nameof(ValidArrayTypes.DecimalVals), new[] { -1.234m }),
            AssertKeyValuePair(nameof(ValidArrayTypes.DoubleVals), new[] { -2.345 }),
            AssertKeyValuePair(nameof(ValidArrayTypes.FloatVals), new[] { -3.456f }),
            AssertKeyValuePair(nameof(ValidArrayTypes.GuidVals), new[] { new Guid("9e7257ad-03aa-42c7-9819-be08b177fef9") }),
            AssertKeyValuePair(nameof(ValidArrayTypes.IntVals), new[] { -54321 }),
            AssertKeyValuePair(nameof(ValidArrayTypes.LongVals), new[] { -99987654321 }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableBoolVals), new[] { true }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDateTimeVals), new[] { new DateTime(2021, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc).ToLocalTime() }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDecimalVals), new[] { 1.234m }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDoubleVals), new[] { 2.345 }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableFloatVals), new[] { 3.456f }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableGuidVals), new[] { new Guid("1e7257ad-03aa-42c7-9819-be08b177fef9") }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableIntVals), new[] { 54321 }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableLongVals), new[] { 99987654321 }),
            AssertKeyValuePair(nameof(ValidArrayTypes.StringVals), new[] { "Some string & more" }));
    }

    [Theory]
    [InlineData("")]
    [InlineData("?")]
    [InlineData("?unrelated=123")]
    public void SuppliesEmptyArrayForArrayTypesIfNotSpecified(string query)
    {
        Assert.Collection(GetSuppliedParameters<ValidArrayTypes>(query),
            AssertKeyValuePair(nameof(ValidArrayTypes.BoolVals), Array.Empty<bool>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.DateTimeVals), Array.Empty<DateTime>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.DecimalVals), Array.Empty<decimal>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.DoubleVals), Array.Empty<double>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.FloatVals), Array.Empty<float>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.GuidVals), Array.Empty<Guid>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.IntVals), Array.Empty<int>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.LongVals), Array.Empty<long>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableBoolVals), Array.Empty<bool?>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDateTimeVals), Array.Empty<DateTime?>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDecimalVals), Array.Empty<decimal?>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDoubleVals), Array.Empty<double?>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableFloatVals), Array.Empty<float?>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableGuidVals), Array.Empty<Guid?>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableIntVals), Array.Empty<int?>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableLongVals), Array.Empty<long?>()),
            AssertKeyValuePair(nameof(ValidArrayTypes.StringVals), Array.Empty<string>()));
    }

    class OverrideParameterName : ComponentBase
    {
        [Parameter, SupplyParameterFromQuery(Name = "anothername1")] public string Value1 { get; set; }
        [Parameter, SupplyParameterFromQuery(Name = "anothername2")] public string Value2 { get; set; }
    }

    [Fact]
    public void CanOverrideParameterName()
    {
        var query = $"anothername1=Some+value+1&Value2=Some+value+2";
        Assert.Collection(GetSuppliedParameters<OverrideParameterName>(query),
            // Because we specified the mapped name, we receive the value
            AssertKeyValuePair(nameof(OverrideParameterName.Value1), "Some value 1"),
            // If we specify the component parameter name directly, we do not receive the value
            AssertKeyValuePair(nameof(OverrideParameterName.Value2), (object)null));
    }

    class MapSingleQueryParameterToMultipleProperties : ComponentBase
    {
        [Parameter, SupplyParameterFromQuery(Name = "a")] public int ValueAsInt { get; set; }
        [Parameter, SupplyParameterFromQuery(Name = "b")] public DateTime ValueAsDateTime { get; set; }
        [Parameter, SupplyParameterFromQuery(Name = "A")] public long ValueAsLong { get; set; }
    }

    [Fact]
    public void CannotMapSingleQueryParameterToMultipleProperties()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => QueryParameterValueSupplier.ForType(typeof(MapSingleQueryParameterToMultipleProperties)));
        Assert.Contains("declares more than one mapping for the query parameter 'a'.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    class UnsupportedType : ComponentBase
    {
        [Parameter, SupplyParameterFromQuery] public int IntValid { get; set; }
        [Parameter, SupplyParameterFromQuery] public object ObjectValue { get; set; }
    }

    [Fact]
    public void RejectsUnsupportedType()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => QueryParameterValueSupplier.ForType(typeof(UnsupportedType)));
        Assert.Equal("Querystring values cannot be parsed as type 'System.Object'.", ex.Message);
    }

    [Theory]
    [InlineData(nameof(ValidTypes.BoolVal), "abc", typeof(bool))]
    [InlineData(nameof(ValidTypes.DateTimeVal), "2020-02-31", typeof(DateTime))]
    [InlineData(nameof(ValidTypes.DecimalVal), "1.2.3", typeof(decimal))]
    [InlineData(nameof(ValidTypes.DoubleVal), "1x", typeof(double))]
    [InlineData(nameof(ValidTypes.FloatVal), "1e1000", typeof(float))]
    [InlineData(nameof(ValidTypes.GuidVal), "123456-789-0", typeof(Guid))]
    [InlineData(nameof(ValidTypes.IntVal), "5000000000", typeof(int))]
    [InlineData(nameof(ValidTypes.LongVal), "this+is+a+long+value", typeof(long))]
    [InlineData(nameof(ValidTypes.NullableBoolVal), "abc", typeof(bool?))]
    [InlineData(nameof(ValidTypes.NullableDateTimeVal), "2020-02-31", typeof(DateTime?))]
    [InlineData(nameof(ValidTypes.NullableDecimalVal), "1.2.3", typeof(decimal?))]
    [InlineData(nameof(ValidTypes.NullableDoubleVal), "1x", typeof(double?))]
    [InlineData(nameof(ValidTypes.NullableFloatVal), "1e1000", typeof(float?))]
    [InlineData(nameof(ValidTypes.NullableGuidVal), "123456-789-0", typeof(Guid?))]
    [InlineData(nameof(ValidTypes.NullableIntVal), "5000000000", typeof(int?))]
    [InlineData(nameof(ValidTypes.NullableLongVal), "this+is+a+long+value", typeof(long?))]
    public void RejectsUnparseableValues(string key, string value, Type targetType)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => GetSuppliedParameters<ValidTypes>($"?{key}={value}"));
        Assert.Equal($"Cannot parse the value '{value.Replace('+', ' ')}' as type '{targetType}' for '{key}'.", ex.Message);
    }

    [Theory]
    [InlineData(nameof(ValidArrayTypes.BoolVals), "true", "abc", typeof(bool))]
    [InlineData(nameof(ValidArrayTypes.DateTimeVals), "2020-02-28", "2020-02-31", typeof(DateTime))]
    [InlineData(nameof(ValidArrayTypes.DecimalVals), "1.23", "1.2.3", typeof(decimal))]
    [InlineData(nameof(ValidArrayTypes.DoubleVals), "1", "1x", typeof(double))]
    [InlineData(nameof(ValidArrayTypes.FloatVals), "1000", "1e1000", typeof(float))]
    [InlineData(nameof(ValidArrayTypes.GuidVals), "9e7257ad-03aa-42c7-9819-be08b177fef9", "123456-789-0", typeof(Guid))]
    [InlineData(nameof(ValidArrayTypes.IntVals), "5000000", "5000000000", typeof(int))]
    [InlineData(nameof(ValidArrayTypes.LongVals), "-1234", "this+is+a+long+value", typeof(long))]
    [InlineData(nameof(ValidArrayTypes.NullableBoolVals), "true", "abc", typeof(bool?))]
    [InlineData(nameof(ValidArrayTypes.NullableDateTimeVals), "2020-02-28", "2020-02-31", typeof(DateTime?))]
    [InlineData(nameof(ValidArrayTypes.NullableDecimalVals), "1.23", "1.2.3", typeof(decimal?))]
    [InlineData(nameof(ValidArrayTypes.NullableDoubleVals), "1", "1x", typeof(double?))]
    [InlineData(nameof(ValidArrayTypes.NullableFloatVals), "1000", "1e1000", typeof(float?))]
    [InlineData(nameof(ValidArrayTypes.NullableGuidVals), "9e7257ad-03aa-42c7-9819-be08b177fef9", "123456-789-0", typeof(Guid?))]
    [InlineData(nameof(ValidArrayTypes.NullableIntVals), "5000000", "5000000000", typeof(int?))]
    [InlineData(nameof(ValidArrayTypes.NullableLongVals), "-1234", "this+is+a+long+value", typeof(long?))]
    public void RejectsUnparseableArrayEntries(string key, string validValue, string invalidValue, Type targetType)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => GetSuppliedParameters<ValidArrayTypes>($"?{key}={validValue}&{key}={invalidValue}"));
        Assert.Equal($"Cannot parse the value '{invalidValue.Replace('+', ' ')}' as type '{targetType}' for '{key}'.", ex.Message);
    }

    [Theory]
    [InlineData(nameof(ValidTypes.BoolVal), typeof(bool))]
    [InlineData(nameof(ValidTypes.DateTimeVal), typeof(DateTime))]
    [InlineData(nameof(ValidTypes.DecimalVal), typeof(decimal))]
    [InlineData(nameof(ValidTypes.DoubleVal), typeof(double))]
    [InlineData(nameof(ValidTypes.FloatVal), typeof(float))]
    [InlineData(nameof(ValidTypes.GuidVal), typeof(Guid))]
    [InlineData(nameof(ValidTypes.IntVal), typeof(int))]
    [InlineData(nameof(ValidTypes.LongVal), typeof(long))]
    public void RejectsBlankValuesWhenNotNullable(string key, Type targetType)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => GetSuppliedParameters<ValidTypes>($"?{nameof(ValidTypes.StringVal)}=somevalue&{key}="));
        Assert.Equal($"Cannot parse the value '' as type '{targetType}' for '{key}'.", ex.Message);
    }

    [Fact]
    public void AcceptsBlankValuesWhenNullable()
    {
        var query =
            $"{nameof(ValidTypes.NullableBoolVal)}=&" +
            $"{nameof(ValidTypes.NullableDateTimeVal)}=&" +
            $"{nameof(ValidTypes.NullableDecimalVal)}=&" +
            $"{nameof(ValidTypes.NullableDoubleVal)}=&" +
            $"{nameof(ValidTypes.NullableFloatVal)}=&" +
            $"{nameof(ValidTypes.NullableGuidVal)}=&" +
            $"{nameof(ValidTypes.NullableIntVal)}=&" +
            $"{nameof(ValidTypes.NullableLongVal)}=&";
        Assert.Collection(GetSuppliedParameters<ValidTypes>(query).Where(pair => pair.key.StartsWith("Nullable", StringComparison.Ordinal)),
            AssertKeyValuePair(nameof(ValidTypes.NullableBoolVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableDateTimeVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableDecimalVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableDoubleVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableFloatVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableGuidVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableIntVal), (object)null),
            AssertKeyValuePair(nameof(ValidTypes.NullableLongVal), (object)null));
    }

    [Theory]
    [InlineData("")]
    [InlineData("=")]
    public void EmptyStringValuesAreSuppliedAsEmptyString(string queryPart)
    {
        var query = $"?{nameof(ValidTypes.StringVal)}{queryPart}";
        var suppliedParameters = GetSuppliedParameters<ValidTypes>(query).ToDictionary(x => x.key, x => x.value);
        Assert.Equal(string.Empty, suppliedParameters[nameof(ValidTypes.StringVal)]);
    }

    [Fact]
    public void EmptyStringArrayValuesAreSuppliedAsEmptyStrings()
    {
        var query = $"?{nameof(ValidArrayTypes.StringVals)}=a&" +
            $"{nameof(ValidArrayTypes.StringVals)}&" +
            $"{nameof(ValidArrayTypes.StringVals)}=&" +
            $"{nameof(ValidArrayTypes.StringVals)}=b";
        var suppliedParameters = GetSuppliedParameters<ValidArrayTypes>(query).ToDictionary(x => x.key, x => x.value);
        Assert.Equal(new[] { "a", string.Empty, string.Empty, "b" }, suppliedParameters[nameof(ValidArrayTypes.StringVals)]);
    }

    [Theory]
    [InlineData(nameof(ValidArrayTypes.BoolVals), typeof(bool))]
    [InlineData(nameof(ValidArrayTypes.DateTimeVals), typeof(DateTime))]
    [InlineData(nameof(ValidArrayTypes.DecimalVals), typeof(decimal))]
    [InlineData(nameof(ValidArrayTypes.DoubleVals), typeof(double))]
    [InlineData(nameof(ValidArrayTypes.FloatVals), typeof(float))]
    [InlineData(nameof(ValidArrayTypes.GuidVals), typeof(Guid))]
    [InlineData(nameof(ValidArrayTypes.IntVals), typeof(int))]
    [InlineData(nameof(ValidArrayTypes.LongVals), typeof(long))]
    public void RejectsBlankArrayEntriesWhenNotNullable(string key, Type targetType)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => GetSuppliedParameters<ValidArrayTypes>($"?{nameof(ValidTypes.StringVal)}=somevalue&{key}="));
        Assert.Equal($"Cannot parse the value '' as type '{targetType}' for '{key}'.", ex.Message);
    }

    [Fact]
    public void AcceptsBlankArrayEntriesWhenNullable()
    {
        var query =
            $"{nameof(ValidArrayTypes.NullableBoolVals)}=&" +
            $"{nameof(ValidArrayTypes.NullableDateTimeVals)}=&" +
            $"{nameof(ValidArrayTypes.NullableDecimalVals)}=&" +
            $"{nameof(ValidArrayTypes.NullableDoubleVals)}=&" +
            $"{nameof(ValidArrayTypes.NullableFloatVals)}=&" +
            $"{nameof(ValidArrayTypes.NullableGuidVals)}=&" +
            $"{nameof(ValidArrayTypes.NullableIntVals)}=&" +
            $"{nameof(ValidArrayTypes.NullableLongVals)}=&";
        Assert.Collection(GetSuppliedParameters<ValidArrayTypes>(query).Where(pair => pair.key.StartsWith("Nullable", StringComparison.Ordinal)),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableBoolVals), new bool?[] { null }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDateTimeVals), new DateTime?[] { null }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDecimalVals), new decimal?[] { null }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableDoubleVals), new double?[] { null }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableFloatVals), new float?[] { null }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableGuidVals), new Guid?[] { null }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableIntVals), new int?[] { null }),
            AssertKeyValuePair(nameof(ValidArrayTypes.NullableLongVals), new long?[] { null }));
    }

    private class SpecialQueryParameterName : ComponentBase
    {
        public const string NameThatLooksEncoded = "name+that+looks+%5Bencoded%5D";
        [Parameter, SupplyParameterFromQuery(Name = NameThatLooksEncoded)] public string Key { get; set; }
    }

    [Fact]
    public void DecodesKeysAndValues()
    {
        var encodedName = Uri.EscapeDataString(SpecialQueryParameterName.NameThatLooksEncoded);
        var query = $"?{encodedName}=Some+%5Bencoded%5D+value";
        Assert.Collection(GetSuppliedParameters<SpecialQueryParameterName>(query),
            AssertKeyValuePair(nameof(SpecialQueryParameterName.Key), "Some [encoded] value"));
    }

    private class KeyCaseMatching : ComponentBase
    {
        [Parameter, SupplyParameterFromQuery] public int KeyOne { get; set; }
        [Parameter, SupplyParameterFromQuery(Name = "keytwo")] public int KeyTwo { get; set; }
    }

    [Fact]
    public void MatchesKeysCaseInsensitively()
    {
        var query = $"?KEYONE=1&KEYTWO=2";
        Assert.Collection(GetSuppliedParameters<KeyCaseMatching>(query),
            AssertKeyValuePair(nameof(KeyCaseMatching.KeyOne), 1),
            AssertKeyValuePair(nameof(KeyCaseMatching.KeyTwo), 2));
    }

    private class KeysWithNonAsciiChars : ComponentBase
    {
        [Parameter, SupplyParameterFromQuery] public string Имя_моей_собственности { get; set; }
        [Parameter, SupplyParameterFromQuery(Name = "خاصية_أخرى")] public string AnotherProperty { get; set; }
    }

    [Fact]
    public void MatchesKeysWithNonAsciiChars()
    {
        var query = $"?{nameof(KeysWithNonAsciiChars.Имя_моей_собственности)}=first&خاصية_أخرى=second";
        var result = GetSuppliedParameters<KeysWithNonAsciiChars>(query);
        Assert.Collection(result,
            AssertKeyValuePair(nameof(KeysWithNonAsciiChars.AnotherProperty), "second"),
            AssertKeyValuePair(nameof(KeysWithNonAsciiChars.Имя_моей_собственности), "first"));
    }

    private class SingleValueOverwriting : ComponentBase
    {
        [Parameter, SupplyParameterFromQuery] public int Age { get; set; }
        [Parameter, SupplyParameterFromQuery] public int? Id { get; set; }
        [Parameter, SupplyParameterFromQuery] public string Name { get; set; }
    }

    [Fact]
    public void ForNonArrayValuesOnlyOneValueIsSupplied()
    {
        // For simplicity and speed, the value assignment logic doesn't check if the a single-valued destination is
        // already populated, and just overwrites in a left-to-right manner. For nullable values it's possible to
        // overwrite a value with null, or a string with empty.
        Assert.Collection(GetSuppliedParameters<SingleValueOverwriting>($"?age=123&age=456&age=789&id=1&id&name=Bobbins&name"),
            AssertKeyValuePair(nameof(SingleValueOverwriting.Age), 789),
            AssertKeyValuePair(nameof(SingleValueOverwriting.Id), (int?)null),
            AssertKeyValuePair(nameof(SingleValueOverwriting.Name), string.Empty));
    }

    private static IEnumerable<(string key, object value)> GetSuppliedParameters<TComponent>(string query) where TComponent : IComponent
    {
        var supplier = QueryParameterValueSupplier.ForType(typeof(TComponent));
        using var builder = new RenderTreeBuilder();
        builder.OpenComponent<TComponent>(0);
        supplier.RenderParametersFromQueryString(builder, query.AsMemory());
        builder.CloseComponent();

        var frames = builder.GetFrames();
        return frames.Array.Take(frames.Count)
            .Where(frame => frame.FrameType == RenderTree.RenderTreeFrameType.Attribute)
            .Select(frame => (frame.AttributeName, frame.AttributeValue))
            .OrderBy(pair => pair.AttributeName) // The order isn't defined, so use alphabetical for tests
            .ToList();
    }

    private Action<(string key, object value)> AssertKeyValuePair<T>(string expectedKey, T expectedValue)
    {
        return pair =>
        {
            Assert.Equal(expectedKey, pair.key);
            if (expectedValue is null)
            {
                Assert.Null(pair.value);
            }
            else
            {
                Assert.IsType<T>(expectedValue);
                Assert.Equal(expectedValue, pair.value);
            }
        };
    }
}
