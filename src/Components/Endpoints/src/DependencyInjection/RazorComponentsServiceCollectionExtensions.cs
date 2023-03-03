// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        services.AddSingleton<RazorComponentsMarkerService>();
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
