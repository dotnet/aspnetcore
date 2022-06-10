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
    /// Constructs a new <see cref="RouteGroupContext"/> instance.
    /// </summary>
    /// <param name="prefix">The full group prefix. See <see cref="Prefix"/>.</param>
    /// <param name="conventions">All conventions added to a parent group. See <see cref="Conventions"/>.</param>
    /// <param name="applicationServices">Application services. See <see cref="ApplicationServices"/>.</param>
    public RouteGroupContext(RoutePattern prefix, IReadOnlyList<Action<EndpointBuilder>> conventions, IServiceProvider applicationServices)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        ArgumentNullException.ThrowIfNull(conventions);
        ArgumentNullException.ThrowIfNull(applicationServices);

        Prefix = prefix;
        Conventions = conventions;
        ApplicationServices = applicationServices;
    }

    /// <summary>
    /// Gets the prefix for all the <see cref="RouteEndpoint.RoutePattern"/> on all <see cref="RouteEndpoint"/> instances returned by the call to <see cref="EndpointDataSource.GetGroupedEndpoints(RouteGroupContext)"/>.
    /// This accounts for nested groups and gives the full group prefix, not just the prefix supplied to the innermost call to <see cref="EndpointRouteBuilderExtensions.MapGroup(IEndpointRouteBuilder, RoutePattern)"/>.
    /// </summary>
    public RoutePattern Prefix { get; }

    /// <summary>
    /// Gets all conventions added to a parent <see cref="RouteGroupBuilder"/> via <see cref="IEndpointConventionBuilder.Add(Action{EndpointBuilder})"/>.
    /// </summary>
    public IReadOnlyList<Action<EndpointBuilder>> Conventions { get; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> instance used to access application services.
    /// </summary>
    public IServiceProvider ApplicationServices { get; }
}
