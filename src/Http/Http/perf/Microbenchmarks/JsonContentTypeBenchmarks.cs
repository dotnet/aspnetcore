// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;

namespace Microsoft.AspNetCore.Http;

public class JsonContentTypeBenchmarks
{
    private HttpRequest _request = null!;

    [Params("application/json", "application/json; charset=utf-8", "application/problem+json", "text/plain")]
    public string ContentType { get; set; } = null!;

    [GlobalSetup]
    public void Setup()
    {
        _request = new DefaultHttpContext().Request;
        _request.ContentType = ContentType;
    }

    [Benchmark]
    public bool HasJsonContentType() => _request.HasJsonContentType();
}
