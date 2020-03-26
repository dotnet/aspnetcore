// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication.Internal;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Contains extension methods to add authentication to Blazor WebAssembly applications.
    /// </summary>
    public static class WebAssemblyAuthenticationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds support for authentication for SPA applications using the given <typeparamref name="TProviderOptions"/> and
        /// <typeparamref name="TRemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The state to be persisted across authentication operations.</typeparam>
        /// <typeparam name="TProviderOptions">The configuration options of the underlying provider being used for handling the authentication operations.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddRemoteAuthentication<TRemoteAuthenticationState, TProviderOptions>(this IServiceCollection services)
            where TRemoteAuthenticationState : RemoteAuthenticationState
            where TProviderOptions : class, new()
        {
            services.AddOptions();
            services.AddAuthorizationCore();
            services.TryAddSingleton<AuthenticationStateProvider, RemoteAuthenticationService<TRemoteAuthenticationState, TProviderOptions>>();
            services.TryAddSingleton(sp =>
                {
                    return (IRemoteAuthenticationService<TRemoteAuthenticationState>)sp.GetRequiredService<AuthenticationStateProvider>();
                });

            services.TryAddSingleton(sp =>
            {
                return (IAccessTokenProvider)sp.GetRequiredService<AuthenticationStateProvider>();
            });

            services.TryAddSingleton<IRemoteAuthenticationPathsProvider, DefaultRemoteApplicationPathsProvider<TProviderOptions>>();

            services.TryAddSingleton<SignOutSessionStateManager>();

            return services;
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using the given <typeparamref name="TProviderOptions"/> and
        /// <typeparamref name="TRemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The state to be persisted across authentication operations.</typeparam>
        /// <typeparam name="TProviderOptions">The configuration options of the underlying provider being used for handling the authentication operations.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddRemoteAuthentication<TRemoteAuthenticationState, TProviderOptions>(this IServiceCollection services, Action<RemoteAuthenticationOptions<TProviderOptions>> configure)
            where TRemoteAuthenticationState : RemoteAuthenticationState
            where TProviderOptions : class, new()
        {
            services.AddRemoteAuthentication<RemoteAuthenticationState, TProviderOptions>();
            if (configure != null)
            {
                services.Configure(configure);
            }

            return services;
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="OidcProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{TProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddOidcAuthentication(this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
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
        public static IServiceCollection AddOidcAuthentication<TRemoteAuthenticationState>(this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        {
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RemoteAuthenticationOptions<OidcProviderOptions>>, DefaultOidcOptionsConfiguration>());

            return AddRemoteAuthentication<TRemoteAuthenticationState, OidcProviderOptions>(services, configure);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
        {
            return AddApiauthorizationCore<RemoteAuthenticationState>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddApiAuthorization<TRemoteAuthenticationState>(this IServiceCollection services)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        {
            return AddApiauthorizationCore<TRemoteAuthenticationState>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddApiAuthorization(this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
        {
            return AddApiauthorizationCore<RemoteAuthenticationState>(services, configure, Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddApiAuthorization<TRemoteAuthenticationState>(this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        {
            return AddApiauthorizationCore<TRemoteAuthenticationState>(services, configure, Assembly.GetCallingAssembly().GetName().Name);
        }

        private static IServiceCollection AddApiauthorizationCore<TRemoteAuthenticationState>(
            IServiceCollection services,
            Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure,
            string inferredClientId)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        {
            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPostConfigureOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>, DefaultApiAuthorizationOptionsConfiguration>(_ =>
                new DefaultApiAuthorizationOptionsConfiguration(inferredClientId)));

            services.AddRemoteAuthentication<TRemoteAuthenticationState, ApiAuthorizationProviderOptions>(configure);

            return services;
        }
    }
}
