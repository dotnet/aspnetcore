// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to add authentication to Blazor WebAssembly applications.
/// </summary>
public static class WebAssemblyAuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Adds an <see cref="AuthenticationStateProvider"/> where the <see cref="AuthenticationState"/> is deserialized from the server
    /// using <see cref="AuthenticationStateData"/> and <see cref="PersistentComponentState"/>. There should be a corresponding call to
    /// AddAuthenticationStateSerialization from the Microsoft.AspNetCore.Components.WebAssembly.Server package in the server project.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="AuthenticationStateDeserializationOptions"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddAuthenticationStateDeserialization(this IServiceCollection services, Action<AuthenticationStateDeserializationOptions>? configure = null)
    {
        services.AddOptions();
        services.TryAddScoped<AuthenticationStateProvider, DeserializedAuthenticationStateProvider>();
        if (configure != null)
        {
            services.Configure(configure);
        }

        return services;
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using the given <typeparamref name="TProviderOptions"/> and
    /// <typeparamref name="TRemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The state to be persisted across authentication operations.</typeparam>
    /// <typeparam name="TAccount">The account type.</typeparam>
    /// <typeparam name="TProviderOptions">The configuration options of the underlying provider being used for handling the authentication operations.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddRemoteAuthentication<
        [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState,
        [DynamicallyAccessedMembers(JsonSerialized)] TAccount,
        [DynamicallyAccessedMembers(JsonSerialized)] TProviderOptions>(
        this IServiceCollection services)
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TAccount : RemoteUserAccount
        where TProviderOptions : class, new()
    {
        services.AddOptions();
        services.AddAuthorizationCore();
        services.TryAddScoped<AuthenticationStateProvider, RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>>();
        AddAuthenticationStateProvider<TRemoteAuthenticationState>(services);

        services.TryAddTransient<BaseAddressAuthorizationMessageHandler>();
        services.TryAddTransient<AuthorizationMessageHandler>();

        services.TryAddScoped(sp =>
        {
            return (IAccessTokenProvider)sp.GetRequiredService<AuthenticationStateProvider>();
        });

        services.TryAddScoped<IRemoteAuthenticationPathsProvider, DefaultRemoteApplicationPathsProvider<TProviderOptions>>();
        services.TryAddScoped<IAccessTokenProviderAccessor, AccessTokenProviderAccessor>();
#pragma warning disable CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility
        services.TryAddScoped<SignOutSessionStateManager>();
#pragma warning restore CS0618 // Type or member is obsolete, we keep it for now for backwards compatibility

        services.TryAddScoped<AccountClaimsPrincipalFactory<TAccount>>();

        return new RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>(services);
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2091", Justification = "The calling method enforces the dynamically accessed members constraints.")]
    private static void AddAuthenticationStateProvider<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(IServiceCollection services) where TRemoteAuthenticationState : RemoteAuthenticationState
    {
        services.TryAddScoped(static sp => (IRemoteAuthenticationService<TRemoteAuthenticationState>)sp.GetRequiredService<AuthenticationStateProvider>());
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using the given <typeparamref name="TProviderOptions"/> and
    /// <typeparamref name="TRemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The state to be persisted across authentication operations.</typeparam>
    /// <typeparam name="TAccount">The account type.</typeparam>
    /// <typeparam name="TProviderOptions">The configuration options of the underlying provider being used for handling the authentication operations.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddRemoteAuthentication<
        [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount, [DynamicallyAccessedMembers(JsonSerialized)] TProviderOptions>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<TProviderOptions>>? configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TAccount : RemoteUserAccount
        where TProviderOptions : class, new()
    {
        services.AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, TProviderOptions>();
        if (configure != null)
        {
            services.Configure(configure);
        }

        return new RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>(services);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="OidcProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddOidcAuthentication(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
    {
        return AddOidcAuthentication<RemoteAuthenticationState>(services, configure);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="OidcProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddOidcAuthentication<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    {
        return AddOidcAuthentication<TRemoteAuthenticationState, RemoteUserAccount>(services, configure);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="OidcProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <typeparam name="TAccount">The account type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddOidcAuthentication<
        [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
    {
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPostConfigureOptions<RemoteAuthenticationOptions<OidcProviderOptions>>, DefaultOidcOptionsConfiguration>());

        return AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, OidcProviderOptions>(services, configure);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization(this IServiceCollection services)
    {
        return AddApiAuthorizationCore<RemoteAuthenticationState, RemoteUserAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name!);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(this IServiceCollection services)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    {
        return AddApiAuthorizationCore<TRemoteAuthenticationState, RemoteUserAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name!);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <typeparam name="TAccount">The account type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiAuthorization<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        this IServiceCollection services)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
    {
        return AddApiAuthorizationCore<TRemoteAuthenticationState, TAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name!);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
    {
        return AddApiAuthorizationCore<RemoteAuthenticationState, RemoteUserAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name!);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    {
        return AddApiAuthorizationCore<TRemoteAuthenticationState, RemoteUserAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name!);
    }

    /// <summary>
    /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <typeparam name="TAccount">The account type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiAuthorization<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
    {
        return AddApiAuthorizationCore<TRemoteAuthenticationState, TAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name!);
    }

    private static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiAuthorizationCore<[DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        IServiceCollection services,
        Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>? configure,
        string inferredClientId)
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TAccount : RemoteUserAccount
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Scoped<IPostConfigureOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>, DefaultApiAuthorizationOptionsConfiguration>(_ =>
            new DefaultApiAuthorizationOptionsConfiguration(inferredClientId)));

        services.AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, ApiAuthorizationProviderOptions>(configure);

        return new RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>(services);
    }
}
