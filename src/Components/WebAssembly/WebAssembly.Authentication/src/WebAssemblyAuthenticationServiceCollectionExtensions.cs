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
            AddRemoteAuthentication<RemoteAuthenticationState, OidcProviderOptions>(services, configure);

            services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<RemoteAuthenticationOptions<OidcProviderOptions>>, DefaultOidcOptionsConfiguration>());

            if (configure != null)
            {
                services.Configure(configure);
            }

            return services;
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
        {
            var inferredClientId = Assembly.GetCallingAssembly().GetName().Name;

            services.AddRemoteAuthentication<RemoteAuthenticationState, ApiAuthorizationProviderOptions>();

            services.TryAddEnumerable(
                ServiceDescriptor.Singleton<IPostConfigureOptions<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>>, DefaultApiAuthorizationOptionsConfiguration>(_ =>
                new DefaultApiAuthorizationOptionsConfiguration(inferredClientId)));

            return services;
        }

        /// <summary>
        /// Adds support for authentication for SPA applications using <see cref="ApiAuthorizationProviderOptions"/> and the <see cref="RemoteAuthenticationState"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
        /// <param name="configure">An action that will configure the <see cref="RemoteAuthenticationOptions{ApiAuthorizationProviderOptions}"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> where the services were registered.</returns>
        public static IServiceCollection AddApiAuthorization(this IServiceCollection services, Action<RemoteAuthenticationOptions<ApiAuthorizationProviderOptions>> configure)
        {
            services.AddApiAuthorization();

            if (configure != null)
            {
                services.Configure(configure);
            }

            return services;
        }
    }
}
