// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Components.Server.Circuits;

/// <summary>
/// Default configuration for <see cref="CircuitOptions.HybridPersistenceCache"/>.
/// </summary>
internal sealed class DefaultHybridCache : IPostConfigureOptions<CircuitOptions>
{
    private readonly HybridCache? _hybridCache;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultHybridCache"/>.
    /// </summary>
    /// <param name="hybridCache">The <see cref="HybridCache"/> service, if available.</param>
    public DefaultHybridCache(HybridCache? hybridCache = null)
    {
        _hybridCache = hybridCache;
    }

    /// <inheritdoc />
    public void PostConfigure(string? name, CircuitOptions options)
    {
        // Only set the HybridPersistenceCache if it hasn't been explicitly configured
        // and a HybridCache service is available
        if (options.HybridPersistenceCache is null && _hybridCache is not null)
        {
            options.HybridPersistenceCache = _hybridCache;
        }
    }
}
