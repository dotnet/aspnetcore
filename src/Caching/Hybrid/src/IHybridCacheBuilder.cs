// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Hybrid;

/// <summary>
/// Helper API for configuring <see cref="HybridCache"/>.
/// </summary>
public interface IHybridCacheBuilder
{
    /// <summary>
    /// Gets the services collection associated with this instance.
    /// </summary>
    IServiceCollection Services { get; }
}

internal sealed class HybridCacheBuilder(IServiceCollection services) : IHybridCacheBuilder
{
    public IServiceCollection Services { get; } = services;
}
