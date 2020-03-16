// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Authentication.WebAssembly.Msal;
using Microsoft.Authentication.WebAssembly.Msal.Models;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
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
        /// <param name="configure">The <see cref="Action{RemoteAuthenticationOptions{MsalProviderOptions}}"/> to configure the <see cref="RemoteAuthenticationOptions{MsalProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMsalAuthentication(this IServiceCollection services, Action<RemoteAuthenticationOptions<MsalProviderOptions>> configure)
        {
            return AddMsalAuthentication<RemoteAuthenticationState>(services, configure);
        }

        /// <summary>
        /// Adds authentication using msal.js to Blazor applications.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The <see cref="Action{RemoteAuthenticationOptions{MsalProviderOptions}}"/> to configure the <see cref="RemoteAuthenticationOptions{MsalProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddMsalAuthentication<TRemoteAuthenticationState>(this IServiceCollection services, Action<RemoteAuthenticationOptions<MsalProviderOptions>> configure)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        {
            services.AddRemoteAuthentication<RemoteAuthenticationState, MsalProviderOptions>();
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RemoteAuthenticationOptions<MsalProviderOptions>>, MsalDefaultOptionsConfiguration>());

            return services;
        }
    }
}
