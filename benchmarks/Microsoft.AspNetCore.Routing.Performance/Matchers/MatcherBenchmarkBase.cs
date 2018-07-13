// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.EndpointConstraints;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Routing.Matchers
{
    public abstract class MatcherBenchmarkBase
    {
        internal MatcherEndpoint[] _endpoints;
        internal HttpContext[] _requests;

        // The older routing implementations retrieve services when they first execute.
        internal static IServiceProvider CreateServices()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            return services.BuildServiceProvider();
        }

        internal static MatcherEndpoint CreateEndpoint(string template, string httpMethod = null)
        {
            var metadata = new List<object>();
            if (httpMethod != null)
            {
                metadata.Add(new HttpMethodEndpointConstraint(new string[] { httpMethod, }));
            }

            return new MatcherEndpoint(
                (next) => (context) => Task.CompletedTask,
                template,
                new RouteValueDictionary(),
                new RouteValueDictionary(),
                new List<MatchProcessorReference>(),
                0,
                EndpointMetadataCollection.Empty,
                template);
        }

        internal static  int[] SampleRequests(int endpointCount, int count)
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
        internal void Validate(HttpContext httpContext, Endpoint expected, Endpoint actual)
        {
            if (!object.ReferenceEquals(expected, actual))
            {
                var message = new StringBuilder();
                message.AppendLine($"Validation failed for request {Array.IndexOf(_requests, httpContext)}");
                message.AppendLine($"{httpContext.Request.Method} {httpContext.Request.Path}");
                message.AppendLine($"expected: '{((MatcherEndpoint)expected)?.DisplayName ?? "null"}'");
                message.AppendLine($"actual:   '{((MatcherEndpoint)actual)?.DisplayName ?? "null"}'");
                throw new InvalidOperationException(message.ToString());
            }
        }
    }
}
