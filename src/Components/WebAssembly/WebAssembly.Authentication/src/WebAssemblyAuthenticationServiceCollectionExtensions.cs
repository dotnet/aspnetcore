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
        /// <typeparam name="TAccount">The account type.</typeparam>
        /// <typeparam name="TProviderOptions">The configuration options of the underlying provider being used for handling the authentication operations.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, TProviderOptions>(this IServiceCollection services)
            where TRemoteAuthenticationState : RemoteAuthenticationState
            where TAccount : RemoteUserAccount
            where TProviderOptions : class, new()
        {
            services.AddOptions();
            services.AddAuthorizationCore();
            services.TryAddScoped<AuthenticationStateProvider, RemoteAuthenticationService<TRemoteAuthenticationState, TAccount, TProviderOptions>>();
            services.TryAddScoped(sp =>
                {
                    return (IRemoteAuthenticationService<TRemoteAuthenticationState>)sp.GetRequiredService<AuthenticationStateProvider>();
                });

            services.TryAddTransient<BaseAddressAuthorizationMessageHandler>();
            services.TryAddTransient<AuthorizationMessageHandler>();

            services.TryAddScoped(sp =>
            {
                return (IAccessTokenProvider)sp.GetRequiredService<AuthenticationStateProvider>();
            });

            services.TryAddScoped<IRemoteAuthenticationPathsProvider, DefaultRemoteApplicationPathsProvider<TProviderOptions>>();
            services.TryAddScoped<IAccessTokenProviderAccessor, AccessTokenProviderAccessor>();
            services.TryAddScoped<SignOutSessionStateManager>();

            services.TryAddScoped<AccountClaimsPrincipalFactory<TAccount>>();

            return new RemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount>(services);
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
        public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddRemoteAuthentication<TRemoteAuthenticationState, TAccount, TProviderOptions>(this IServiceCollection services, Action<RemoteAuthenticationOptions<TProviderOptions>> configure)
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
        public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddOidcAuthentication(this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
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
        public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddOidcAuthentication<TRemoteAuthenticationState>(this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
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
        public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddOidcAuthentication<TRemoteAuthenticationState, TAccount>(this IServiceCollection services, Action<RemoteAuthenticationOptions<OidcProviderOptions>> configure)
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
            return AddApiauthorizationCore<RemoteAuthenticationState, RemoteUserAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization<TRemoteAuthenticationState>(this IServiceCollection services)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        {
            return AddApiauthorizationCore<TRemoteAuthenticationState, RemoteUserAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <typeparam name="TAccount">The account type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiAuthorization<TRemoteAuthenticationState, TAccount>(this IServiceCollection services)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
            where TAccount : RemoteUserAccount
        {
            return AddApiauthorizationCore<TRemoteAuthenticationState, TAccount>(services, configure: null, Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IRemoteAuthenticationBuilder<RemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization(this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
        {
            return AddApiauthorizationCore<RemoteAuthenticationState, RemoteUserAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, RemoteUserAccount> AddApiAuthorization<TRemoteAuthenticationState>(this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
        {
            return AddApiauthorizationCore<TRemoteAuthenticationState, RemoteUserAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name);
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <typeparam name="TRemoteAuthenticationState">The type of the remote authentication state.</typeparam>
        /// <typeparam name="TAccount">The account type.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiAuthorization<TRemoteAuthenticationState, TAccount>(this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
            where TRemoteAuthenticationState : RemoteAuthenticationState, new()
            where TAccount : RemoteUserAccount
        {
            return AddApiauthorizationCore<TRemoteAuthenticationState, TAccount>(services, configure, Assembly.GetCallingAssembly().GetName().Name);
        }

        private static IRemoteAuthenticationBuilder<TRemoteAuthenticationState, TAccount> AddApiauthorizationCore<TRemoteAuthenticationState, TAccount>(
            IServiceCollection services,
            Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure,
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
}
