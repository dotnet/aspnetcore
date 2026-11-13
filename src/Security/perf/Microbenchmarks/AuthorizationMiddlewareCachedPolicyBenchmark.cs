// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Security;

[MemoryDiagnoser]
public class AuthorizationMiddlewareCachedPolicyBenchmark
{
    private AuthorizationMiddleware _authorizationMiddleware;
    private DefaultHttpContext _httpContextNoAuthorizationMetadata;

    [GlobalSetup]
    public void Setup()
    {
        // A typical endpoint carries several non-authorization metadata items (route name, HTTP methods, filters, ...).
        var metadata = new object[12];
        for (var i = 0; i < metadata.Length; i++)
        {
            metadata[i] = new object();
        }
        var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(metadata), "No authorization metadata");

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization();
        services.AddSingleton<EndpointDataSource>(new DefaultEndpointDataSource(endpoint));
        var serviceProvider = services.BuildServiceProvider();

        // Passing the IServiceProvider enables the endpoint policy cache.
        _authorizationMiddleware = new AuthorizationMiddleware(
            _ => Task.CompletedTask,
            serviceProvider.GetRequiredService<IAuthorizationPolicyProvider>(),
            serviceProvider,
            serviceProvider.GetRequiredService<ILogger<AuthorizationMiddleware>>());

        _httpContextNoAuthorizationMetadata = new DefaultHttpContext { RequestServices = serviceProvider };
        _httpContextNoAuthorizationMetadata.SetEndpoint(endpoint);
    }

    [Benchmark]
    public Task Invoke_CachedEndpoint_NoAuthorizationMetadata()
    {
        return _authorizationMiddleware.Invoke(_httpContextNoAuthorizationMetadata);
    }
}
