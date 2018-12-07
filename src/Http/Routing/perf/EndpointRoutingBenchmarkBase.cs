// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
        private protected RouteEndpoint[] Endpoints;
        private protected HttpContext[] Requests;

        private protected void SetupEndpoints(params RouteEndpoint[] endpoints)
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
                message.AppendLine($"expected: '{((RouteEndpoint)expected)?.DisplayName ?? "null"}'");
                message.AppendLine($"actual:   '{((RouteEndpoint)actual)?.DisplayName ?? "null"}'");
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

        protected RouteEndpoint CreateEndpoint(string template, string httpMethod)
        {
            return CreateEndpoint(template, metadata: new object[]
            {
                new HttpMethodMetadata(new string[]{ httpMethod, }),
            });
        }

        protected RouteEndpoint CreateEndpoint(
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
            if (routeName != null)
            {
                endpointMetadata.Add(new RouteNameMetadata(routeName));
            }

            return new RouteEndpoint(
                (context) => Task.CompletedTask,
                RoutePatternFactory.Parse(template, defaults, constraints, requiredValues),
                order,
                new EndpointMetadataCollection(endpointMetadata),
                displayName);
        }

        protected (HttpContext httpContext, RouteValueDictionary ambientValues) CreateCurrentRequestContext(
            object ambientValues = null)
        {
            var feature = new EndpointSelectorContext { RouteValues = new RouteValueDictionary(ambientValues) };
            var context = new DefaultHttpContext();
            context.Features.Set<IEndpointFeature>(feature);
            context.Features.Set<IRouteValuesFeature>(feature);

            return (context, feature.RouteValues);
        }

        protected void CreateOutboundRouteEntry(TreeRouteBuilder treeRouteBuilder, RouteEndpoint endpoint)
        {
            treeRouteBuilder.MapOutbound(
                NullRouter.Instance,
                new RouteTemplate(RoutePatternFactory.Parse(
                    endpoint.RoutePattern.RawText,
                    defaults: endpoint.RoutePattern.Defaults,
                    parameterPolicies: null)),
                requiredLinkValues: new RouteValueDictionary(endpoint.RoutePattern.RequiredValues),
                routeName: null,
                order: 0);
        }
    }
}
