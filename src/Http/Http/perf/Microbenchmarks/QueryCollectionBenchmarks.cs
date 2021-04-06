// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http
{
    public class QueryCollectionBenchmarks
    {
        private string _queryString;

        [IterationSetup]
        public void Setup()
        {
            _queryString = "?key1=value1&key2=value2&key3=value3&key4=&key5=";
        }

        [Benchmark]
        public void ParseNew()
        {
            var dict = QueryFeature.ParseNullableQueryInternal(_queryString);
        }

        [Benchmark]
        public void Parse()
        {
            var dict = QueryHelpers.ParseNullableQuery(_queryString);
        }
    }
}
