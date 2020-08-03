// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// An interface for configuring remote authentication services.
    /// </summary>
    /// <typeparam name="TRemoteAuthenticationState">The remote authentication state type.</typeparam>
    /// <typeparam name="TAccount">The account type.</typeparam>
    public interface IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TAccount : RemoteUserAccount
    {
        IServiceCollection Services { get; }
    }
}
