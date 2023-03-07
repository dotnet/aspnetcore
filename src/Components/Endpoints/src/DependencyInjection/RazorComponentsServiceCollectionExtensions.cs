// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 
/// </summary>
public static class RazorComponentsServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IRazorComponentsBuilder AddRazorComponents(this IServiceCollection services)
    {
        services.TryAddSingleton<RazorComponentsMarkerService>();

        // Routing
        // This can't be a singleton
        // https://github.com/dotnet/aspnetcore/issues/46980
        services.TryAddSingleton<RazorComponentEndpointDataSource>();

        // TODO: Register common services required for server side rendering

        return new DefaultRazorcomponentsBuilder(services);
    }

    private sealed class DefaultRazorcomponentsBuilder : IRazorComponentsBuilder
    {
        public DefaultRazorcomponentsBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
