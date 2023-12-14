// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class Http2ConnectionBenchmark : Http2ConnectionBenchmarkBase
{
    [Params(0)]
    public int ResponseDataLength { get; set; }

    private string _responseData;

    [GlobalSetup]
    public override void GlobalSetup()
    {
        base.GlobalSetup();
        _responseData = new string('!', ResponseDataLength);
    }

    protected override Task ProcessRequest(HttpContext httpContext)
    {
        return ResponseDataLength == 0 ? Task.CompletedTask : httpContext.Response.WriteAsync(_responseData);
    }
}
