// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Abstractions.Microbenchmarks
{
    public class GetHeaderSplitBenchmark
    {
        HeaderDictionary _dictionary;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var dict = new Dictionary<string, StringValues>()
            {
                { "singleValue", new StringValues("single") },
                { "singleValueQuoted", new StringValues("\"single\"") },
                { "doubleValue", new StringValues(new [] { "first", "second" }) },
                { "manyValue", new StringValues(new [] { "first", "second", "third", "fourth", "fifth", "sixth" }) }
            };
            _dictionary = new HeaderDictionary(dict);
        }

        [Benchmark]
        public void SplitSingleHeader()
        {
            var values = ParsingHelpers.GetHeaderSplit(_dictionary, "singleValue");
            if (values.Count != 1)
                throw new Exception();
        }

        [Benchmark]
        public void SplitSingleQuotedHeader()
        {
            var values = ParsingHelpers.GetHeaderSplit(_dictionary, "singleValueQuoted");
            if (values.Count != 1)
                throw new Exception();
        }

        [Benchmark]
        public void SplitDoubleHeader()
        {
            var values = ParsingHelpers.GetHeaderSplit(_dictionary, "doubleValue");
            if (values.Count != 2)
                throw new Exception();
        }

        [Benchmark]
        public void SplitManyHeaders()
        {
            var values = ParsingHelpers.GetHeaderSplit(_dictionary, "manyValue");
            if (values.Count != 6)
                throw new Exception();
        }
    }
}
