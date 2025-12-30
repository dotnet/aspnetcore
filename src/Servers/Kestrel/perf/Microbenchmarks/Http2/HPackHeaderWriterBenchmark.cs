// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http.HPack;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class HPackHeaderWriterBenchmark
{
    private Http2HeadersEnumerator _http2HeadersEnumerator;
    private DynamicHPackEncoder _hpackEncoder;
    private HttpResponseHeaders _knownResponseHeaders;
    private HttpResponseHeaders _unknownResponseHeaders;
    private byte[] _buffer;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _http2HeadersEnumerator = new Http2HeadersEnumerator();
        _hpackEncoder = new DynamicHPackEncoder();
        _buffer = new byte[1024 * 1024];

        _knownResponseHeaders = new HttpResponseHeaders();

        var knownHeaders = (IHeaderDictionary)_knownResponseHeaders;
        knownHeaders.Server = "Kestrel";
        knownHeaders.ContentType = "application/json";
        knownHeaders.Date = "Date!";
        knownHeaders.ContentLength = 0;
        knownHeaders.AcceptRanges = "Ranges!";
        knownHeaders.TransferEncoding = "Encoding!";
        knownHeaders.Via = "Via!";
        knownHeaders.Vary = "Vary!";
        knownHeaders.WWWAuthenticate = "Authenticate!";
        knownHeaders.LastModified = "Modified!";
        knownHeaders.Expires = "Expires!";
        knownHeaders.Age = "Age!";

        _unknownResponseHeaders = new HttpResponseHeaders();
        for (var i = 0; i < 10; i++)
        {
            _unknownResponseHeaders.Append("Unknown" + i, "Value" + i);
        }
    }

    [Benchmark]
    public void BeginEncodeHeaders_KnownHeaders()
    {
        _http2HeadersEnumerator.Initialize(_knownResponseHeaders);
        HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, _http2HeadersEnumerator, _buffer, out _);
    }

    [Benchmark]
    public void BeginEncodeHeaders_KnownHeaders_CustomEncoding()
    {
        _knownResponseHeaders.EncodingSelector = _ => Encoding.UTF8;
        _http2HeadersEnumerator.Initialize(_knownResponseHeaders);
        HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, _http2HeadersEnumerator, _buffer, out _);
    }

    [Benchmark]
    public void BeginEncodeHeaders_UnknownHeaders()
    {
        _http2HeadersEnumerator.Initialize(_unknownResponseHeaders);
        HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, _http2HeadersEnumerator, _buffer, out _);
    }

    [Benchmark]
    public void BeginEncodeHeaders_UnknownHeaders_CustomEncoding()
    {
        _knownResponseHeaders.EncodingSelector = _ => Encoding.UTF8;
        _http2HeadersEnumerator.Initialize(_unknownResponseHeaders);
        HPackHeaderWriter.BeginEncodeHeaders(_hpackEncoder, _http2HeadersEnumerator, _buffer, out _);
    }
}
