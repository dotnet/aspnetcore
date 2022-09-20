// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using System.Net.Http.QPack;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http3;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class QPackDecoderBenchmark
{
    private static readonly byte[] _headerFieldLine_LargeLiteralValue;
    private static readonly byte[] _headerFieldLine_LargeLiteralValue_Multiple;
    private static readonly byte[] _headerFieldLine_Static_Multiple;

    static QPackDecoderBenchmark()
    {
        var headers = new HttpResponseHeaders();
        var enumerator = new Http3HeadersEnumerator();

        AddLargeHeader(headers, 'a');
        enumerator.Initialize(headers);
        _headerFieldLine_LargeLiteralValue = GenerateHeaderBytes(enumerator);
        headers.Reset();

        AddLargeHeader(headers, 'a');
        AddLargeHeader(headers, 'b');
        AddLargeHeader(headers, 'c');
        AddLargeHeader(headers, 'd');
        AddLargeHeader(headers, 'e');
        enumerator.Initialize(headers);
        _headerFieldLine_LargeLiteralValue_Multiple = GenerateHeaderBytes(enumerator);
        headers.Reset();

        ((IHeaderDictionary)headers).ContentLength = 0;
        ((IHeaderDictionary)headers).ContentType = "application/json";
        ((IHeaderDictionary)headers).Age = "0";
        ((IHeaderDictionary)headers).AcceptRanges = "bytes";
        ((IHeaderDictionary)headers).AccessControlAllowOrigin = "*";
        enumerator.Initialize(headers);
        _headerFieldLine_Static_Multiple = GenerateHeaderBytes(enumerator);

        static void AddLargeHeader(HttpResponseHeaders headers, char c)
        {
            var s = new string(c, 8193);
            var success = headers.TryAdd(s, s);
            if (!success)
            {
                throw new InvalidOperationException();
            }
        }

        static byte[] GenerateHeaderBytes(Http3HeadersEnumerator enumerator)
        {
            var buffer = new byte[1024 * 1024];
            var totalHeaderSize = 0;
            var success = QPackHeaderWriter.BeginEncodeHeaders(enumerator, buffer, ref totalHeaderSize, out var length);
            if (!success)
            {
                throw new InvalidOperationException();
            }

            return buffer[..length];
        }
    }

    private QPackDecoder _decoder;
    private TestHeadersHandler _testHeadersHandler;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _decoder = new QPackDecoder(maxHeadersLength: 65536);
        _testHeadersHandler = new TestHeadersHandler();
    }

    [Benchmark]
    public void DecodeHeaderFieldLine_LargeLiteralValue()
    {
        _decoder.Decode(_headerFieldLine_LargeLiteralValue, endHeaders: true, handler: _testHeadersHandler);
        _decoder.Reset();
    }

    [Benchmark]
    public void DecodeHeaderFieldLine_LargeLiteralValue_Multiple()
    {
        _decoder.Decode(_headerFieldLine_LargeLiteralValue_Multiple, endHeaders: true, handler: _testHeadersHandler);
        _decoder.Reset();
    }

    [Benchmark]
    public void DecodeHeaderFieldLine_Static_Multiple()
    {
        _decoder.Decode(_headerFieldLine_Static_Multiple, endHeaders: true, handler: _testHeadersHandler);
        _decoder.Reset();
    }

    private sealed class TestHeadersHandler : IHttpStreamHeadersHandler
    {
        public void OnDynamicIndexedHeader(int? index, ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
        }

        public void OnHeader(ReadOnlySpan<byte> name, ReadOnlySpan<byte> value)
        {
        }

        public void OnHeadersComplete(bool endStream)
        {
        }

        public void OnStaticIndexedHeader(int index)
        {
        }

        public void OnStaticIndexedHeader(int index, ReadOnlySpan<byte> value)
        {
        }
    }
}
