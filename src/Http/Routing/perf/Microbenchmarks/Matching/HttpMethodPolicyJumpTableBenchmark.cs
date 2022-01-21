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

    [GlobalSetup]
    public void Setup()
    {
        _dictionaryJumptable = new HttpMethodDictionaryPolicyJumpTable(
            0,
            new Dictionary<string, int>
            {
                [HttpMethods.Get] = 1
            },
            -1,
            new Dictionary<string, int>
            {
                [HttpMethods.Get] = 2
            });
        _singleEntryJumptable = new HttpMethodSingleEntryPolicyJumpTable(
            0,
            HttpMethods.Get,
            -1,
            supportsCorsPreflight: true,
            -1,
            2);

        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Method = HttpMethods.Get;
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
