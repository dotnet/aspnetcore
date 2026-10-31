// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Microsoft.AspNetCore.Routing;

/// <summary>
/// Provides a collection of <see cref="Endpoint"/> instances.
/// </summary>
[DebuggerDisplay("{DebuggerDisplayString,nq}")]
public sealed class DefaultEndpointDataSource : EndpointDataSource
{
    private readonly IReadOnlyList<Endpoint> _endpoints;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEndpointDataSource" /> class.
    /// </summary>
    /// <param name="endpoints">The <see cref="Endpoint"/> instances that the data source will return.</param>
    public DefaultEndpointDataSource(params Endpoint[] endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _endpoints = (Endpoint[])endpoints.Clone();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultEndpointDataSource" /> class.
    /// </summary>
    /// <param name="endpoints">The <see cref="Endpoint"/> instances that the data source will return.</param>
    public DefaultEndpointDataSource(IEnumerable<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        _endpoints = new List<Endpoint>(endpoints);
    }

    /// <summary>
    /// Gets a <see cref="IChangeToken"/> used to signal invalidation of cached <see cref="Endpoint"/>
    /// instances.
    /// </summary>
    /// <returns>The <see cref="IChangeToken"/>.</returns>
    public override IChangeToken GetChangeToken() => NullChangeToken.Singleton;

    /// <summary>
    /// Returns a read-only collection of <see cref="Endpoint"/> instances.
    /// </summary>
    public override IReadOnlyList<Endpoint> Endpoints => _endpoints;

    private string DebuggerDisplayString => GetDebuggerDisplayStringForEndpoints(_endpoints);
}
