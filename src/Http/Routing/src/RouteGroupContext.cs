// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Represents the information accessible to <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>.
/// </summary>
public sealed class RouteGroupContext
{
    /// <summary>
    /// Gets the <see cref="RouteEndpoint.RoutePattern"/> which should prefix the <see cref="RouteEndpoint.RoutePattern"/> of all <see cref="RouteEndpoint"/> instances
    /// returned by the call to <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>. This accounts for nested groups and gives the full group prefix
    /// not just the prefix supplied to the innermost call to <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, RoutePattern)"/>.
    /// </summary>
    public required RoutePattern Prefix { get; init; }

    /// <summary>
    /// Gets all conventions added to ancestor <see cref="RouteGroupBuilder"/> instances returned from <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, RoutePattern)"/>
    /// via <see cref="IEndpointConventionBuilder.Add(Action{EndpointBuilder})"/>. These should be applied in order when building every <see cref="RouteEndpoint"/>
    /// returned from <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>.
    /// </summary>
    public IReadOnlyList<Action<EndpointBuilder>> Conventions { get; init; } = Array.Empty<Action<EndpointBuilder>>();

    /// <summary>
    /// Gets all conventions added to ancestor <see cref="RouteGroupBuilder"/> instances returned from <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, RoutePattern)"/>
    /// via <see cref="IEndpointConventionBuilder.Add(Action{EndpointBuilder})"/>. These should be applied in LIFO order when building every <see cref="RouteEndpoint"/>
    /// returned from <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>.
    /// </summary>
    public IReadOnlyList<Action<EndpointBuilder>> FinallyConventions { get; init; } = Array.Empty<Action<EndpointBuilder>>();

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance used to access application services.
    /// </summary>
    public IServiceProvider ApplicationServices { get; init; } = EmptyServiceProvider.Instance;

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static EmptyServiceProvider Instance { get; } = new EmptyServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
