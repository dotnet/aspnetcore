// Licensed to the .NET Foundation under one or more agreements.	
// The .NET Foundation licenses this file to you under the MIT license.	

namespace Microsoft.AspNetCore.Components.Routing;

public class QueryParameterValueSupplierTest
{
    private readonly QueryParameterValueSupplier _supplier = new();

    [Fact]
    public void SupportsExpectedValueTypes()
    {
        var query =
            $"BoolVal=true&" +
            $"DateTimeVal=2020-01-02+03:04:05.678-09:00&" +
            $"DecimalVal=-1.234&" +
            $"DoubleVal=-2.345&" +
            $"FloatVal=-3.456&" +
            $"GuidVal=9e7257ad-03aa-42c7-9819-be08b177fef9&" +
            $"IntVal=-54321&" +
            $"LongVal=-99987654321&" +
            $"StringVal=Some+string+%26+more&" +
            $"NullableBoolVal=true&" +
            $"NullableDateTimeVal=2021-01-02+03:04:05.678Z&" +
            $"NullableDecimalVal=1.234&" +
            $"NullableDoubleVal=2.345&" +
            $"NullableFloatVal=3.456&" +
            $"NullableGuidVal=1e7257ad-03aa-42c7-9819-be08b177fef9&" +
            $"NullableIntVal=54321&" +
            $"NullableLongVal=99987654321&";

        ReadQuery(query);

        AssertKeyValuePair<bool>("BoolVal", true);
        AssertKeyValuePair<DateTime>("DateTimeVal", new DateTimeOffset(2020, 1, 2, 3, 4, 5, 678, TimeSpan.FromHours(-9)).LocalDateTime);
        AssertKeyValuePair<decimal>("DecimalVal", -1.234m);
        AssertKeyValuePair<double>("DoubleVal", -2.345);
        AssertKeyValuePair<float>("FloatVal", -3.456f);
        AssertKeyValuePair<Guid>("GuidVal", new Guid("9e7257ad-03aa-42c7-9819-be08b177fef9"));
        AssertKeyValuePair<int>("IntVal", -54321);
        AssertKeyValuePair<long>("LongVal", -99987654321);
        AssertKeyValuePair<bool?>("NullableBoolVal", true);
        AssertKeyValuePair<DateTime?>("NullableDateTimeVal", new DateTime(2021, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc).ToLocalTime());
        AssertKeyValuePair<decimal?>("NullableDecimalVal", 1.234m);
        AssertKeyValuePair<double?>("NullableDoubleVal", 2.345);
        AssertKeyValuePair<float?>("NullableFloatVal", 3.456f);
        AssertKeyValuePair<Guid?>("NullableGuidVal", new Guid("1e7257ad-03aa-42c7-9819-be08b177fef9"));
        AssertKeyValuePair<int?>("NullableIntVal", 54321);
        AssertKeyValuePair<long?>("NullableLongVal", 99987654321);
        AssertKeyValuePair<string>("StringVal", "Some string & more");
    }

    [Theory]
    [InlineData("")]
    [InlineData("?")]
    [InlineData("?unrelated=123")]
    public void SuppliesNullForValueTypesIfNotSpecified(string query)
    {
        ReadQuery(query);

        // Although we could supply default(T) for missing values, there's precedent in the routing	
        // system for supplying null for missing route parameters. The component is then responsible	
        // for interpreting null as a blank value for the parameter, regardless of its type. To keep	
        // the rules aligned, we do the same thing for querystring parameters.	
        AssertKeyValuePair<bool>("BoolVal", null);
        AssertKeyValuePair<DateTime>("DateTimeVal", null);
        AssertKeyValuePair<decimal>("DecimalVal", null);
        AssertKeyValuePair<double>("DoubleVal", null);
        AssertKeyValuePair<float>("FloatVal", null);
        AssertKeyValuePair<Guid>("GuidVal", null);
        AssertKeyValuePair<int>("IntVal", null);
        AssertKeyValuePair<long>("LongVal", null);
        AssertKeyValuePair<bool?>("NullableBoolVal", null);
        AssertKeyValuePair<DateTime?>("NullableDateTimeVal", null);
        AssertKeyValuePair<decimal?>("NullableDecimalVal", null);
        AssertKeyValuePair<double?>("NullableDoubleVal", null);
        AssertKeyValuePair<float?>("NullableFloatVal", null);
        AssertKeyValuePair<Guid?>("NullableGuidVal", null);
        AssertKeyValuePair<int?>("NullableIntVal", null);
        AssertKeyValuePair<long?>("NullableLongVal", null);
        AssertKeyValuePair<string>("StringVal", null);
    }

    [Fact]
    public void SupportsExpectedArrayTypes()
    {
        var query =
            $"BoolVals=true&" +
            $"DateTimeVals=2020-01-02+03:04:05.678Z&" +
            $"DecimalVals=-1.234&" +
            $"DoubleVals=-2.345&" +
            $"FloatVals=-3.456&" +
            $"GuidVals=9e7257ad-03aa-42c7-9819-be08b177fef9&" +
            $"IntVals=-54321&" +
            $"LongVals=-99987654321&" +
            $"StringVals=Some+string+%26+more&" +
            $"NullableBoolVals=true&" +
            $"NullableDateTimeVals=2021-01-02+03:04:05.678Z&" +
            $"NullableDecimalVals=1.234&" +
            $"NullableDoubleVals=2.345&" +
            $"NullableFloatVals=3.456&" +
            $"NullableGuidVals=1e7257ad-03aa-42c7-9819-be08b177fef9&" +
            $"NullableIntVals=54321&" +
            $"NullableLongVals=99987654321&";

        ReadQuery(query);

        AssertKeyValuePair<bool[]>("BoolVals", new[] { true });
        AssertKeyValuePair<DateTime[]>("DateTimeVals", new[] { new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc).ToLocalTime() });
        AssertKeyValuePair<decimal[]>("DecimalVals", new[] { -1.234m });
        AssertKeyValuePair<double[]>("DoubleVals", new[] { -2.345 });
        AssertKeyValuePair<float[]>("FloatVals", new[] { -3.456f });
        AssertKeyValuePair<Guid[]>("GuidVals", new[] { new Guid("9e7257ad-03aa-42c7-9819-be08b177fef9") });
        AssertKeyValuePair<int[]>("IntVals", new[] { -54321 });
        AssertKeyValuePair<long[]>("LongVals", new[] { -99987654321 });
        AssertKeyValuePair<bool?[]>("NullableBoolVals", new[] { true });
        AssertKeyValuePair<DateTime?[]>("NullableDateTimeVals", new[] { new DateTime(2021, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc).ToLocalTime() });
        AssertKeyValuePair<decimal?[]>("NullableDecimalVals", new[] { 1.234m });
        AssertKeyValuePair<double?[]>("NullableDoubleVals", new[] { 2.345 });
        AssertKeyValuePair<float?[]>("NullableFloatVals", new[] { 3.456f });
        AssertKeyValuePair<Guid?[]>("NullableGuidVals", new[] { new Guid("1e7257ad-03aa-42c7-9819-be08b177fef9") });
        AssertKeyValuePair<int?[]>("NullableIntVals", new[] { 54321 });
        AssertKeyValuePair<long?[]>("NullableLongVals", new[] { 99987654321 });
        AssertKeyValuePair<string[]>("StringVals", new[] { "Some string & more" });
    }

    [Theory]
    [InlineData("")]
    [InlineData("?")]
    [InlineData("?unrelated=123")]
    public void SuppliesEmptyArrayForArrayTypesIfNotSpecified(string query)
    {
        ReadQuery(query);

        AssertKeyValuePair<bool[]>("BoolVals", Array.Empty<bool>());
        AssertKeyValuePair<DateTime[]>("DateTimeVals", Array.Empty<DateTime>());
        AssertKeyValuePair<decimal[]>("DecimalVals", Array.Empty<decimal>());
        AssertKeyValuePair<double[]>("DoubleVals", Array.Empty<double>());
        AssertKeyValuePair<float[]>("FloatVals", Array.Empty<float>());
        AssertKeyValuePair<Guid[]>("GuidVals", Array.Empty<Guid>());
        AssertKeyValuePair<int[]>("IntVals", Array.Empty<int>());
        AssertKeyValuePair<long[]>("LongVals", Array.Empty<long>());
        AssertKeyValuePair<bool?[]>("NullableBoolVals", Array.Empty<bool?>());
        AssertKeyValuePair<DateTime?[]>("NullableDateTimeVals", Array.Empty<DateTime?>());
        AssertKeyValuePair<decimal?[]>("NullableDecimalVals", Array.Empty<decimal?>());
        AssertKeyValuePair<double?[]>("NullableDoubleVals", Array.Empty<double?>());
        AssertKeyValuePair<float?[]>("NullableFloatVals", Array.Empty<float?>());
        AssertKeyValuePair<Guid?[]>("NullableGuidVals", Array.Empty<Guid?>());
        AssertKeyValuePair<int?[]>("NullableIntVals", Array.Empty<int?>());
        AssertKeyValuePair<long?[]>("NullableLongVals", Array.Empty<long?>());
        AssertKeyValuePair<string[]>("StringVals", Array.Empty<string>());
    }

    [Theory]
    [InlineData("BoolVal", "abc", typeof(bool))]
    [InlineData("DateTimeVal", "2020-02-31", typeof(DateTime))]
    [InlineData("DecimalVal", "1.2.3", typeof(decimal))]
    [InlineData("DoubleVal", "1x", typeof(double))]
    [InlineData("FloatVal", "1e1000", typeof(float))]
    [InlineData("GuidVal", "123456-789-0", typeof(Guid))]
    [InlineData("IntVal", "5000000000", typeof(int))]
    [InlineData("LongVal", "this+is+a+long+value", typeof(long))]
    [InlineData("NullableBoolVal", "abc", typeof(bool?))]
    [InlineData("NullableDateTimeVal", "2020-02-31", typeof(DateTime?))]
    [InlineData("NullableDecimalVal", "1.2.3", typeof(decimal?))]
    [InlineData("NullableDoubleVal", "1x", typeof(double?))]
    [InlineData("NullableFloatVal", "1e1000", typeof(float?))]
    [InlineData("NullableGuidVal", "123456-789-0", typeof(Guid?))]
    [InlineData("NullableIntVal", "5000000000", typeof(int?))]
    [InlineData("NullableLongVal", "this+is+a+long+value", typeof(long?))]
    public void RejectsUnparseableValues(string key, string value, Type targetType)
    {
        ReadQuery($"?{key}={value}");

        var ex = Assert.Throws<InvalidOperationException>(() => _supplier.GetQueryParameterValue(targetType, key));
        Assert.Equal($"Cannot parse the value '{value.Replace('+', ' ')}' as type '{targetType}' for '{key}'.", ex.Message);
    }

    [Theory]
    [InlineData("BoolVals", "true", "abc", typeof(bool))]
    [InlineData("DateTimeVals", "2020-02-28", "2020-02-31", typeof(DateTime))]
    [InlineData("DecimalVals", "1.23", "1.2.3", typeof(decimal))]
    [InlineData("DoubleVals", "1", "1x", typeof(double))]
    [InlineData("FloatVals", "1000", "1e1000", typeof(float))]
    [InlineData("GuidVals", "9e7257ad-03aa-42c7-9819-be08b177fef9", "123456-789-0", typeof(Guid))]
    [InlineData("IntVals", "5000000", "5000000000", typeof(int))]
    [InlineData("LongVals", "-1234", "this+is+a+long+value", typeof(long))]
    [InlineData("NullableBoolVals", "true", "abc", typeof(bool?))]
    [InlineData("NullableDateTimeVals", "2020-02-28", "2020-02-31", typeof(DateTime?))]
    [InlineData("NullableDecimalVals", "1.23", "1.2.3", typeof(decimal?))]
    [InlineData("NullableDoubleVals", "1", "1x", typeof(double?))]
    [InlineData("NullableFloatVals", "1000", "1e1000", typeof(float?))]
    [InlineData("NullableGuidVals", "9e7257ad-03aa-42c7-9819-be08b177fef9", "123456-789-0", typeof(Guid?))]
    [InlineData("NullableIntVals", "5000000", "5000000000", typeof(int?))]
    [InlineData("NullableLongVals", "-1234", "this+is+a+long+value", typeof(long?))]
    public void RejectsUnparseableArrayEntries(string key, string validValue, string invalidValue, Type targetType)
    {
        ReadQuery($"?{key}={validValue}&{key}={invalidValue}");

        var ex = Assert.Throws<InvalidOperationException>(() => _supplier.GetQueryParameterValue(targetType.MakeArrayType(), key));
        Assert.Equal($"Cannot parse the value '{invalidValue.Replace('+', ' ')}' as type '{targetType}' for '{key}'.", ex.Message);
    }

    [Theory]
    [InlineData("BoolVal", typeof(bool))]
    [InlineData("DateTimeVal", typeof(DateTime))]
    [InlineData("DecimalVal", typeof(decimal))]
    [InlineData("DoubleVal", typeof(double))]
    [InlineData("FloatVal", typeof(float))]
    [InlineData("GuidVal", typeof(Guid))]
    [InlineData("IntVal", typeof(int))]
    [InlineData("LongVal", typeof(long))]
    public void RejectsBlankValuesWhenNotNullable(string key, Type targetType)
    {
        ReadQuery($"?StringVal=somevalue&{key}=");

        var ex = Assert.Throws<InvalidOperationException>(() => _supplier.GetQueryParameterValue(targetType, key));
        Assert.Equal($"Cannot parse the value '' as type '{targetType}' for '{key}'.", ex.Message);
    }

    [Fact]
    public void AcceptsBlankValuesWhenNullable()
    {
        var query =
            $"NullableBoolVal=&" +
            $"NullableDateTimeVal=&" +
            $"NullableDecimalVal=&" +
            $"NullableDoubleVal=&" +
            $"NullableFloatVal=&" +
            $"NullableGuidVal=&" +
            $"NullableIntVal=&" +
            $"NullableLongVal=&";

        ReadQuery(query);

        AssertKeyValuePair<bool?>("NullableBoolVal", null);
        AssertKeyValuePair<DateTime?>("NullableDateTimeVal", null);
        AssertKeyValuePair<decimal?>("NullableDecimalVal", null);
        AssertKeyValuePair<double?>("NullableDoubleVal", null);
        AssertKeyValuePair<float?>("NullableFloatVal", null);
        AssertKeyValuePair<Guid?>("NullableGuidVal", null);
        AssertKeyValuePair<int?>("NullableIntVal", null);
        AssertKeyValuePair<long?>("NullableLongVal", null);
    }

    [Theory]
    [InlineData("")]
    [InlineData("=")]
    public void EmptyStringValuesAreSuppliedAsEmptyString(string queryPart)
    {
        ReadQuery($"?StringVal{queryPart}");

        Assert.Equal(string.Empty, _supplier.GetQueryParameterValue(typeof(string), "StringVal"));
    }

    [Fact]
    public void EmptyStringArrayValuesAreSuppliedAsEmptyStrings()
    {
        var query = $"?StringVals=a&" +
            $"StringVals&" +
            $"StringVals=&" +
            $"StringVals=b";

        ReadQuery(query);

        Assert.Equal(new[] { "a", string.Empty, string.Empty, "b" }, _supplier.GetQueryParameterValue(typeof(string[]), "StringVals"));
    }

    [Theory]
    [InlineData("BoolVals", typeof(bool))]
    [InlineData("DateTimeVals", typeof(DateTime))]
    [InlineData("DecimalVals", typeof(decimal))]
    [InlineData("DoubleVals", typeof(double))]
    [InlineData("FloatVals", typeof(float))]
    [InlineData("GuidVals", typeof(Guid))]
    [InlineData("IntVals", typeof(int))]
    [InlineData("LongVals", typeof(long))]
    public void RejectsBlankArrayEntriesWhenNotNullable(string key, Type targetType)
    {
        ReadQuery($"?StringVal=somevalue&{key}=");

        var ex = Assert.Throws<InvalidOperationException>(
            () => _supplier.GetQueryParameterValue(targetType, key));
        Assert.Equal($"Cannot parse the value '' as type '{targetType}' for '{key}'.", ex.Message);
    }

    [Fact]
    public void AcceptsBlankArrayEntriesWhenNullable()
    {
        var query =
            $"NullableBoolVals=&" +
            $"NullableDateTimeVals=&" +
            $"NullableDecimalVals=&" +
            $"NullableDoubleVals=&" +
            $"NullableFloatVals=&" +
            $"NullableGuidVals=&" +
            $"NullableIntVals=&" +
            $"NullableLongVals=&";

        ReadQuery(query);

        AssertKeyValuePair<bool?[]>("NullableBoolVals", new bool?[] { null });
        AssertKeyValuePair<DateTime?[]>("NullableDateTimeVals", new DateTime?[] { null });
        AssertKeyValuePair<decimal?[]>("NullableDecimalVals", new decimal?[] { null });
        AssertKeyValuePair<double?[]>("NullableDoubleVals", new double?[] { null });
        AssertKeyValuePair<float?[]>("NullableFloatVals", new float?[] { null });
        AssertKeyValuePair<Guid?[]>("NullableGuidVals", new Guid?[] { null });
        AssertKeyValuePair<int?[]>("NullableIntVals", new int?[] { null });
        AssertKeyValuePair<long?[]>("NullableLongVals", new long?[] { null });
    }

    [Fact]
    public void DecodesKeysAndValues()
    {
        var nameThatLooksEncoded = "name+that+looks+%5Bencoded%5D";
        var encodedName = Uri.EscapeDataString(nameThatLooksEncoded);
        var query = $"?{encodedName}=Some+%5Bencoded%5D+value";

        ReadQuery(query);

        AssertKeyValuePair<string>(nameThatLooksEncoded, "Some [encoded] value");
    }

    [Fact]
    public void MatchesKeysCaseInsensitively()
    {
        ReadQuery($"?KEYONE=1&KEYTWO=2");

        AssertKeyValuePair<int>("KeyOne", 1);
        AssertKeyValuePair<int>("KeyTwo", 2);
    }

    [Fact]
    public void MatchesKeysWithNonAsciiChars()
    {
        ReadQuery($"?Имя_моей_собственности=first&خاصية_أخرى=second");

        AssertKeyValuePair<string>("خاصية_أخرى", "second");
        AssertKeyValuePair<string>("Имя_моей_собственности", "first");
    }

    private void ReadQuery(string query)
    {
        _supplier.ReadParametersFromQuery(query.AsMemory());
    }

    private void AssertKeyValuePair<T>(string key, object expectedValue)
    {
        var actualValue = _supplier.GetQueryParameterValue(typeof(T), key);
        Assert.Equal(expectedValue, actualValue);
    }
}
