// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
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
        services.AddRoutingCore();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<RouteOptions>, RegexInlineRouteConstraintSetup>());
        return services;
    }

    /// <summary>
    /// Adds services required for routing requests. This is similar to
    /// <see cref="AddRouting(IServiceCollection)" /> except that it
    /// excludes certain options that can be opted in separately, if needed.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddRoutingCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Required for IMeterFactory dependency.
        services.AddMetrics();

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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<MatcherPolicy, ContentEncodingNegotiationMatcherPolicy>());

        //
        // Misc infrastructure
        //
        services.TryAddSingleton<TemplateBinderFactory, DefaultTemplateBinderFactory>();
        services.TryAddSingleton<RoutePatternTransformer, DefaultRoutePatternTransformer>();
        services.TryAddSingleton<RoutingMetrics>();

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
