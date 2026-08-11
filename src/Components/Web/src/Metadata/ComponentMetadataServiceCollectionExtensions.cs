// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering compile-time Razor components metadata.
/// </summary>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public static class ComponentMetadataServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="RazorComponentsMetadataContext"/> so the framework consults its JSON
    /// contracts before falling back to reflection.
    /// </summary>
    /// <typeparam name="TContext">The metadata context type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so additional calls can be chained.</returns>
    /// <example>
    /// <code>
    /// builder.Services.AddComponentMetadata&lt;AppMetadata&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddComponentMetadata<TContext>(this IServiceCollection services)
        where TContext : RazorComponentsMetadataContext, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddComponentJsonMetadata();
        var context = new TContext();
        if (context.JsonTypeInfoResolver is { } resolver)
        {
            services.Configure<ComponentJsonMetadataOptions>(options => options.Resolvers.Add(resolver));
        }
        services.AddSingleton<RazorComponentsMetadataContext>(context);

        return services;
    }

    internal static IServiceCollection AddComponentJsonMetadata(this IServiceCollection services)
    {
        services.AddOptions<ComponentJsonMetadataOptions>();
        services.TryAddSingleton<ComponentJsonMetadataResolver>();
        services.TryAddSingleton<IComponentJsonMetadataResolver>(
            static services => services.GetRequiredService<ComponentJsonMetadataResolver>());
        return services;
    }
}
