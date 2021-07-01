// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components.Rendering;
using Xunit;

namespace Microsoft.AspNetCore.Components.Routing
{
    public class QueryParameterValueSupplierTest
    {
        [Fact]
        public void ComponentWithNoQueryParametersHasNoSupplier()
        {
            Assert.Null(QueryParameterValueSupplier.ForType(typeof(NoQueryParameters)));
        }

        [Fact]
        public void SuppliesParametersOnlyForPropertiesWithMatchingAttributes()
        {
            var query = $"?{nameof(IgnorableProperties.Invalid1)}=a&{nameof(IgnorableProperties.Invalid2)}=b&{nameof(IgnorableProperties.Valid)}=c";
            Assert.Collection(GetSuppliedParameters<IgnorableProperties>(query),
                AssertKeyValuePair(nameof(IgnorableProperties.Valid), "c"));
        }

        [Fact]
        public void SupportsExpectedValueTypes()
        {
            var query =
                $"{nameof(ValidTypes.BoolVal)}=true&" +
                $"{nameof(ValidTypes.DateTimeVal)}=2020-01-02+03:04:05.678Z&" +
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
                AssertKeyValuePair(nameof(ValidTypes.DateTimeVal), new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc)),
                AssertKeyValuePair(nameof(ValidTypes.DecimalVal), -1.234m),
                AssertKeyValuePair(nameof(ValidTypes.DoubleVal), -2.345),
                AssertKeyValuePair(nameof(ValidTypes.FloatVal), -3.456f),
                AssertKeyValuePair(nameof(ValidTypes.GuidVal), new Guid("9e7257ad-03aa-42c7-9819-be08b177fef9")),
                AssertKeyValuePair(nameof(ValidTypes.IntVal), -54321),
                AssertKeyValuePair(nameof(ValidTypes.LongVal), -99987654321),
                AssertKeyValuePair(nameof(ValidTypes.NullableBoolVal), true),
                AssertKeyValuePair(nameof(ValidTypes.NullableDateTimeVal), new DateTime(2021, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc)),
                AssertKeyValuePair(nameof(ValidTypes.NullableDecimalVal), 1.234m),
                AssertKeyValuePair(nameof(ValidTypes.NullableDoubleVal), 2.345),
                AssertKeyValuePair(nameof(ValidTypes.NullableFloatVal), 3.456f),
                AssertKeyValuePair(nameof(ValidTypes.NullableGuidVal), new Guid("1e7257ad-03aa-42c7-9819-be08b177fef9")),
                AssertKeyValuePair(nameof(ValidTypes.NullableIntVal), 54321),
                AssertKeyValuePair(nameof(ValidTypes.NullableLongVal), 99987654321),
                AssertKeyValuePair(nameof(ValidTypes.StringVal), "Some string & more"));
        }

        [Fact]
        public void SuppliesNullForValueTypesIfNotSpecified()
        {
            // Although we could supply default(T) for missing values, there's precedent in the routing
            // system for supplying null for missing route parameters. The component is then responsible
            // for interpreting null as a blank value for the parameter, regardless of its type. To keep
            // the rules aligned, we do the same thing for querystring parameters.
            Assert.Collection(GetSuppliedParameters<ValidTypes>(default),
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
                AssertKeyValuePair(nameof(ValidArrayTypes.DateTimeVals), new[] { new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc) }),
                AssertKeyValuePair(nameof(ValidArrayTypes.DecimalVals), new[] { -1.234m }),
                AssertKeyValuePair(nameof(ValidArrayTypes.DoubleVals), new[] { -2.345 }),
                AssertKeyValuePair(nameof(ValidArrayTypes.FloatVals), new[] { -3.456f }),
                AssertKeyValuePair(nameof(ValidArrayTypes.GuidVals), new[] { new Guid("9e7257ad-03aa-42c7-9819-be08b177fef9") }),
                AssertKeyValuePair(nameof(ValidArrayTypes.IntVals), new[] { -54321 }),
                AssertKeyValuePair(nameof(ValidArrayTypes.LongVals), new[] { -99987654321 }),
                AssertKeyValuePair(nameof(ValidArrayTypes.NullableBoolVals), new[] { true }),
                AssertKeyValuePair(nameof(ValidArrayTypes.NullableDateTimeVals), new[] { new DateTime(2021, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc) }),
                AssertKeyValuePair(nameof(ValidArrayTypes.NullableDecimalVals), new[] { 1.234m }),
                AssertKeyValuePair(nameof(ValidArrayTypes.NullableDoubleVals), new[] { 2.345 }),
                AssertKeyValuePair(nameof(ValidArrayTypes.NullableFloatVals), new[] { 3.456f }),
                AssertKeyValuePair(nameof(ValidArrayTypes.NullableGuidVals), new[] { new Guid("1e7257ad-03aa-42c7-9819-be08b177fef9") }),
                AssertKeyValuePair(nameof(ValidArrayTypes.NullableIntVals), new[] { 54321 }),
                AssertKeyValuePair(nameof(ValidArrayTypes.NullableLongVals), new[] { 99987654321 }),
                AssertKeyValuePair(nameof(ValidArrayTypes.StringVals), new[] { "Some string & more" }));
        }

        [Fact]
        public void SuppliesEmptyArrayForArrayTypesIfNotSpecified()
        {
            Assert.Collection(GetSuppliedParameters<ValidArrayTypes>(default),
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

        private static IEnumerable<(string key, object value)> GetSuppliedParameters<TComponent>(string query) where TComponent : IComponent
        {
            var supplier = QueryParameterValueSupplier.ForType(typeof(TComponent));
            var renderTreeBuilder = new RenderTreeBuilder();
            renderTreeBuilder.OpenComponent<TComponent>(0);
            supplier.RenderParameterAttributes(renderTreeBuilder, query);
            renderTreeBuilder.CloseComponent();

            var frames = renderTreeBuilder.GetFrames();
            return frames.Array
                .Take(frames.Count)
                .Where(f => f.FrameType == RenderTree.RenderTreeFrameType.Attribute)
                .Select(f => (f.AttributeName, f.AttributeValue))
                .OrderBy(f => f.AttributeName); // The order isn't defined, so use alphabetical for tests
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

        private class NoQueryParameters : ComponentBase { }

        private class IgnorableProperties : ComponentBase
        {
            [Parameter] public string Invalid1 { get; set; }
            [SupplyParameterFromQuery] public string Invalid2 { get; set; }
            [Parameter, SupplyParameterFromQuery] public string Valid { get; set; }
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

        // Custom named query param
        // Mapping to multiple destinations
        // Invalid (nonprimitive) types
        // Unparseable values
        // Valid array types
        // Blank single values
        // Blank single nullable values
        // Blank multiple values
        // Blank multiple nullable values
        // Decodes values
        // Doesn't decode keys
        // Matches keys case-insensitively
        // Multiple values supplied for non-array parameter
    }
}
