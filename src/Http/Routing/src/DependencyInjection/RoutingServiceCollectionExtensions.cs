// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Internal;
using Microsoft.AspNetCore.Routing.Matching;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Routing.Tree;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to <see cref="IServiceCollection"/>.
/// </summary>
public static class RoutingServiceCollectionExtensions
{
    /// <summary>
    /// Adds services required for routing requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddRouting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddTransient<IInlineConstraintResolver, DefaultInlineConstraintResolver>();
        services.TryAddTransient<ObjectPoolProvider, DefaultObjectPoolProvider>();
        services.TryAddSingleton<ObjectPool<UriBuildingContext>>(s =>
        {
            var provider = s.GetRequiredService<ObjectPoolProvider>();
            return provider.Create<UriBuildingContext>(new UriBuilderContextPooledObjectPolicy());
        });

        // The TreeRouteBuilder is a builder for creating routes, it should stay transient because it's
        // stateful.
        services.TryAdd(ServiceDescriptor.Transient<TreeRouteBuilder>(s =>
        {
            var loggerFactory = s.GetRequiredService<ILoggerFactory>();
            var objectPool = s.GetRequiredService<ObjectPool<UriBuildingContext>>();
            var constraintResolver = s.GetRequiredService<IInlineConstraintResolver>();
            return new TreeRouteBuilder(loggerFactory, objectPool, constraintResolver);
        }));

        services.TryAddSingleton(typeof(RoutingMarkerService));

        // Setup global collection of endpoint data sources
        var dataSources = new ObservableCollection<EndpointDataSource>();
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<RouteOptions>, ConfigureRouteOptions>(
            serviceProvider => new ConfigureRouteOptions(dataSources)));

        // Allow global access to the list of endpoints.
        services.TryAddSingleton<EndpointDataSource>(s =>
        {
            // Call internal ctor and pass global collection
            return new CompositeEndpointDataSource(dataSources);
        });

        //
        // Default matcher implementation
        //
        services.TryAddSingleton<ParameterPolicyFactory, DefaultParameterPolicyFactory>();
        services.TryAddSingleton<MatcherFactory, DfaMatcherFactory>();
        services.TryAddTransient<DfaMatcherBuilder>();
        services.TryAddSingleton<DfaGraphWriter>();
        services.TryAddTransient<DataSourceDependentMatcher.Lifetime>();
        services.TryAddSingleton<EndpointMetadataComparer>(services =>
        {
            // This has no public constructor.
            return new EndpointMetadataComparer(services);
        });

        // Link generation related services
        services.TryAddSingleton<LinkGenerator, DefaultLinkGenerator>();
        services.TryAddSingleton<IEndpointAddressScheme<string>, EndpointNameAddressScheme>();
        services.TryAddSingleton<IEndpointAddressScheme<RouteValuesAddress>, RouteValuesAddressScheme>();
        services.TryAddSingleton<LinkParser, DefaultLinkParser>();

        //
        // Endpoint Selection
        //
        services.TryAddSingleton<EndpointSelector, DefaultEndpointSelector>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, HttpMethodMatcherPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, HostMatcherPolicy>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, AcceptsMatcherPolicy>());
        // TODO: Make this 1st class instead
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, EndpointMetadataDecoratorMatcherPolicy>());

        //
        // Misc infrastructure
        //
        services.TryAddSingleton<TemplateBinderFactory, DefaultTemplateBinderFactory>();
        services.TryAddSingleton<RoutePatternTransformer, DefaultRoutePatternTransformer>();

        // Set RouteHandlerOptions.ThrowOnBadRequest in development
        services.TryAddEnumerable(ServiceDescriptor.Transient<IConfigureOptions<RouteHandlerOptions>, ConfigureRouteHandlerOptions>());

        return services;
    }

    /// <summary>
    /// Adds services required for routing requests.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configureOptions">The routing options to configure the middleware with.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddRouting(
        this IServiceCollection services,
        Action<RouteOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        services.Configure(configureOptions);
        services.AddRouting();

        return services;
    }
}

internal class EndpointMetadataDecoratorMatcherPolicy : MatcherPolicy, IEndpointSelectorPolicy
{
    private readonly ConditionalWeakTable<Endpoint, Endpoint> _endpointsCache = new();

    public override int Order { get; }

    public bool AppliesToEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        return endpoints.Any(e => MetadataOnlyEndpoint.IsMetadataOnlyEndpoint(e)
            && e.Metadata.GetMetadata<MetadataOnlyEndpointMetadata>() is not null);
    }

    public Task ApplyAsync(HttpContext httpContext, CandidateSet candidates)
    {
        // Try to find cache entry for single candidate
        var firstCandidate = candidates[0];
        Endpoint? cachedEndpoint;
        if (candidates.Count == 1 && _endpointsCache.TryGetValue(firstCandidate.Endpoint, out cachedEndpoint))
        {
            // Only use the current request's route values if the candidate match is an actual endpoint
            var values = !MetadataOnlyEndpoint.IsMetadataOnlyEndpoint(firstCandidate.Endpoint)
                ? firstCandidate.Values
                : null;
            candidates.ReplaceEndpoint(0, cachedEndpoint, values);
            return Task.CompletedTask;
        }

        // Fallback to looping through all candiates
        Endpoint? firstMetadataOnlyEndpoint = null;
        // PERF: Use a list type optimized for small item counts instead
        List<Endpoint>? metadataOnlyEndpoints = null;
        var replacementCandidateIndex = -1;
        var realEndpointCandidateCount = 0;

        for (int i = 0; i < candidates.Count; i++)
        {
            var candidate = candidates[i];

            if (MetadataOnlyEndpoint.IsMetadataOnlyEndpoint(candidate.Endpoint))
            {
                if (firstMetadataOnlyEndpoint is null)
                {
                    firstMetadataOnlyEndpoint = candidate.Endpoint;
                }
                else
                {
                    if (metadataOnlyEndpoints is null)
                    {
                        metadataOnlyEndpoints = new List<Endpoint>
                        {
                            firstMetadataOnlyEndpoint
                        };
                    }
                    metadataOnlyEndpoints.Add(candidate.Endpoint);
                }
                if (realEndpointCandidateCount == 0 && replacementCandidateIndex == -1)
                {
                    // Only capture index of first metadata only endpoint as candidate replacement
                    replacementCandidateIndex = i;
                }
            }
            else
            {
                realEndpointCandidateCount++;
                if (realEndpointCandidateCount == 1)
                {
                    // Only first real endpoint is a candidate
                    replacementCandidateIndex = i;
                }
            }
        }

        Debug.Assert(firstMetadataOnlyEndpoint is not null);
        Debug.Assert(metadataOnlyEndpoints?.Count >= 1 || firstMetadataOnlyEndpoint is not null);
        Debug.Assert(replacementCandidateIndex >= 0);

        var activeCandidate = candidates[replacementCandidateIndex];
        var activeEndpoint = (RouteEndpoint)activeCandidate.Endpoint;

        // TODO: Review what the correct behavior is if there is more than 1 real endpoint candidate.

        if (realEndpointCandidateCount is 0 or 1 && activeEndpoint is not null)
        {
            Endpoint? replacementEndpoint = null;

            // Check cache for replacement endpoint
            if (!_endpointsCache.TryGetValue(activeEndpoint, out replacementEndpoint))
            {
                // Not found in cache so build up the replacement endpoint
                IReadOnlyList<object> decoratedMetadata = metadataOnlyEndpoints is not null
                    ? metadataOnlyEndpoints.SelectMany(e => e.Metadata).ToList()
                    : firstMetadataOnlyEndpoint.Metadata;

                if (realEndpointCandidateCount == 1)
                {
                    var routeEndpointBuilder = new RouteEndpointBuilder(activeEndpoint.RequestDelegate!, activeEndpoint.RoutePattern, activeEndpoint.Order);

                    routeEndpointBuilder.DisplayName = activeEndpoint.DisplayName;

                    // Add metadata from metadata-only endpoint candidates
                    foreach (var metadata in decoratedMetadata)
                    {
                        routeEndpointBuilder.Metadata.Add(metadata);
                    }

                    // Add metadata from active endpoint
                    if (realEndpointCandidateCount > 0)
                    {
                        foreach (var metadata in activeEndpoint.Metadata)
                        {
                            if (metadata is not null)
                            {
                                routeEndpointBuilder.Metadata.Add(metadata);
                            }
                        }
                    }

                    replacementEndpoint = routeEndpointBuilder.Build();
                }
                else
                {
                    replacementEndpoint = new MetadataOnlyEndpoint(activeEndpoint, decoratedMetadata);
                }

                _endpointsCache.Add(activeEndpoint, replacementEndpoint);
            }
            var values = realEndpointCandidateCount == 1 ? activeCandidate.Values : null;

            // Replace the endpoint
            candidates.ReplaceEndpoint(replacementCandidateIndex, replacementEndpoint, values);
        }

        return Task.CompletedTask;
    }
}
