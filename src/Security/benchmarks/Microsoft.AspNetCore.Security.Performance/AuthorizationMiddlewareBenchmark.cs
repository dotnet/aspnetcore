// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Security
{
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

        private class EndpointFeature : IEndpointFeature
        {
            public Endpoint Endpoint { get; set; }
        }
    }
}
