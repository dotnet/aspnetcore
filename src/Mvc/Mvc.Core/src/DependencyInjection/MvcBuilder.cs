// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc.ApplicationParts;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Allows fine grained configuration of MVC services.
/// </summary>
internal sealed class MvcBuilder : IMvcBuilder
{
    /// <summary>
    /// Initializes a new <see cref="MvcBuilder"/> instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to add services to.</param>
    /// <param name="manager">The <see cref="ApplicationPartManager"/> of the application.</param>
    public MvcBuilder(IServiceCollection services, ApplicationPartManager manager)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(manager);

        Services = services;
        PartManager = manager;
    }

    /// <inheritdoc />
    public IServiceCollection Services { get; }

    /// <inheritdoc />
    public ApplicationPartManager PartManager { get; }
}
