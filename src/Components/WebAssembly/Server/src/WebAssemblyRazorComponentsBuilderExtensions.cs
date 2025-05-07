// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Endpoints.Infrastructure;
using Microsoft.AspNetCore.Components.WebAssembly.Server;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods to configure an <see cref="IServiceCollection"/> for WebAssembly components.
/// </summary>
public static class WebAssemblyRazorComponentsBuilderExtensions
{
    private const string LazyAssemblyLoaderAssemblyName = "Microsoft.AspNetCore.Components.WebAssembly";
    private const string LazyAssemblyLoaderTypeName = "Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader";

    /// <summary>
    /// Adds services to support rendering interactive WebAssembly components.
    /// </summary>
    /// <param name="builder">The <see cref="IRazorComponentsBuilder"/>.</param>
    /// <returns>An <see cref="IRazorComponentsBuilder"/> that can be used to further customize the configuration.</returns>
    public static IRazorComponentsBuilder AddInteractiveWebAssemblyComponents(this IRazorComponentsBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder, nameof(builder));

        builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<RenderModeEndpointProvider, WebAssemblyEndpointProvider>());

        // Try register LazyAssemblyLoader to prevent crashes during prerendering.
        // TODO: Remove this once LazyAssemblyLoader is no longer used.
        var lazyAssemblyLoaderType = GetLazyAssemblyLoaderType();

        if (lazyAssemblyLoaderType != null)
        {
            builder.Services.TryAddScoped(lazyAssemblyLoaderType);
        }

        return builder;
    }

    /// <summary>
    /// Serializes the <see cref="AuthenticationState"/> returned by the server-side <see cref="AuthenticationStateProvider"/> using <see cref="PersistentComponentState"/>
    /// for use by interactive WebAssembly components via a deserializing client-side <see cref="AuthenticationStateProvider"/> which can be added by calling
    /// AddAuthenticationStateDeserialization from the Microsoft.AspNetCore.Components.WebAssembly.Authentication package in the client project.
    /// </summary>
    /// <param name="builder">The <see cref="IRazorComponentsBuilder"/>.</param>
    /// <param name="configure">A callback to customize the serialization of the <see cref="AuthenticationState"/>.</param>
    /// <returns>An <see cref="IRazorComponentsBuilder"/> that can be used to further customize the configuration.</returns>
    public static IRazorComponentsBuilder AddAuthenticationStateSerialization(this IRazorComponentsBuilder builder, Action<AuthenticationStateSerializationOptions>? configure = null)
    {
        builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<IHostEnvironmentAuthenticationStateProvider, AuthenticationStateSerializer>());
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }

        return builder;
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors, LazyAssemblyLoaderTypeName, LazyAssemblyLoaderAssemblyName)]
    private static Type? GetLazyAssemblyLoaderType()
    {
        try
        {
            var assembly = Assembly.Load(LazyAssemblyLoaderAssemblyName);
            return assembly.GetType(LazyAssemblyLoaderTypeName, throwOnError: false);
        }
        catch
        {
            return null;
        }
    }
}
