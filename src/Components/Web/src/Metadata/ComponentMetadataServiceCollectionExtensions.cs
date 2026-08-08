// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for registering compile-time Razor components metadata.
/// </summary>
[Experimental("ASPNETCORE9004", UrlFormat = "https://aka.ms/aspnet/analyzer/{0}")]
public static class ComponentMetadataServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="RazorComponentsMetadataContext"/> so that the framework consults its
    /// compile-time descriptions before falling back to reflection.
    /// </summary>
    /// <typeparam name="TContext">The generated metadata context type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <remarks>
    /// Calling this more than once composes the contexts by concatenation, so a component library can
    /// contribute its own metadata alongside the application's.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddComponentMetadata&lt;AppMetadata&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddComponentMetadata<TContext>(this IServiceCollection services)
        where TContext : RazorComponentsMetadataContext, new()
    {
        ArgumentNullException.ThrowIfNull(services);

        var context = new TContext();

        services.Configure<ComponentMetadataOptions>(options =>
        {
            foreach (var component in context.Components)
            {
                options.Components.Add(component);
            }

            foreach (var bindableType in context.BindableTypes)
            {
                options.BindableTypes.Add(bindableType);
            }

            foreach (var method in context.JSInvokableMethods)
            {
                options.JSInvokableMethods.Add(method);
            }

            if (context.JsonTypeInfoResolver is { } resolver)
            {
                options.JsonTypeInfoResolvers.Add(resolver);
            }
        });

        // Persisted state is serialized through options the framework owns statically, so the
        // contracts are contributed here rather than resolved from the container.
        PersistentStateSerializationOptions.AddResolver(WebPersistentStateJsonContext.Default);
        if (context.JsonTypeInfoResolver is { } persistentStateResolver)
        {
            PersistentStateSerializationOptions.AddResolver(persistentStateResolver);
            ComponentMarkerJsonTypeInfoResolver.Instance.AddResolver(persistentStateResolver);
        }

        services.TryAddSingleton<ComponentMetadataResolver>();
        services.TryAddSingleton<IComponentMetadataResolver>(sp => sp.GetRequiredService<ComponentMetadataResolver>());
        services.TryAddSingleton<IComponentTypeInfoResolver>(ComponentTypeInfoResolverFactory.Create);
        services.TryAddSingleton<IBindableTypeResolver>(sp => sp.GetRequiredService<ComponentMetadataResolver>());
        services.TryAddSingleton<IComponentJsonMetadataResolver>(sp => sp.GetRequiredService<ComponentMetadataResolver>());

        // Keep contexts enumerable for consumers of the public metadata model.
        services.AddSingleton<RazorComponentsMetadataContext>(context);

        return services;
    }
}
