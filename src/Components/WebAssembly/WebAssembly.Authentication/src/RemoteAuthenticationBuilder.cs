// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication
{
    internal class RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
        : IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>
        where TRemoteAuthenticationState : RemoteAuthenticationState
        where TAccount : RemoteUserAccount
    {
        public RemoteAuthenticationBuilder(IServiceCollection services) => Services = services;

        public IServiceCollection Services { get; }
    }
}
