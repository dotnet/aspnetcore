// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using static Microsoft.AspNetCore.Http.Features.QueryFeature;

namespace Microsoft.AspNetCore.Http
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [CategoriesColumn]
    public class QueryCollectionBenchmarks
    {
        private string _queryString;
        private string _singleValue;
        private string _singleValueWithPlus;

        [IterationSetup]
        public void Setup()
        {
            _queryString = "?key1=value1&key2=value2&key3=value3&key4=&key5=";
            _singleValue = "?key1=value1";
            _singleValueWithPlus = "?key1=value1+value2+value3";
        }

        [Benchmark(Description = "ParseNew")]
        [BenchmarkCategory("QueryString")]
        public void ParseNew()
        {
            _ = QueryFeature.ParseNullableQueryInternal(_queryString);
        }

        [Benchmark(Description = "ParseNew")]
        [BenchmarkCategory("Single")]
        public void ParseNewSingle()
        {
            _ = QueryFeature.ParseNullableQueryInternal(_singleValue);
        }

        [Benchmark(Description = "ParseNew")]
        [BenchmarkCategory("SingleWithPlus")]
        public void ParseNewSingleWithPlus()
        {
            _ = QueryFeature.ParseNullableQueryInternal(_singleValueWithPlus);
        }

        [Benchmark(Description = "QueryHelpersParse")]
        [BenchmarkCategory("QueryString")]
        public void QueryHelpersParse()
        {
            _ = QueryHelpers.ParseNullableQuery(_queryString);
        }

        [Benchmark(Description = "QueryHelpersParse")]
        [BenchmarkCategory("Single")]
        public void QueryHelpersParseSingle()
        {
            _ = QueryHelpers.ParseNullableQuery(_singleValue);
        }

        [Benchmark(Description = "QueryHelpersParse")]
        [BenchmarkCategory("SingleWithPlus")]
        public void QueryHelpersParseSingleWithPlus()
        {
            _ = QueryHelpers.ParseNullableQuery(_singleValueWithPlus);
        }

        [Benchmark]
        [BenchmarkCategory("Constructor")]
        public void Constructor()
        {
            var dict = new KvpAccumulator();
            if (dict.HasValues)
            {
                return;
            }
        }
    }
}
