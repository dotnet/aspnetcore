// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.Net.Http.Headers;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class ResponseHeaderCollectionBenchmark
{
    private const int InnerLoopCount = 128;

    private static readonly byte[] _bytesServer = Encoding.ASCII.GetBytes("\r\nServer: Kestrel");
    private static readonly DateHeaderValueManager _dateHeaderValueManager = new DateHeaderValueManager(TimeProvider.System);
    private HttpResponseHeaders _responseHeadersDirect;
    private HttpResponse _response;

    public enum BenchmarkTypes
    {
        ContentLengthNumeric,
        ContentLengthString,
        Plaintext,
        Common,
        Unknown
    }

    [Params(
        BenchmarkTypes.ContentLengthNumeric,
        BenchmarkTypes.ContentLengthString,
        BenchmarkTypes.Plaintext,
        BenchmarkTypes.Common,
        BenchmarkTypes.Unknown
    )]
    public BenchmarkTypes Type { get; set; }

    [Benchmark(OperationsPerInvoke = InnerLoopCount)]
    public void SetHeaders()
    {
        switch (Type)
        {
            case BenchmarkTypes.ContentLengthNumeric:
                ContentLengthNumeric(InnerLoopCount);
                break;
            case BenchmarkTypes.ContentLengthString:
                ContentLengthString(InnerLoopCount);
                break;
            case BenchmarkTypes.Plaintext:
                Plaintext(InnerLoopCount);
                break;
            case BenchmarkTypes.Common:
                Common(InnerLoopCount);
                break;
            case BenchmarkTypes.Unknown:
                Unknown(InnerLoopCount);
                break;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ContentLengthNumeric(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _responseHeadersDirect.Reset();

            _response.ContentLength = 0;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void ContentLengthString(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _responseHeadersDirect.Reset();

            _response.Headers[HeaderNames.ContentLength] = "0";
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Plaintext(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _responseHeadersDirect.Reset();

            _response.StatusCode = 200;
            _response.ContentType = "text/plain";
            _response.ContentLength = 13;

            var dateHeaderValues = _dateHeaderValueManager.GetDateHeaderValues();
            _responseHeadersDirect.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);
            _responseHeadersDirect.SetRawServer("Kestrel", _bytesServer);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Common(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _responseHeadersDirect.Reset();

            _response.StatusCode = 200;
            _response.ContentType = "text/css";
            _response.ContentLength = 421;

            var headers = _response.Headers;

            headers.Connection = "Close";
            headers.CacheControl = "public, max-age=30672000";
            headers.Vary = "Accept-Encoding";
            headers.ContentEncoding = "gzip";
            headers.Expires = "Fri, 12 Jan 2018 22:01:55 GMT";
            headers.LastModified = "Wed, 22 Jun 2016 20:08:29 GMT";
            headers.SetCookie = "prov=20629ccd-8b0f-e8ef-2935-cd26609fc0bc; __qca=P0-1591065732-1479167353442; _ga=GA1.2.1298898376.1479167354; _gat=1; sgt=id=9519gfde_3347_4762_8762_df51458c8ec2; acct=t=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric&s=why-is-%e0%a5%a7%e0%a5%a8%e0%a5%a9-numeric";
            headers.ETag = "\"54ef7954-1078\"";
            headers.TransferEncoding = "chunked";
            headers.ContentLanguage = "en-gb";
            headers.Upgrade = "websocket";
            headers.Via = "1.1 varnish";
            headers.AccessControlAllowOrigin = "*";
            headers.AccessControlAllowCredentials = "true";
            headers.AccessControlExposeHeaders = "Client-Protocol, Content-Length, Content-Type, X-Bandwidth-Est, X-Bandwidth-Est2, X-Bandwidth-Est-Comp, X-Bandwidth-Avg, X-Walltime-Ms, X-Sequence-Num";

            var dateHeaderValues = _dateHeaderValueManager.GetDateHeaderValues();
            _responseHeadersDirect.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);
            _responseHeadersDirect.SetRawServer("Kestrel", _bytesServer);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Unknown(int count)
    {
        for (var i = 0; i < count; i++)
        {
            _responseHeadersDirect.Reset();

            _response.StatusCode = 200;
            _response.ContentType = "text/plain";
            _response.ContentLength = 13;

            var headers = _response.Headers;

            headers.Link = "<https://www.gravatar.com/avatar/6ae816bfaad7bbc58b17fac49ef5cced?d=404&s=250>; rel=\"canonical\"";
            headers.XUACompatible = "IE=Edge";
            headers.XPoweredBy = "ASP.NET";
            headers.XContentTypeOptions = "nosniff";
            headers.XXSSProtection = "1; mode=block";
            headers.XFrameOptions = "SAMEORIGIN";
            headers.StrictTransportSecurity = "max-age=31536000; includeSubDomains; preload";
            headers.ContentSecurityPolicy = "default-src 'none'; script-src 'self' cdnjs.cloudflare.com code.jquery.com scotthelme.disqus.com a.disquscdn.com www.google-analytics.com go.disqus.com platform.twitter.com cdn.syndication.twimg.com; style-src 'self' a.disquscdn.com fonts.googleapis.com cdnjs.cloudflare.com platform.twitter.com; img-src 'self' data: www.gravatar.com www.google-analytics.com links.services.disqus.com referrer.disqus.com a.disquscdn.com cdn.syndication.twimg.com syndication.twitter.com pbs.twimg.com platform.twitter.com abs.twimg.com; child-src fusiontables.googleusercontent.com fusiontables.google.com www.google.com disqus.com www.youtube.com syndication.twitter.com platform.twitter.com; frame-src fusiontables.googleusercontent.com fusiontables.google.com www.google.com disqus.com www.youtube.com syndication.twitter.com platform.twitter.com; connect-src 'self' links.services.disqus.com; font-src 'self' cdnjs.cloudflare.com fonts.gstatic.com fonts.googleapis.com; form-action 'self'; upgrade-insecure-requests;";

            var dateHeaderValues = _dateHeaderValueManager.GetDateHeaderValues();
            _responseHeadersDirect.SetRawDate(dateHeaderValues.String, dateHeaderValues.Bytes);
            _responseHeadersDirect.SetRawServer("Kestrel", _bytesServer);
        }
    }

    [IterationSetup]
    public void Setup()
    {
        var memoryPool = PinnedBlockMemoryPoolFactory.Create();
        var options = new PipeOptions(memoryPool, readerScheduler: PipeScheduler.Inline, writerScheduler: PipeScheduler.Inline, useSynchronizationContext: false);
        var pair = DuplexPipe.CreateConnectionPair(options, options);

        var serviceContext = TestContextFactory.CreateServiceContext(
            serverOptions: new KestrelServerOptions(),
            httpParser: new HttpParser<Http1ParsingHandler>(),
            dateHeaderValueManager: _dateHeaderValueManager);

        var connectionContext = TestContextFactory.CreateHttpConnectionContext(
            serviceContext: serviceContext,
            connectionContext: null,
            transport: pair.Transport,
            memoryPool: memoryPool,
            connectionFeatures: new FeatureCollection());

        var http1Connection = new Http1Connection(connectionContext);

        http1Connection.Reset();
        serviceContext.DateHeaderValueManager.OnHeartbeat();

        _responseHeadersDirect = (HttpResponseHeaders)http1Connection.ResponseHeaders;
        var context = new DefaultHttpContext(http1Connection);
        _response = context.Response;

        switch (Type)
        {
            case BenchmarkTypes.ContentLengthNumeric:
                ContentLengthNumeric(1);
                break;
            case BenchmarkTypes.ContentLengthString:
                ContentLengthString(1);
                break;
            case BenchmarkTypes.Plaintext:
                Plaintext(1);
                break;
            case BenchmarkTypes.Common:
                Common(1);
                break;
            case BenchmarkTypes.Unknown:
                Unknown(1);
                break;
        }
    }
}
