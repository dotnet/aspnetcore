// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.AspNetCore.Routing.Metadata;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public abstract class MatcherBenchmarkBase
    {
        private protected MatcherEndpoint[] Endpoints;
        private protected HttpContext[] Requests;

        // The older routing implementations retrieve services when they first execute.
        private protected static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddOptions();
            services.AddRouting();
            return services.BuildServiceProvider();
        }

        private protected DfaMatcherBuilder CreateDfaMatcherBuilder()
        {
            return CreateServices().GetRequiredService<DfaMatcherBuilder>();
        }

        private protected static MatcherEndpoint CreateEndpoint(string template, string httpMethod = null)
        {
            var metadata = new List<object>();
            if (httpMethod != null)
            {
                metadata.Add(new HttpMethodMetadata(new string[] { httpMethod, }));
            }

            return new MatcherEndpoint(
                MatcherEndpoint.EmptyInvoker,
                RoutePatternFactory.Parse(template),
                new RouteValueDictionary(),
                0,
                new EndpointMetadataCollection(metadata),
                template);
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
    }
}
