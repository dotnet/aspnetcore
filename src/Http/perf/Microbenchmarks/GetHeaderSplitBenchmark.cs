// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Extensions.Primitives;
using BenchmarkDotNet.Configs;

namespace Microsoft.AspNetCore.Http.Abstractions.Microbenchmarks
{
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
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

        [BenchmarkCategory("Single"), Benchmark(Baseline = true)]
        public void SplitSingleHeaderOriginal()
        {
            var values = ParsingHelpers.GetHeaderSplitOriginal(_dictionary, "singleValue");
            if (values.Count != 1)
                throw new Exception();
        }

        [BenchmarkCategory("Single"), Benchmark]
        public void SplitSingleHeader()
        {
            var values = ParsingHelpers.GetHeaderSplit(_dictionary, "singleValue");
            if (values.Count != 1)
                throw new Exception();
        }

        [BenchmarkCategory("Single"), Benchmark]
        public void SplitSingleHeaderExperimental()
        {
            var values = ParsingHelpers.GetHeaderSplitExperimental(_dictionary, "singleValue");
            if (values.Count != 1)
                throw new Exception();
        }

        [BenchmarkCategory("SingleQuoted"), Benchmark(Baseline = true)]
        public void SplitSingleQuotedHeaderOriginal()
        {
            var values = ParsingHelpers.GetHeaderSplitOriginal(_dictionary, "singleValueQuoted");
            if (values.Count != 1)
                throw new Exception();
        }

        [BenchmarkCategory("SingleQuoted"), Benchmark]
        public void SplitSingleQuotedHeader()
        {
            var values = ParsingHelpers.GetHeaderSplit(_dictionary, "singleValueQuoted");
            if (values.Count != 1)
                throw new Exception();
        }

        [BenchmarkCategory("SingleQuoted"), Benchmark]
        public void SplitSingleQuotedHeaderExperimental()
        {
            var values = ParsingHelpers.GetHeaderSplitExperimental(_dictionary, "singleValueQuoted");
            if (values.Count != 1)
                throw new Exception();
        }

        // [BenchmarkCategory("Double"), Benchmark(Baseline = true)]
        // public void SplitDoubleHeaderOriginal()
        // {
        //     var values = ParsingHelpers.GetHeaderSplitOriginal(_dictionary, "doubleValue");
        //     if (values.Count != 2)
        //         throw new Exception();
        // }

        // [BenchmarkCategory("Double"), Benchmark]
        // public void SplitDoubleHeader()
        // {
        //     var values = ParsingHelpers.GetHeaderSplit(_dictionary, "doubleValue");
        //     if (values.Count != 2)
        //         throw new Exception();
        // }

        // [BenchmarkCategory("Many"), Benchmark(Baseline = true)]
        // public void SplitManyHeaderOriginal()
        // {
        //     var values = ParsingHelpers.GetHeaderSplitOriginal(_dictionary, "manyValue");
        //     if (values.Count != 6)
        //         throw new Exception();
        // }

        // [BenchmarkCategory("Many"), Benchmark]
        // public void SplitManyHeader()
        // {
        //     var values = ParsingHelpers.GetHeaderSplit(_dictionary, "manyValue");
        //     if (values.Count != 6)
        //         throw new Exception();
        // }
    }
}
