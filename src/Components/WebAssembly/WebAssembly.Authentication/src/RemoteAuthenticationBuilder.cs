// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

internal sealed class RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
    : IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
    where TRemoteAuthenticationState : RemoteAuthenticationState
    where TAccount : RemoteUserAccount
{
    public RemoteAuthenticationBuilder(IServiceCollection services) => Services = services;

    public IServiceCollection Services { get; }
}
