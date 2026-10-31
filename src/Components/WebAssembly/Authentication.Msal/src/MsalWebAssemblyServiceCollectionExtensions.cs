// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Authentication.WebAssembly.Msal;
using Microsoft.Authentication.WebAssembly.Msal.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Internal.LinkerFlags;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Contains extension methods to add authentication to Blazor WebAssembly applications using
/// Azure Active Directory or Azure Active Directory B2C.
/// </summary>
public static class MsalWebAssemblyServiceCollectionExtensions
{
    /// <summary>
    /// Adds authentication using msal.js to Blazor applications.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">A callback to configure the <see cref="RemoteAuthenticationOptions{MsalProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddMsalAuthentication(this IServiceCollection services, Action<RemoteAuthenticationOptions<MsalProviderOptions>> configure)
    {
        return AddMsalAuthentication<RemoteAuthenticationState>(services, configure);
    }

    /// <summary>
    /// Adds authentication using msal.js to Blazor applications.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">A callback to configure the <see cref="RemoteAuthenticationOptions{MsalProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddMsalAuthentication<
    [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<MsalProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    {
        return AddMsalAuthentication<TRemoteAuthenticationState, RemoteUserAccount>(services, configure);
    }

    /// <summary>
    /// Adds authentication using msal.js to Blazor applications.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
    /// <typeparam name="TAccount">The type of the <see cref="RemoteUserAccount"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">A callback to configure the <see cref="RemoteAuthenticationOptions{MsalProviderOptions}"/>.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddMsalAuthentication<
        [DynamicallyAccessedMembers(JsonSerialized)] TRemoteAuthenticationState, [DynamicallyAccessedMembers(JsonSerialized)] TAccount>(
        this IServiceCollection services, Action<RemoteAuthenticationOptions<MsalProviderOptions>> configure)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
    {
        services.AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, MsalProviderOptions>(configure);
        services.TryAddEnumerable(ServiceDescriptor.Scoped<IPostConfigureOptions<RemoteAuthenticationOptions<MsalProviderOptions>>, MsalDefaultOptionsConfiguration>());

        return new MsalRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>(services);
    }
}

internal sealed class MsalRemoteAuthenticationBuilder<TRemoteAuthenticationState, TRemoteUserAccount> : IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TRemoteUserAccount>
    where TRemoteAuthenticationState : RemoteAuthenticationState, new()
    where TRemoteUserAccount : RemoteUserAccount
{

    public MsalRemoteAuthenticationBuilder(IServiceCollection services) => Services = services;

    public IServiceCollection Services { get; }
}
