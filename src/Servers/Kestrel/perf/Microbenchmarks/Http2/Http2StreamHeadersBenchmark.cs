// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.IO.Pipelines;
using System.Net.Http.HPack;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2.FlowControl;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

/// <summary>
/// Measures the per-request work an <see cref="Http2Stream"/> does to receive request headers
/// (as dispatched by Http2Connection after HPACK decoding) and validate the pseudo-headers.
/// </summary>
public class Http2StreamHeadersBenchmark
{
    private static readonly byte[] _authorityValue = Encoding.ASCII.GetBytes("www.example.com");
    private static readonly byte[] _userAgentValue = Encoding.ASCII.GetBytes("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36");
    private static readonly byte[] _acceptValue = Encoding.ASCII.GetBytes("text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
    private static readonly byte[] _acceptEncodingValue = Encoding.ASCII.GetBytes("gzip, deflate, br, zstd");
    private static readonly byte[] _acceptLanguageValue = Encoding.ASCII.GetBytes("en-US,en;q=0.9");

    private MemoryPool<byte> _memoryPool;
    private BenchmarkHttp2Stream _stream;
    private byte[] _pathValue;

    [Params("/", "/api/products/12345", "/shop/category/electronics/laptops/item-12345?ref=homepage&utm_source=newsletter")]
    public string Path { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _memoryPool = TestMemoryPoolFactory.Create();
        _pathValue = Encoding.ASCII.GetBytes(Path);

        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            httpParser: new HttpParser<Http1ParsingHandler>(),
            dateHeaderValueManager: new DateHeaderValueManager(TimeProvider.System));

        var frameWriter = new Http2FrameWriter(
            new NullPipeWriter(),
            connectionContext: null,
            http2Connection: null,
            maxStreamsPerConnection: 1,
            timeoutControl: null,
            minResponseDataRate: null,
            "TestConnectionId",
            _memoryPool,
            serviceContext);

        var streamContext = TestContextFactory.CreateHttp2StreamContext(
            serviceContext: serviceContext,
            memoryPool: _memoryPool,
            streamId: 1,
            frameWriter: frameWriter,
            connectionInputFlowControl: new InputFlowControl(Http2PeerSettings.DefaultInitialWindowSize, Http2PeerSettings.DefaultInitialWindowSize / 2));

        _stream = new BenchmarkHttp2Stream();
        _stream.Initialize(streamContext);

        // Prime header value reuse.
        if (!ProcessRequestHeaders())
        {
            throw new InvalidOperationException("Request headers were rejected.");
        }
    }

    [Benchmark]
    public bool ProcessRequestHeaders()
    {
        var stream = _stream;
        stream.Reset();

        // Header callbacks as dispatched by Http2Connection.OnHeaderCore for a typical browser request.
        stream.OnHeader(H2StaticTable.MethodGet, indexOnly: true, ":method"u8, "GET"u8);
        stream.OnHeader(H2StaticTable.SchemeHttp, indexOnly: true, ":scheme"u8, "http"u8);
        stream.OnHeader(H2StaticTable.Authority, indexOnly: false, ":authority"u8, _authorityValue);
        stream.OnHeader(H2StaticTable.PathSlash, indexOnly: false, ":path"u8, _pathValue);
        stream.OnHeader(H2StaticTable.UserAgent, indexOnly: false, "user-agent"u8, _userAgentValue);
        stream.OnHeader(H2StaticTable.Accept, indexOnly: false, "accept"u8, _acceptValue);
        stream.OnHeader(H2StaticTable.AcceptEncoding, indexOnly: false, "accept-encoding"u8, _acceptEncodingValue);
        stream.OnHeader(H2StaticTable.AcceptLanguage, indexOnly: false, "accept-language"u8, _acceptLanguageValue);
        stream.OnHeadersComplete();

        return stream.ValidateRequest();
    }

    [GlobalCleanup]
    public void Dispose()
    {
        _memoryPool?.Dispose();
    }

    private sealed class BenchmarkHttp2Stream : Http2Stream
    {
        public override void Execute()
        {
        }

        public bool ValidateRequest() => TryParseRequest(default, out var endConnection) && !endConnection;
    }
}
