// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class RequestHeadersAppendBenchmark
{
    private const int InnerLoopCount = 1024;

    private readonly HttpRequestHeaders _headers = new HttpRequestHeaders();
    private (byte[] Name, byte[] Value)[] _requestHeaders;

    public enum HeaderSet
    {
        // Unknown headers that have a preallocated HeaderNames string, as sent over HTTP/1.1
        InternedUnknown,
        // Same headers lowercased, as sent over HTTP/2 and HTTP/3
        InternedUnknownLowercase,
        // Unknown headers with no preallocated name
        CustomUnknown,
    }

    [Params(HeaderSet.InternedUnknown, HeaderSet.InternedUnknownLowercase, HeaderSet.CustomUnknown)]
    public HeaderSet Headers { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string[] names = Headers switch
        {
            HeaderSet.InternedUnknown => ["DNT", "Keep-Alive", "X-Requested-With", "Request-Id", "Sec-WebSocket-Key", "Sec-WebSocket-Version"],
            HeaderSet.InternedUnknownLowercase => ["dnt", "keep-alive", "x-requested-with", "request-id", "sec-websocket-key", "sec-websocket-version"],
            _ => ["sec-ch-ua", "sec-ch-ua-mobile", "sec-fetch-site", "sec-fetch-mode", "X-Forwarded-For", "X-Forwarded-Proto"],
        };

        _requestHeaders = names.Select(n => (Encoding.ASCII.GetBytes(n), Encoding.ASCII.GetBytes("1"))).ToArray();
    }

    [Benchmark(OperationsPerInvoke = InnerLoopCount)]
    public void AppendUnknownHeaders()
    {
        var headers = _headers;
        var requestHeaders = _requestHeaders;
        for (var i = 0; i < InnerLoopCount; i++)
        {
            headers.Reset();
            foreach (var (name, value) in requestHeaders)
            {
                headers.Append(name, value, checkForNewlineChars: false);
            }
            headers.OnHeadersComplete();
        }
    }
}
