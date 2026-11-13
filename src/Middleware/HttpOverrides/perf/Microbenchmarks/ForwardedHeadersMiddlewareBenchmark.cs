// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.HttpOverrides.Microbenchmarks;

[MemoryDiagnoser]
public class ForwardedHeadersMiddlewareBenchmark
{
    private ForwardedHeadersMiddleware _middleware = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost,
            ForwardLimit = null,
        };
        options.KnownProxies.Clear();
        options.KnownProxies.Add(IPAddress.Parse("10.0.0.1"));
        options.KnownProxies.Add(IPAddress.Parse("10.0.0.2"));

        _middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
    }

    [Benchmark(Baseline = true)]
    public HttpContext ContextOnly() => CreateHttpContext();

    [Benchmark]
    public HttpContext NoForwardedHeaders()
    {
        var context = CreateHttpContext();
        _middleware.ApplyForwarders(context);
        return context;
    }

    [Benchmark]
    public HttpContext ForwardedForAndProto()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7";
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        _middleware.ApplyForwarders(context);
        return context;
    }

    [Benchmark]
    public HttpContext ThreeHopChain()
    {
        var context = CreateHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.7, 10.0.0.2, 10.0.0.1";
        context.Request.Headers["X-Forwarded-Proto"] = "https, http, http";
        context.Request.Headers["X-Forwarded-Host"] = "www.example.com, internal-lb, internal-proxy";
        _middleware.ApplyForwarders(context);
        return context;
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.1");
        context.Connection.RemotePort = 54321;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost:5000");
        return context;
    }
}
