// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// An interface for configuring remote authentication services.
/// </summary>
/// <typeparam name="TRemoteAuthenticationState">The remote authentication state type.</typeparam>
/// <typeparam name="TAccount">The account type.</typeparam>
public interface IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TAccount : RemoteUserAccount
{
    /// <summary>
    /// The <see cref="IServiceCollection"/>.
    /// </summary>
    IServiceCollection Services { get; }
}
