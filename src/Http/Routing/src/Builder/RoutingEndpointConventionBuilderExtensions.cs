// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Routing;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods for adding routing metadata to endpoint instances using <see cref="IEndpointConventionBuilder"/>.
/// </summary>
public static class RoutingEndpointConventionBuilderExtensions
{
    /// <summary>
    /// Requires that endpoints match one of the specified hosts during routing.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/> to add the metadata to.</param>
    /// <param name="hosts">
    /// The hosts used during routing.
    /// Hosts should be Unicode rather than punycode, and may have a port.
    /// An empty collection means any host will be accepted.
    /// </param>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static TBuilder RequireHost<TBuilder>(this TBuilder builder, params string[] hosts) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(hosts);

        builder.Add(endpointBuilder =>
        {
            endpointBuilder.Metadata.Add(new HostAttribute(hosts));
        });
        return builder;
    }

    /// <summary>
    /// Sets the <see cref="EndpointBuilder.DisplayName"/> to the provided <paramref name="displayName"/> for all
    /// builders created by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="displayName">The display name.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static TBuilder WithDisplayName<TBuilder>(this TBuilder builder, string displayName) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Add(b =>
        {
            b.DisplayName = displayName;
        });

        return builder;
    }

    /// <summary>
    /// Sets the <see cref="EndpointBuilder.DisplayName"/> using the provided <paramref name="func"/> for all
    /// builders created by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="func">A delegate that produces the display name for each <see cref="EndpointBuilder"/>.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static TBuilder WithDisplayName<TBuilder>(this TBuilder builder, Func<EndpointBuilder, string> func) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(func);

        builder.Add(b =>
        {
            b.DisplayName = func(b);
        });

        return builder;
    }

    /// <summary>
    /// Adds the provided metadata <paramref name="items"/> to <see cref="EndpointBuilder.Metadata"/> for all builders
    /// produced by <paramref name="builder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="items">A collection of metadata items.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static TBuilder WithMetadata<TBuilder>(this TBuilder builder, params object[] items) where TBuilder : IEndpointConventionBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(items);

        builder.Add(b =>
        {
            foreach (var item in items)
            {
                b.Metadata.Add(item);
            }
        });

        return builder;
    }

    /// <summary>
    /// Adds the <see cref="IEndpointNameMetadata"/> to the Metadata collection for all endpoints produced
    /// on the target <see cref="IEndpointConventionBuilder"/> given the <paramref name="endpointName" />.
    /// The <see cref="IEndpointNameMetadata" /> on the endpoint is used for link generation and
    /// is treated as the operation ID in the given endpoint's OpenAPI specification.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="endpointName">The endpoint name.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static TBuilder WithName<TBuilder>(this TBuilder builder, string endpointName) where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new EndpointNameMetadata(endpointName), new RouteNameMetadata(endpointName));
        return builder;
    }

    /// <summary>
    /// Sets the <see cref="EndpointGroupNameAttribute"/> for all endpoints produced
    /// on the target <see cref="IEndpointConventionBuilder"/> given the <paramref name="endpointGroupName" />.
    /// The <see cref="IEndpointGroupNameMetadata" /> on the endpoint is used to set the endpoint's
    /// GroupName in the OpenAPI specification.
    /// </summary>
    /// <param name="builder">The <see cref="IEndpointConventionBuilder"/>.</param>
    /// <param name="endpointGroupName">The endpoint group name.</param>
    /// <returns>The <see cref="IEndpointConventionBuilder"/>.</returns>
    public static TBuilder WithGroupName<TBuilder>(this TBuilder builder, string endpointGroupName) where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new EndpointGroupNameAttribute(endpointGroupName));
        return builder;
    }
}
