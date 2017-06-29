// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.Authentication
{
    /// <summary>
    /// Used to configure authentication
    /// </summary>
    public class AuthenticationBuilder
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="services">The services being configured.</param>
        public AuthenticationBuilder(IServiceCollection services)
            => Services = services;

        /// <summary>
        /// The services being configured.
        /// </summary>
        public virtual IServiceCollection Services { get; }

        /// <summary>
        /// Adds a <see cref="AuthenticationScheme"/> which can be used by <see cref="IAuthenticationService"/>.
        /// </summary>
        /// <typeparam name="TOptions">The <see cref="AuthenticationSchemeOptions"/> type to configure the handler."/>.</typeparam>
        /// <typeparam name="THandler">The <see cref="AuthenticationHandler{TOptions}"/> used to handle this scheme.</typeparam>
        /// <param name="authenticationScheme">The name of this scheme.</param>
        /// <param name="displayName">The display name of this scheme.</param>
        /// <param name="configureOptions">Used to configure the scheme options.</param>
        /// <returns>The builder.</returns>
        public virtual AuthenticationBuilder AddScheme<TOptions, THandler>(string authenticationScheme, string displayName, Action<TOptions> configureOptions)
            where TOptions : AuthenticationSchemeOptions, new()
            where THandler : AuthenticationHandler<TOptions>
        {
            Services.Configure<AuthenticationOptions>(o =>
            {
                o.AddScheme(authenticationScheme, scheme => {
                    scheme.HandlerType = typeof(THandler);
                    scheme.DisplayName = displayName;
                });
            });
            if (configureOptions != null)
            {
                Services.Configure(authenticationScheme, configureOptions);
            }
            Services.AddTransient<THandler>();
            return this;
        }

        /// <summary>
        /// Adds a <see cref="AuthenticationScheme"/> which can be used by <see cref="IAuthenticationService"/>.
        /// </summary>
        /// <typeparam name="TOptions">The <see cref="AuthenticationSchemeOptions"/> type to configure the handler."/>.</typeparam>
        /// <typeparam name="THandler">The <see cref="AuthenticationHandler{TOptions}"/> used to handle this scheme.</typeparam>
        /// <param name="authenticationScheme">The name of this scheme.</param>
        /// <param name="configureOptions">Used to configure the scheme options.</param>
        /// <returns>The builder.</returns>
        public virtual AuthenticationBuilder AddScheme<TOptions, THandler>(string authenticationScheme, Action<TOptions> configureOptions)
            where TOptions : AuthenticationSchemeOptions, new()
            where THandler : AuthenticationHandler<TOptions>
            => AddScheme<TOptions, THandler>(authenticationScheme, displayName: null, configureOptions: configureOptions);

        /// <summary>
        /// Adds a <see cref="RemoteAuthenticationHandler{TOptions}"/> based <see cref="AuthenticationScheme"/> that supports remote authentication
        /// which can be used by <see cref="IAuthenticationService"/>.
        /// </summary>
        /// <typeparam name="TOptions">The <see cref="RemoteAuthenticationOptions"/> type to configure the handler."/>.</typeparam>
        /// <typeparam name="THandler">The <see cref="RemoteAuthenticationHandler{TOptions}"/> used to handle this scheme.</typeparam>
        /// <param name="authenticationScheme">The name of this scheme.</param>
        /// <param name="displayName">The display name of this scheme.</param>
        /// <param name="configureOptions">Used to configure the scheme options.</param>
        /// <returns>The builder.</returns>
        public virtual AuthenticationBuilder AddRemoteScheme<TOptions, THandler>(string authenticationScheme, string displayName, Action<TOptions> configureOptions)
            where TOptions : RemoteAuthenticationOptions, new()
            where THandler : RemoteAuthenticationHandler<TOptions>
        {
            Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<TOptions>, EnsureSignInScheme<TOptions>>());
            return AddScheme<TOptions, THandler>(authenticationScheme, displayName, configureOptions: configureOptions);
        }

        // Used to ensure that there's always a default data protection provider
        private class EnsureSignInScheme<TOptions> : IPostConfigureOptions<TOptions> where TOptions : RemoteAuthenticationOptions
        {
            private readonly AuthenticationOptions _authOptions;

            public EnsureSignInScheme(IOptions<AuthenticationOptions> authOptions)
            {
                _authOptions = authOptions.Value;
            }

            public void PostConfigure(string name, TOptions options)
            {
                options.SignInScheme = options.SignInScheme ?? _authOptions.DefaultSignInScheme;
            }
        }
    }
}
