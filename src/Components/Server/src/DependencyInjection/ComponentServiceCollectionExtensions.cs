// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.Server;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure an <see cref="IServiceCollection"/> for components.
/// </summary>
public static class ComponentServiceCollectionExtensions
{
    /// <summary>
    /// Adds Server-Side Blazor services to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">A callback to configure <see cref="CircuitOptions"/>.</param>
    /// <returns>An <see cref="IServerSideBlazorBuilder"/> that can be used to further customize the configuration.</returns>
    [RequiresUnreferencedCode("Server-side Blazor does not currently support native AOT.", Url = "https://aka.ms/aspnet/nativeaot")]
    public static IServerSideBlazorBuilder AddServerSideBlazor(this IServiceCollection services, Action<CircuitOptions>? configure = null)
    {
        services.AddRazorComponents().AddServerComponents();

        return new DefaultServerSideBlazorBuilder(services);
    }

    private sealed class DefaultServerSideBlazorBuilder : IServerSideBlazorBuilder
    {
        public DefaultServerSideBlazorBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
