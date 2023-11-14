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

    [Params("GET", "POST", "Merge")]
    public string TestHttpMethod { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _destinations.Add("MERGE", 10);
        var lookup = CreateLookup(_destinations);

        _dictionaryJumptable = new HttpMethodDictionaryPolicyJumpTable(lookup, corsPreflightDestinations: null);
        _singleEntryJumptable = new HttpMethodSingleEntryPolicyJumpTable(0, HttpMethods.Get, -1, supportsCorsPreflight: false, -1, 2);
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Method = TestHttpMethod;
    }

    private static HttpMethodDestinationsLookup CreateLookup(Dictionary<string, int> extra)
    {
        var destinations = new List<KeyValuePair<string, int>>
        {
            KeyValuePair.Create(HttpMethods.Connect, 1),
            KeyValuePair.Create(HttpMethods.Delete, 2),
            KeyValuePair.Create(HttpMethods.Head, 3),
            KeyValuePair.Create(HttpMethods.Get, 4),
            KeyValuePair.Create(HttpMethods.Options, 5),
            KeyValuePair.Create(HttpMethods.Patch, 6),
            KeyValuePair.Create(HttpMethods.Put, 7),
            KeyValuePair.Create(HttpMethods.Post, 8),
            KeyValuePair.Create(HttpMethods.Trace, 9)
        };

        foreach (var item in extra)
        {
            destinations.Add(item);
        }

        return new HttpMethodDestinationsLookup(destinations, exitDestination: 0);
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
