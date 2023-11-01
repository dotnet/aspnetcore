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

    private static string[] AllHttpMethods = [HttpMethods.Get, HttpMethods.Connect, HttpMethods.Delete, HttpMethods.Head, HttpMethods.Options, HttpMethods.Patch, HttpMethods.Put, HttpMethods.Post, HttpMethods.Trace];

    [Params(2, 4, 9)]
    public int DestinationCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        for (int i = 0; i < DestinationCount; i++)
        {
            _destinations.Add(AllHttpMethods[i], i);
            _corsDestinations.Add(AllHttpMethods[i], DestinationCount + i);
        }

        _dictionaryJumptable = new HttpMethodDictionaryPolicyJumpTable(0, _destinations, -1, _corsDestinations);
        _singleEntryJumptable = new HttpMethodSingleEntryPolicyJumpTable(0, HttpMethods.Get, -1, supportsCorsPreflight: true, -1, 2);
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Method = HttpMethods.Get;
    }

    [Benchmark]
    public PolicyJumpTable DictionaryPolicyJumpTableCtor() =>
        new HttpMethodDictionaryPolicyJumpTable(0, _destinations, -1, _corsDestinations);

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
