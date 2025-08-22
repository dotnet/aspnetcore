// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Builds conventions that will be used for customization of <see cref="EndpointBuilder"/> instances.
/// </summary>
/// <remarks>
/// This interface is used at application startup to customize endpoints for the application.
/// </remarks>
public sealed class ControllerActionEndpointConventionBuilder : IEndpointConventionBuilder
{
    // The lock is shared with the data source.
    private readonly object _lock;
    private readonly List<Action<EndpointBuilder>> _conventions;
    private readonly List<Action<EndpointBuilder>> _finallyConventions;

    internal ControllerActionEndpointConventionBuilder(object @lock, List<Action<EndpointBuilder>> conventions, List<Action<EndpointBuilder>> finallyConventions)
    {
        _lock = @lock;
        _conventions = conventions;
        _finallyConventions = finallyConventions;
    }

    internal Dictionary<string, object> Items { get; set; } = [];

    /// <summary>
    /// Adds the specified convention to the builder. Conventions are used to customize <see cref="EndpointBuilder"/> instances.
    /// </summary>
    /// <param name="convention">The convention to add to the builder.</param>
    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        // The lock is shared with the data source. We want to lock here
        // to avoid mutating this list while its read in the data source.
        lock (_lock)
        {
            _conventions.Add(convention);
        }
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        ArgumentNullException.ThrowIfNull(finalConvention);

        lock (_lock)
        {
            _finallyConventions.Add(finalConvention);
        };
    }
}
