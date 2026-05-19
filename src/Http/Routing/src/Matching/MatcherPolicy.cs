// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Matching;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Defines a policy that applies behaviors to the URL matcher. Implementations
/// of <see cref="MatcherPolicy"/> and related interfaces must be registered
/// in the dependency injection container as singleton services of type
/// <see cref="MatcherPolicy"/>.
/// </summary>
/// <remarks>
/// <see cref="MatcherPolicy"/> implementations can implement the following
/// interfaces <see cref="IEndpointComparerPolicy"/>, <see cref="IEndpointSelectorPolicy"/>,
/// and <see cref="INodeBuilderPolicy"/>.
/// </remarks>
public abstract class MatcherPolicy
{
    /// <summary>
    /// Gets a value that determines the order the <see cref="MatcherPolicy"/> should
    /// be applied. Policies are applied in ascending numeric value of the <see cref="Order"/>
    /// property.
    /// </summary>
    public abstract int Order { get; }

    /// <summary>
    /// Returns a value that indicates whether the provided <paramref name="endpoints"/> contains
    /// one or more dynamic endpoints.
    /// </summary>
    /// <param name="endpoints">The set of endpoints.</param>
    /// <returns><c>true</c> if a dynamic endpoint is found; otherwise returns <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// The presence of <see cref="IDynamicEndpointMetadata"/> signifies that an endpoint that may be replaced
    /// during processing by an <see cref="IEndpointSelectorPolicy"/>.
    /// </para>
    /// <para>
    /// An implementation of <see cref="INodeBuilderPolicy"/> should also implement <see cref="IEndpointSelectorPolicy"/>
    /// and use its <see cref="IEndpointSelectorPolicy"/> implementation when a node contains a dynamic endpoint.
    /// <see cref="INodeBuilderPolicy"/> implementations rely on caching of data based on a static set of endpoints. This
    /// is not possible when endpoints are replaced dynamically.
    /// </para>
    /// </remarks>
    protected static bool ContainsDynamicEndpoints(IReadOnlyList<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        for (var i = 0; i < endpoints.Count; i++)
        {
            var metadata = endpoints[i].Metadata.GetMetadata<IDynamicEndpointMetadata>();
            if (metadata?.IsDynamic == true)
            {
                return true;
            }
        }

        return false;
    }
}
