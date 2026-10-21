// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

[MemoryDiagnoser]
public class LazyRequestHeadersBenchmark
{
    private HttpRequestHeaders _headers;
    private int _iteration;

    [Params(false, true)]
    public bool Lazy { get; set; }

    [Params(false, true)]
    public bool Changing { get; set; }

    [Params("Ignore", "Read", "Enumerate", "Duplicate")]
    public string Access { get; set; }

    [Params(1)]
    public int Protocol { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        AppContext.SetSwitch("Microsoft.AspNetCore.Server.Kestrel.Experimental.LazyUserAgent", Lazy);
        _headers = new HttpRequestHeaders();
        Request();
    }

    [Benchmark]
    public int Request()
    {
        _headers.Reset();
        Append(Changing && (_iteration++ & 1) != 0
            ? "Mozilla/5.0 changing-user-agent-B"u8
            : "Mozilla/5.0 changing-user-agent-A"u8);
        if (Access == "Duplicate")
        {
            Append("second-agent"u8);
        }
        _headers.OnHeadersComplete();

        if (Access == "Read" || Access == "Duplicate")
        {
            return ((IHeaderDictionary)_headers).UserAgent.Count;
        }
        if (Access == "Enumerate")
        {
            var length = 0;
            foreach (var header in _headers)
            {
                length += header.Value.ToString().Length;
            }
            return length;
        }
        return _headers.Count;
    }

    private void Append(ReadOnlySpan<byte> value)
    {
        switch (Protocol)
        {
            case 1:
#if LAZY_HEADERS_BASELINE
                _headers.Append("User-Agent"u8, value, checkForNewlineChars: true);
#else
                _headers.Append("User-Agent"u8, value, checkForNewlineChars: true, allowLazyUserAgent: true);
#endif
                break;
            case 2:
                _headers.TryHPackAppend(58, value, checkForNewlineChars: true);
                break;
            case 3:
                _headers.TryQPackAppend(95, value, checkForNewlineChars: true);
                break;
            default:
                throw new InvalidOperationException($"Unsupported protocol: {Protocol}");
        }
    }
}
