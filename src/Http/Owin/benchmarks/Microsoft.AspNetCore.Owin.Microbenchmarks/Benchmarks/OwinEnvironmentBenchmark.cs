// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Owin.Microbenchmarks.Benchmarks;

[MemoryDiagnoser]
public class OwinEnvironmentBenchmark
{
    const int RequestCount = 10000;

    RequestDelegate _noOperationRequestDelegate;
    RequestDelegate _accessPortsRequestDelegate;
    RequestDelegate _accessHeadersRequestDelegate;

    HttpContext _defaultHttpContext;
    HttpContext _httpContextWithHeaders;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _noOperationRequestDelegate = BuildRequestDelegate();
        _accessPortsRequestDelegate = BuildRequestDelegate(beforeOwinInvokeAction: env =>
        {
            _ = env.TryGetValue("server.LocalPort", out var localPort);
            _ = env.TryGetValue("server.RemotePort", out var remotePort);
        });
        _accessHeadersRequestDelegate = BuildRequestDelegate(
            beforeOwinInvokeAction: env =>
            {
                _ = env.TryGetValue("owin.RequestHeaders", out var requestHeaders);
            },
            afterOwinInvokeAction: env =>
            {
                _ = env.TryGetValue("owin.ResponseHeaders", out var responseHeaders);
            }
        );

        _defaultHttpContext = new DefaultHttpContext();

        _httpContextWithHeaders = new DefaultHttpContext();
        _httpContextWithHeaders.Request.Headers["CustomRequestHeader1"] = "CustomRequestValue";
        _httpContextWithHeaders.Request.Headers["CustomRequestHeader2"] = "CustomRequestValue";
        _httpContextWithHeaders.Response.Headers["CustomResponseHeader1"] = "CustomResponseValue";
        _httpContextWithHeaders.Response.Headers["CustomResponseHeader2"] = "CustomResponseValue";
    }

    [Benchmark]
    public async Task OwinRequest_NoOperation()
    {
        foreach (var i in Enumerable.Range(0, RequestCount))
        {
            await _noOperationRequestDelegate(_defaultHttpContext);
        }
    }

    [Benchmark]
    public async Task OwinRequest_AccessPorts()
    {
        foreach (var i in Enumerable.Range(0, RequestCount))
        {
            await _accessPortsRequestDelegate(_defaultHttpContext);
        }
    }

    [Benchmark]
    public async Task OwinRequest_AccessHeaders()
    {
        foreach (var i in Enumerable.Range(0, RequestCount))
        {
            await _accessHeadersRequestDelegate(_httpContextWithHeaders);
        }
    }

    private static RequestDelegate BuildRequestDelegate(
        Action<IDictionary<string, object>> beforeOwinInvokeAction = null,
        Action<IDictionary<string, object>> afterOwinInvokeAction = null)
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var builder = new ApplicationBuilder(serviceProvider);

        return builder.UseOwin(addToPipeline =>
        {
            addToPipeline(next =>
            {
                return async env =>
                {
                    if (beforeOwinInvokeAction is not null)
                    {
                        beforeOwinInvokeAction(env);
                    }

                    await next(env);

                    if (afterOwinInvokeAction is not null)
                    {
                        afterOwinInvokeAction(env);
                    }
                };
            });
        }).Build();
    }
}
