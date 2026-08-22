// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.InternalTesting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http2;

namespace Microsoft.AspNetCore.Server.Kestrel.Microbenchmarks;

public class Http2PrefaceConnectionMiddlewareBenchmark
{
    private static readonly byte[] Http1Request = "GET / HTTP/1.1\r\nHost:\r\n\r\n"u8.ToArray();
    private static readonly byte[] Http2Preface = Http2Connection.ClientPreface.ToArray();
    private static readonly ConnectionDelegate Next = _ => Task.CompletedTask;

    private Http2PrefaceConnectionMiddleware _http1OnlyMiddleware;
    private Http2PrefaceConnectionMiddleware _mixedMiddleware;
    private DefaultConnectionContext _directConnection;
    private DefaultConnectionContext _http1OnlyConnection;
    private DefaultConnectionContext _mixedHttp1Connection;
    private DefaultConnectionContext _mixedHttp2Connection;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var serviceContext = TestContextFactory.CreateServiceContext(serverOptions: new KestrelServerOptions());
        serviceContext.TimeProvider = TimeProvider.System;
        _http1OnlyMiddleware = new Http2PrefaceConnectionMiddleware(Next, serviceContext, HttpProtocols.Http1);
        _mixedMiddleware = new Http2PrefaceConnectionMiddleware(Next, serviceContext, HttpProtocols.Http1AndHttp2);
        _directConnection = CreateConnection(Http1Request);
        _http1OnlyConnection = CreateConnection(Http1Request);
        _mixedHttp1Connection = CreateConnection(Http1Request);
        _mixedHttp2Connection = CreateConnection(Http2Preface);
    }

    [Benchmark(Baseline = true)]
    public Task DirectNext() => Next(_directConnection);

    [Benchmark]
    public Task Http1OnlyBypass() => _http1OnlyMiddleware.OnConnectionAsync(_http1OnlyConnection);

    [Benchmark]
    public Task MixedSelectHttp1()
    {
        _mixedHttp1Connection.Features.Set<HttpProtocolsFeature>(null);
        return _mixedMiddleware.OnConnectionAsync(_mixedHttp1Connection);
    }

    [Benchmark]
    public Task MixedSelectHttp2()
    {
        _mixedHttp2Connection.Features.Set<HttpProtocolsFeature>(null);
        return _mixedMiddleware.OnConnectionAsync(_mixedHttp2Connection);
    }

    private static DefaultConnectionContext CreateConnection(byte[] input)
    {
        var pair = DuplexPipe.CreateConnectionPair(PipeOptions.Default, PipeOptions.Default);
        pair.Application.Output.WriteAsync(input).GetAwaiter().GetResult();
        return new DefaultConnectionContext("benchmark", pair.Transport, pair.Application);
    }
}
