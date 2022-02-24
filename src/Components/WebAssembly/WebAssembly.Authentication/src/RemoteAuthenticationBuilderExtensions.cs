// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions for remote authentication services.
/// </summary>
public static class RemoteAuthenticationBuilderExtensions
{
    /// <summary>
    /// Replaces the existing <see cref="AccountClaimsPrincipalFactory{TAccount}"/> with the user factory defined by <typeparamref name="TAccountClaimsPrincipalFactory"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The remote authentication state.</typeparam>
    /// <typeparam name="TAccount">The account type.</typeparam>
    /// <typeparam name="TAccountClaimsPrincipalFactory">The new user factory type.</typeparam>
    /// <param name="builder">The <see cref="IRemoteAuthenticationBuilder{TRemoteAuthenticationState, TAccount}"/>.</param>
    /// <returns>The <see cref="IRemoteAuthenticationBuilder{TRemoteAuthenticationState, TAccount}"/>.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddAccountClaimsPrincipalFactory<TRemoteAuthenticationState, TAccount, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccountClaimsPrincipalFactory>(
        this IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> builder)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccount : RemoteUserAccount
        where TAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<TAccount>
    {
        builder.Services.Replace(ServiceDescriptor.Scoped<AccountClaimsPrincipalFactory<TAccount>, TAccountClaimsPrincipalFactory>());

        return builder;
    }

    /// <summary>
    /// Replaces the existing <see cref="AccountClaimsPrincipalFactory{Account}"/> with the user factory defined by <typeparamref name="TAccountClaimsPrincipalFactory"/>.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The remote authentication state.</typeparam>
    /// <typeparam name="TAccountClaimsPrincipalFactory">The new user factory type.</typeparam>
    /// <param name="builder">The <see cref="IRemoteAuthenticationBuilder{TRemoteAuthenticationState, Account}"/>.</param>
    /// <returns>The <see cref="IRemoteAuthenticationBuilder{TRemoteAuthenticationState, Account}"/>.</returns>
    public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddAccountClaimsPrincipalFactory<TRemoteAuthenticationState, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccountClaimsPrincipalFactory>(
        this IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> builder)
        where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        where TAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount> => builder.AddAccountClaimsPrincipalFactory<TRemoteAuthenticationState, RemoteUserAccount, TAccountClaimsPrincipalFactory>();

    /// <summary>
    /// Replaces the existing <see cref="AccountClaimsPrincipalFactory{TAccount}"/> with the user factory defined by <typeparamref name="TAccountClaimsPrincipalFactory"/>.
    /// </summary>
    /// <typeparam name="TAccountClaimsPrincipalFactory">The new user factory type.</typeparam>
    /// <param name="builder">The <see cref="IRemoteAuthenticationBuilder{RemoteAuthenticationState, Account}"/>.</param>
    /// <returns>The <see cref="IRemoteAuthenticationBuilder{RemoteAuthenticationState, Account}"/>.</returns>
    public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddAccountClaimsPrincipalFactory<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TAccountClaimsPrincipalFactory>(
        this IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> builder)
        where TAccountClaimsPrincipalFactory : AccountClaimsPrincipalFactory<RemoteUserAccount> => builder.AddAccountClaimsPrincipalFactory<RemoteAuthenticationState, RemoteUserAccount, TAccountClaimsPrincipalFactory>();
}
