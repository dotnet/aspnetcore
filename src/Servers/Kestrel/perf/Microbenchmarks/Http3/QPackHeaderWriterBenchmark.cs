// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class QPackHeaderWriterBenchmark
{
    private readonly byte[] _buffer = new byte[1024 * 64];
    private readonly Http3HeadersEnumerator _enumerator = new Http3HeadersEnumerator();
    private HttpResponseHeaders _responseHeaders;

    [Params(0, 1024)]
    public long ContentLength { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _responseHeaders = new HttpResponseHeaders();
        var headers = (IHeaderDictionary)_responseHeaders;
        headers.ContentType = "application/json";
        headers.ContentLength = ContentLength;
    }

    [Benchmark]
    public bool BeginEncodeHeaders()
    {
        _enumerator.Initialize(_responseHeaders);
        var totalHeaderSize = 0;
        return QPackHeaderWriter.BeginEncodeHeaders(200, _enumerator, _buffer, ref totalHeaderSize, out _);
    }
}
