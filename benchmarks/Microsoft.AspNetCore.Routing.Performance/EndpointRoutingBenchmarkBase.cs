// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Routing
{
    public abstract class EndpointRoutingBenchmarkBase
    {
        private protected MatcherEndpoint[] Endpoints;
        private protected HttpContext[] Requests;

        private protected void SetupEndpoints(params MatcherEndpoint[] endpoints)
        {
            Endpoints = endpoints;
        }

        // The older routing implementations retrieve services when they first execute.
        private protected IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
            services.AddLogging();
            services.AddOptions();
            services.AddRouting();

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<EndpointDataSource>(new DefaultEndpointDataSource(Endpoints)));

            return services.BuildServiceProvider();
        }

        private protected DfaMatcherBuilder CreateDfaMatcherBuilder()
        {
            return CreateServices().GetRequiredService<DfaMatcherBuilder>();
        }

        private protected static  int[] SampleRequests(int endpointCount, int count)
        {
            // This isn't very high tech, but it's at least regular distribution.
            // We sort the route templates by precedence, so this should result in
            // an even distribution of the 'complexity' of the routes that are exercised.
            var frequency = endpointCount / count;
            if (frequency < 2)
            {
                throw new InvalidOperationException(
                    "The sample count is too high. This won't produce an accurate sampling" +
                    "of the request data.");
            }

            var samples = new int[count];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = i * frequency;
            }

            return samples;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private protected void Validate(HttpContext httpContext, Endpoint expected, Endpoint actual)
        {
            if (!object.ReferenceEquals(expected, actual))
            {
                var message = new StringBuilder();
                message.AppendLine($"Validation failed for request {Array.IndexOf(Requests, httpContext)}");
                message.AppendLine($"{httpContext.Request.Method} {httpContext.Request.Path}");
                message.AppendLine($"expected: '{((MatcherEndpoint)expected)?.DisplayName ?? "null"}'");
                message.AppendLine($"actual:   '{((MatcherEndpoint)actual)?.DisplayName ?? "null"}'");
                throw new InvalidOperationException(message.ToString());
            }
        }

        protected void AssertUrl(string expectedUrl, string actualUrl)
        {
            AssertUrl(expectedUrl, actualUrl, StringComparison.Ordinal);
        }

        protected void AssertUrl(string expectedUrl, string actualUrl, StringComparison stringComparison)
        {
            if (!string.Equals(expectedUrl, actualUrl, stringComparison))
            {
                throw new InvalidOperationException($"Expected: {expectedUrl}, Actual: {actualUrl}");
            }
        }

        protected MatcherEndpoint CreateEndpoint(
            string template,
            object defaults = null,
            object constraints = null,
            object requiredValues = null,
            int order = 0,
            string displayName = null,
            string routeName = null,
            params object[] metadata)
        {
            var endpointMetadata = new List<object>(metadata ?? Array.Empty<object>());
            endpointMetadata.Add(new RouteValuesAddressMetadata(routeName, new RouteValueDictionary(requiredValues)));

            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                RoutePatternFactory.Parse(template, defaults, constraints),
                order,
                new EndpointMetadataCollection(endpointMetadata),
                displayName);
        }

        protected (HttpContext httpContext, RouteValueDictionary ambientValues) CreateCurrentRequestContext(
            object ambientValues = null)
        {
            var feature = new EndpointFeature { Values = new RouteValueDictionary(ambientValues) };
            var context = new DefaultHttpContext();
            context.Features.Set<IEndpointFeature>(feature);

            return (context, feature.Values);
        }

        protected void CreateOutboundRouteEntry(TreeRouteBuilder treeRouteBuilder, MatcherEndpoint endpoint)
        {
            var routeValuesAddressMetadata = endpoint.Metadata.GetMetadata<IRouteValuesAddressMetadata>();
            var requiredValues = routeValuesAddressMetadata?.RequiredValues ?? new RouteValueDictionary();

            treeRouteBuilder.MapOutbound(
                NullRouter.Instance,
                new RouteTemplate(RoutePatternFactory.Parse(
                    endpoint.RoutePattern.RawText,
                    defaults: endpoint.RoutePattern.Defaults,
                    parameterPolicies: null)),
                requiredLinkValues: new RouteValueDictionary(requiredValues),
                routeName: null,
                order: 0);
        }
    }
}
