// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing.Matching;

public class HttpMethodPolicyJumpTableBenchmark
{
    private PolicyJumpTable _dictionaryJumptable;
    private PolicyJumpTable _singleEntryJumptable;
    private DefaultHttpContext _httpContext;
    private Dictionary<string, int> _destinations = new();
    private Dictionary<string, int> _corsDestinations = new();

    [Params("GET", "POST", "Merge")]
    public string TestHttpMethod { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var knownJumpTable = new KnownHttpMethodsJumpTable()
        {
            ConnectDestination = 1,
            DeleteDestination = 2,
            HeadDestination = 3,
            GetDestination = 4,
            OptionsDestination = 5,
            PatchDestination = 6,
            PutDestination = 7,
            PostDestination = 8,
            TraceDestination = 9
        };
        _destinations.Add("MERGE", 10);

        _dictionaryJumptable = new HttpMethodDictionaryPolicyJumpTable(0, knownJumpTable, _destinations, supportsCorsPreflight: false, -1, knownJumpTable, _corsDestinations);
        _singleEntryJumptable = new HttpMethodSingleEntryPolicyJumpTable(0, HttpMethods.Get, -1, supportsCorsPreflight: false, -1, 2);
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Method = TestHttpMethod;
    }

    [Benchmark]
    public int DictionaryPolicyJumpTable()
    {
        return _dictionaryJumptable.GetDestination(_httpContext);
    }

    [Benchmark]
    public int SingleEntryPolicyJumpTable()
    {
        return _singleEntryJumptable.GetDestination(_httpContext);
    }
}
