// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides extension methods for <see cref="IEndpointRouteBuilder"/> to add short circuited endpoints.
/// </summary>
public static class RouteShortCircuitEndpointRouteBuilderExtensions
{
    private static readonly RequestDelegate _shortCircuitDelegate = (context) => Task.CompletedTask;
    /// <summary>
    /// Adds a <see cref="RouteEndpoint"/> to the <see cref="IEndpointRouteBuilder"/> that matches HTTP requests (all verbs)
    /// for the specified prefixes.
    /// </summary>
    ///<param name="builder">The <see cref="IEndpointRouteBuilder"/> to add the route to.</param>
    /// <param name="statusCode">The status code to set in the response.</param>
    /// <param name="routePrefixes">An array of route prefixes to be short circuited.</param>
    /// <returns>A <see cref="IEndpointConventionBuilder"/> that can be used to further customize the endpoint.</returns>
    public static IEndpointConventionBuilder MapShortCircuit(this IEndpointRouteBuilder builder, int statusCode, params string[] routePrefixes)
    {
        var group = builder.MapGroup("");
        foreach (var routePrefix in routePrefixes)
        {
            string route;
            if (routePrefix.EndsWith("/", StringComparison.OrdinalIgnoreCase))
            {
                route = $"{routePrefix}{{**catchall}}";
            }
            else
            {
                route = $"{routePrefix}/{{**catchall}}";
            }
            group.Map(route, _shortCircuitDelegate)
                .ShortCircuit(statusCode)
                .Add(endpoint =>
                {
                    endpoint.DisplayName = $"ShortCircuit {endpoint.DisplayName}";
                    ((RouteEndpointBuilder)endpoint).Order = int.MaxValue;
                });
        }

        return new EndpointConventionBuilder(group);
    }

    private sealed class EndpointConventionBuilder : IEndpointConventionBuilder
    {
        private readonly IEndpointConventionBuilder _endpointConventionBuilder;

        public EndpointConventionBuilder(IEndpointConventionBuilder endpointConventionBuilder)
        {
            _endpointConventionBuilder = endpointConventionBuilder;
        }

        public void Add(Action<EndpointBuilder> convention)
        {
            _endpointConventionBuilder.Add(convention);
        }

        public void Finally(Action<EndpointBuilder> finalConvention)
        {
            _endpointConventionBuilder.Finally(finalConvention);
        }
    }
}
