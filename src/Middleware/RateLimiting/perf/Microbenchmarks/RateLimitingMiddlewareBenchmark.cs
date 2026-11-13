// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.RateLimiting;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.RateLimiting.Microbenchmarks;

[MemoryDiagnoser]
public class RateLimitingMiddlewareBenchmark
{
    private const string PolicyName = "policy";

    private ServiceProvider _globalServices = null!;
    private ServiceProvider _endpointOnlyServices = null!;
    private RequestDelegate _global = null!;
    private RequestDelegate _endpointOnly = null!;
    private Endpoint _endpointWithoutPolicy = null!;
    private Endpoint _endpointWithPolicy = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _endpointWithoutPolicy = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "NoPolicy");
        _endpointWithPolicy = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(new EnableRateLimitingAttribute(PolicyName)), "Policy");

        (_globalServices, _global) = CreatePipeline(withGlobalLimiter: true);
        (_endpointOnlyServices, _endpointOnly) = CreatePipeline(withGlobalLimiter: false);
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _globalServices.Dispose();
        _endpointOnlyServices.Dispose();
    }

    [Benchmark(Baseline = true)]
    public HttpContext ContextOnly() => CreateHttpContext(_endpointWithoutPolicy);

    [Benchmark]
    public Task GlobalLimiter_EndpointWithoutPolicy() => _global(CreateHttpContext(_endpointWithoutPolicy));

    [Benchmark]
    public Task GlobalLimiter_EndpointWithPolicy() => _global(CreateHttpContext(_endpointWithPolicy));

    [Benchmark]
    public Task EndpointPolicyOnly() => _endpointOnly(CreateHttpContext(_endpointWithPolicy));

    private static (ServiceProvider, RequestDelegate) CreatePipeline(bool withGlobalLimiter)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRateLimiter(options =>
        {
            if (withGlobalLimiter)
            {
                options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                    RateLimitPartition.GetConcurrencyLimiter("global", _ => new ConcurrencyLimiterOptions { PermitLimit = 1_000_000, QueueLimit = 0 }));
            }
            options.AddPolicy(PolicyName, _ =>
                RateLimitPartition.GetConcurrencyLimiter("endpoint", _ => new ConcurrencyLimiterOptions { PermitLimit = 1_000_000, QueueLimit = 0 }));
        });

        var serviceProvider = services.BuildServiceProvider();
        var app = new ApplicationBuilder(serviceProvider);
        app.UseRateLimiter();
        app.Run(_ => Task.CompletedTask);
        return (serviceProvider, app.Build());
    }

    private static DefaultHttpContext CreateHttpContext(Endpoint endpoint)
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(endpoint);
        return context;
    }
}
