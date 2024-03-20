// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Extensions.Caching.Distributed;

public interface IHybridCacheBuilder
{
    IServiceCollection Services { get; }
}

internal class HybridCacheBuilder(IServiceCollection services) : IHybridCacheBuilder
{
    public IServiceCollection Services { get; } = services;
}
