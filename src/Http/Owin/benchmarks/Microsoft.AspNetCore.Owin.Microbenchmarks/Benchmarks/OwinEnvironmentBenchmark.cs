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
    private RequestDelegate _requestDelegate;
    private readonly HttpContext _httpContext = new DefaultHttpContext();

    [GlobalSetup]
    public void GlobalSetup()
    {
        var serviceProvider = new ServiceCollection().BuildServiceProvider();
        var builder = new ApplicationBuilder(serviceProvider);
        IDictionary<string, object> environment = null;

        builder.UseOwin(addToPipeline =>
        {
            addToPipeline(next =>
            {
                return async env =>
                {
                    environment = env;
                    await next(env);
                };
            });
        });

        _requestDelegate = builder.Build();
    }

    [Benchmark]
    public async Task ProcessMultipleRequests()
    {
        foreach (var i in Enumerable.Range(0, 10000))
        {
            await _requestDelegate(_httpContext);
        }
    }
}
