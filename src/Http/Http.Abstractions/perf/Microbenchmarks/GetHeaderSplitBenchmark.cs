// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Abstractions.Microbenchmarks;

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
        if (values.Length != 1)
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void SplitSingleQuotedHeader()
    {
        var values = ParsingHelpers.GetHeaderSplit(_dictionary, "singleValueQuoted");
        if (values.Length != 1)
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void SplitDoubleHeader()
    {
        var values = ParsingHelpers.GetHeaderSplit(_dictionary, "doubleValue");
        if (values.Length != 2)
        {
            throw new Exception();
        }
    }

    [Benchmark]
    public void SplitManyHeaders()
    {
        var values = ParsingHelpers.GetHeaderSplit(_dictionary, "manyValue");
        if (values.Length != 6)
        {
            throw new Exception();
        }
    }
}
