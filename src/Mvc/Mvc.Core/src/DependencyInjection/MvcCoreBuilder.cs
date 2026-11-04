// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Allows fine grained configuration of essential MVC services.
/// </summary>
internal sealed class MvcCoreBuilder : IMvcCoreBuilder
{
    /// <summary>
    /// Initializes a new <see cref="MvcCoreBuilder"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="manager">The <see cref="ApplicationPartManager"/> of the application.</param>
    public MvcCoreBuilder(
        IServiceCollection services,
        ApplicationPartManager manager)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(manager);

        Services = services;
        PartManager = manager;
    }

    /// <inheritdoc />
    public ApplicationPartManager PartManager { get; }

    /// <inheritdoc />
    public IServiceCollection Services { get; }
}
