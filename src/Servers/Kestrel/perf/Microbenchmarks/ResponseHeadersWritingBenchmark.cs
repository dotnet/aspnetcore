// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Infrastructure;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class ResponseHeadersWritingBenchmark
{
    private const int Iterations = 1000;

    private static readonly byte[] _bytesServer = Encoding.ASCII.GetBytes("\r\nServer: " + Constants.ServerName);
    private static readonly byte[] _helloWorldPayload = Encoding.ASCII.GetBytes("Hello, World!");

    private HttpResponseHeaders _responseHeaders;
    private IHeaderDictionary _responseHeadersDict;
    private DateHeaderValueManager _dateHeaderValueManager;
    private Writer _writer;

    private DateHeaderValueManager.DateHeaderValues DateHeaderValues => _dateHeaderValueManager.GetDateHeaderValues();

    [Params(
        BenchmarkTypes.TechEmpowerPlaintext,
        BenchmarkTypes.PlaintextChunked,
        BenchmarkTypes.PlaintextWithCookie,
        BenchmarkTypes.PlaintextChunkedWithCookie,
        BenchmarkTypes.LiveAspNet,
        BenchmarkTypes.Common
    )]
    public BenchmarkTypes Type { get; set; }

    [Benchmark(OperationsPerInvoke = Iterations)]
    public void Output()
    {
        switch (Type)
        {
            case BenchmarkTypes.TechEmpowerPlaintext:
                TechEmpowerPlaintext();
                break;
            case BenchmarkTypes.PlaintextChunked:
                PlaintextChunked();
                break;
            case BenchmarkTypes.PlaintextWithCookie:
                PlaintextWithCookie();
                break;
            case BenchmarkTypes.PlaintextChunkedWithCookie:
                PlaintextChunkedWithCookie();
                break;
            case BenchmarkTypes.LiveAspNet:
                LiveAspNet();
                break;
            case BenchmarkTypes.Common:
                Common();
                break;
        }
    }

    private void TechEmpowerPlaintext()
    {
        var responseHeaders = _responseHeadersDict;
        responseHeaders.ContentType = "text/plain";
        responseHeaders.ContentLength = _helloWorldPayload.Length;

        var writer = new BufferWriter<PipeWriter>(_writer);

        for (var i = 0; i < Iterations; i++)
        {
            _responseHeaders.CopyTo(ref writer);
        }
    }

    private void PlaintextChunked()
    {
        var responseHeaders = _responseHeadersDict;
        responseHeaders.ContentType = "text/plain";

        var writer = new BufferWriter<PipeWriter>(_writer);

        for (var i = 0; i < Iterations; i++)
        {
            _responseHeaders.CopyTo(ref writer);
        }
    }

    private void LiveAspNet()
    {
        var responseHeaders = _responseHeadersDict;
        responseHeaders.ContentEncoding = "gzip";
        responseHeaders.ContentType = "text/html; charset=utf-8";
        responseHeaders.StrictTransportSecurity = "max-age=31536000; includeSubdomains";
        responseHeaders.Vary = "Accept-Encoding";
        _responseHeadersDict["X-Powered-By"] = "ASP.NET";

        var writer = new BufferWriter<PipeWriter>(_writer);

        for (var i = 0; i < Iterations; i++)
        {
            _responseHeaders.CopyTo(ref writer);
        }
    }

    private void Common()
    {
        var responseHeaders = _responseHeadersDict;
        responseHeaders.ContentType = "text/css";
        responseHeaders.ContentLength = 421;

        responseHeaders.Connection = "Close";
        responseHeaders.CacheControl = "public, max-age=30672000";
        responseHeaders.Vary = "Accept-Encoding";
        responseHeaders.ContentEncoding = "gzip";
        responseHeaders.Expires = "Fri, 12 Jan 2018 22:01:55 GMT";
        responseHeaders.LastModified = "Wed, 22 Jun 2016 20:08:29 GMT";
        responseHeaders.SetCookie = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
        responseHeaders.ETag = "\"54ef7954-1078\"";
        responseHeaders.TransferEncoding = "chunked";
        responseHeaders.ContentLanguage = "en-gb";
        responseHeaders.Upgrade = "websocket";
        responseHeaders.Via = "1.1 varnish";
        responseHeaders.AccessControlAllowOrigin = "*";
        responseHeaders.AccessControlAllowCredentials = "true";
        responseHeaders.AccessControlExposeHeaders = "Client-Protocol, Content-Length, Content-Type, X-Bandwidth-Est, X-Bandwidth-Est2, X-Bandwidth-Est-Comp, X-Bandwidth-Avg, X-Walltime-Ms, X-Sequence-Num";

        var dateHeaderValues = _dateHeaderValueManager.GetDateHeaderValues();
        _responseHeaders.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);
        _responseHeaders.SetRawServer("Kestrel", _bytesServer);

        var writer = new BufferWriter<PipeWriter>(_writer);

        for (var i = 0; i < Iterations; i++)
        {
            _responseHeaders.CopyTo(ref writer);
        }
    }

    private void PlaintextWithCookie()
    {
        var responseHeaders = _responseHeadersDict;
        responseHeaders.ContentType = "text/plain";
        responseHeaders.SetCookie = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
        responseHeaders.ContentLength = _helloWorldPayload.Length;

        var writer = new BufferWriter<PipeWriter>(_writer);

        for (var i = 0; i < Iterations; i++)
        {
            _responseHeaders.CopyTo(ref writer);
        }
    }

    private void PlaintextChunkedWithCookie()
    {
        var responseHeaders = _responseHeadersDict;
        responseHeaders.ContentType = "text/plain";
        responseHeaders.SetCookie = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
        responseHeaders.TransferEncoding = "chunked";

        var writer = new BufferWriter<PipeWriter>(_writer);

        for (var i = 0; i < Iterations; i++)
        {
            _responseHeaders.CopyTo(ref writer);
        }
    }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _responseHeaders = new HttpResponseHeaders();
        _responseHeadersDict = _responseHeaders;
        _dateHeaderValueManager = new DateHeaderValueManager(TimeProvider.System);
        _dateHeaderValueManager.OnHeartbeat();
        _writer = new Writer();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _responseHeaders.Reset();
        _responseHeaders.SetRawServer(Constants.ServerName, _bytesServer);
        _responseHeaders.SetRawDate(DateHeaderValues.String, DateHeaderValues.Bytes);
    }

    public class Writer : PipeWriter
    {
        private readonly Memory<byte> _memory = new byte[4096 * 4];

        public override Memory<byte> GetMemory(int sizeHint = 0) => _memory;

        public override Span<byte> GetSpan(int sizeHint = 0) => _memory.Span;

        public override void Advance(int bytes) { }
        public override void CancelPendingFlush() { }
        public override void Complete(Exception exception = null) { }
        public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => default;
    }

    public enum BenchmarkTypes
    {
        TechEmpowerPlaintext,
        PlaintextChunked,
        PlaintextWithCookie,
        PlaintextChunkedWithCookie,
        LiveAspNet,
        Common
    }
}
