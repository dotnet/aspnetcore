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
                $"{nameof(ValidTypes.StringVal)}=Some+string+%26+more&";

            Assert.Collection(GetSuppliedParameters<ValidTypes>(query),
                AssertKeyValuePair(nameof(ValidTypes.BoolVal), true),
                AssertKeyValuePair(nameof(ValidTypes.DateTimeVal), new DateTime(2020, 1, 2, 3, 4, 5, 678, DateTimeKind.Utc)),
                AssertKeyValuePair(nameof(ValidTypes.DecimalVal), -1.234m),
                AssertKeyValuePair(nameof(ValidTypes.DoubleVal), -2.345),
                AssertKeyValuePair(nameof(ValidTypes.FloatVal), -3.456f),
                AssertKeyValuePair(nameof(ValidTypes.GuidVal), new Guid("9e7257ad-03aa-42c7-9819-be08b177fef9")),
                AssertKeyValuePair(nameof(ValidTypes.IntVal), -54321),
                AssertKeyValuePair(nameof(ValidTypes.LongVal), -99987654321),
                AssertKeyValuePair(nameof(ValidTypes.StringVal), "Some string & more"));
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
                Assert.IsType<T>(expectedValue);
                Assert.Equal(expectedValue, pair.value);
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
        }

        private class ValidNullableTypes : ComponentBase
        {
            [Parameter, SupplyParameterFromQuery] public bool? BoolVal { get; set; }
            [Parameter, SupplyParameterFromQuery] public DateTime? DateTimeVal { get; set; }
            [Parameter, SupplyParameterFromQuery] public decimal? DecimalVal { get; set; }
            [Parameter, SupplyParameterFromQuery] public double? DoubleVal { get; set; }
            [Parameter, SupplyParameterFromQuery] public float? FloatVal { get; set; }
            [Parameter, SupplyParameterFromQuery] public Guid? GuidVal { get; set; }
            [Parameter, SupplyParameterFromQuery] public int? IntVal { get; set; }
            [Parameter, SupplyParameterFromQuery] public long? LongVal { get; set; }
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
        }

        private class ValidArrayOfNullableTypes : ComponentBase
        {
            [Parameter, SupplyParameterFromQuery] public bool?[] BoolVals { get; set; }
            [Parameter, SupplyParameterFromQuery] public DateTime?[] DateTimeVals { get; set; }
            [Parameter, SupplyParameterFromQuery] public decimal?[] DecimalVals { get; set; }
            [Parameter, SupplyParameterFromQuery] public double?[] DoubleVals { get; set; }
            [Parameter, SupplyParameterFromQuery] public float?[] FloatVals { get; set; }
            [Parameter, SupplyParameterFromQuery] public Guid?[] GuidVals { get; set; }
            [Parameter, SupplyParameterFromQuery] public int?[] IntVals { get; set; }
            [Parameter, SupplyParameterFromQuery] public long?[] LongVals { get; set; }
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
    }
}
