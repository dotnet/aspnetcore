// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Security;

public class AuthorizationMiddlewareBenchmark
{
    private AuthorizationMiddleware _authorizationMiddleware;
    private DefaultHttpContext _httpContextNoEndpoint;
    private DefaultHttpContext _httpContextHasEndpoint;

    [GlobalSetup]
    public void Setup()
    {
        var policyProvider = new DefaultAuthorizationPolicyProvider(Options.Create(new AuthorizationOptions()));
        _authorizationMiddleware = new AuthorizationMiddleware((context) => Task.CompletedTask, policyProvider);

        _httpContextNoEndpoint = new DefaultHttpContext();

        var feature = new EndpointFeature
        {
            Endpoint = new Endpoint((context) => Task.CompletedTask, EndpointMetadataCollection.Empty, "Test endpoint")
        };
        _httpContextHasEndpoint = new DefaultHttpContext();
        _httpContextHasEndpoint.Features.Set<IEndpointFeature>(feature);
    }

    [Benchmark]
    public Task Invoke_NoEndpoint_NoAuthorization()
    {
        return _authorizationMiddleware.Invoke(_httpContextNoEndpoint);
    }

    [Benchmark]
    public Task Invoke_HasEndpoint_NoAuthorization()
    {
        return _authorizationMiddleware.Invoke(_httpContextHasEndpoint);
    }

    private sealed class EndpointFeature : IEndpointFeature
    {
        public Endpoint Endpoint { get; set; }
    }
}
