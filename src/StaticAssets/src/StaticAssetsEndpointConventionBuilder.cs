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
    private readonly bool _isBuildManifest;
    private readonly List<StaticAssetDescriptor> _descriptors;
    private readonly List<Action<EndpointBuilder>> _conventions;
    private readonly List<Action<EndpointBuilder>> _finallyConventions;
    private IEndpointConventionBuilder[] _fallbacks = [];

    internal StaticAssetsEndpointConventionBuilder(object lck, bool isBuildManifest, List<StaticAssetDescriptor> descriptors, List<Action<EndpointBuilder>> conventions, List<Action<EndpointBuilder>> finallyConventions)
    {
        _lck = lck;
        _isBuildManifest = isBuildManifest;
        _descriptors = descriptors;
        _conventions = conventions;
        _finallyConventions = finallyConventions;
    }

    internal List<StaticAssetDescriptor> Descriptors => _descriptors;

    internal bool IsBuildManifest => _isBuildManifest;

    // Conventions added after this call are also applied to the runtime fallback endpoint,
    // so that it is configured the same way as the endpoints from the manifest.
    internal void AttachFallback(IEndpointConventionBuilder fallback)
    {
        lock (_lck)
        {
            _fallbacks = [.. _fallbacks, fallback];
        }
    }

    /// <inheritdoc/>
    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        IEndpointConventionBuilder[] fallbacks;
        lock (_lck)
        {
            _conventions.Add(convention);
            fallbacks = _fallbacks;
        }

        foreach (var fallback in fallbacks)
        {
            fallback.Add(convention);
        }
    }

    /// <inheritdoc/>
    public void Finally(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        IEndpointConventionBuilder[] fallbacks;
        lock (_lck)
        {
            _finallyConventions.Add(convention);
            fallbacks = _fallbacks;
        }

        foreach (var fallback in fallbacks)
        {
            fallback.Finally(convention);
        }
    }
}
