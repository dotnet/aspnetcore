// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;

namespace Microsoft.AspNetCore.StaticAssets;

/// <summary>
/// A builder for configuring conventions for static assets.
/// </summary>
public sealed class StaticAssetsEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly object _lck;
    private readonly List<StaticAssetDescriptor> _descriptors;
    private readonly List<Action<EndpointBuilder>> _conventions;
    private readonly List<Action<EndpointBuilder>> _finallyConventions;

    internal StaticAssetsEndpointConventionBuilder(object lck, List<StaticAssetDescriptor> descriptors, List<Action<EndpointBuilder>> conventions, List<Action<EndpointBuilder>> finallyConventions)
    {
        _lck = lck;
        _descriptors = descriptors;
        _conventions = conventions;
        _finallyConventions = finallyConventions;
    }

    internal List<StaticAssetDescriptor> Descriptors => _descriptors;

    /// <inheritdoc/>
    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        lock (_lck)
        {
            _conventions.Add(convention);
        }
    }

    /// <inheritdoc/>
    public void Finally(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        lock (_lck)
        {
            _finallyConventions.Add(convention);
        }
    }
}
