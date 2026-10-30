// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Http.Abstractions.Microbenchmarks;

[MemoryDiagnoser]
public class SetHeaderJoinedBenchmark
{
    private readonly HeaderDictionary _dictionary = new HeaderDictionary();
    private readonly StringValues _singleValue = new StringValues("GET");
    private readonly StringValues _manyValues = new StringValues(new[] { "GET", "POST", "PUT", "DELETE" });
    private readonly StringValues _quotedValues = new StringValues(new[] { "a,b", "c" });
    private readonly string[] _appendValues = new[] { "X-Custom-Header", "X-Other-Header" };

    [Benchmark]
    public void SetSingleValue() => ParsingHelpers.SetHeaderJoined(_dictionary, "Access-Control-Allow-Methods", _singleValue);

    [Benchmark]
    public void SetManyValues() => ParsingHelpers.SetHeaderJoined(_dictionary, "Access-Control-Allow-Methods", _manyValues);

    [Benchmark]
    public void SetValuesNeedingQuotes() => ParsingHelpers.SetHeaderJoined(_dictionary, "X-Values", _quotedValues);

    [Benchmark]
    public void AppendValues()
    {
        _dictionary["Access-Control-Expose-Headers"] = "X-Existing";
        ParsingHelpers.AppendHeaderJoined(_dictionary, "Access-Control-Expose-Headers", _appendValues);
    }
}
