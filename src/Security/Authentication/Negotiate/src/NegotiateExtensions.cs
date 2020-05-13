// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extensions for enabling Negotiate authentication.
    /// </summary>
    public static class NegotiateExtensions
    {
        /// <summary>
        /// Adds Negotiate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <returns>The original builder.</returns>
        public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder)
            => builder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, _ => { });

        /// <summary>
        /// Adds and configures Negotiate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
        /// <returns>The original builder.</returns>
        public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, Action<NegotiateOptions> configureOptions)
            => builder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Adds and configures Negotiate authentication.
        /// </summary>
        /// <typeparam name="TService">TService: A service resolved from the IServiceProvider for use when configuring this authentication provider. If you need multiple services then specify IServiceProvider and resolve them directly.</typeparam>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
        /// <returns>The original builder.</returns>
        public static AuthenticationBuilder AddNegotiate<TService>(this AuthenticationBuilder builder, Action<NegotiateOptions, TService> configureOptions) where TService : class
            => builder.AddNegotiate(NegotiateDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Adds and configures Negotiate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">The scheme name used to identify the authentication handler internally.</param>
        /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
        /// <returns>The original builder.</returns>
        public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, string authenticationScheme, Action<NegotiateOptions> configureOptions)
            => builder.AddNegotiate(authenticationScheme, displayName: null, configureOptions: configureOptions);

        /// <summary>
        /// Adds and configures Negotiate authentication.
        /// </summary>
        /// <typeparam name="TService">TService: A service resolved from the IServiceProvider for use when configuring this authentication provider. If you need multiple services then specify IServiceProvider and resolve them directly.</typeparam>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">The scheme name used to identify the authentication handler internally.</param>
        /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
        /// <returns>The original builder.</returns>
        public static AuthenticationBuilder AddNegotiate<TService>(this AuthenticationBuilder builder, string authenticationScheme, Action<NegotiateOptions, TService> configureOptions) where TService : class
            => builder.AddNegotiate(authenticationScheme, displayName: null, configureOptions: configureOptions);

        /// <summary>
        /// Adds and configures Negotiate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">The scheme name used to identify the authentication handler internally.</param>
        /// <param name="displayName">The name displayed to users when selecting an authentication handler. The default is null to prevent this from displaying.</param>
        /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
        /// <returns>The original builder.</returns>
        public static AuthenticationBuilder AddNegotiate(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<NegotiateOptions> configureOptions)
        {
            Action<NegotiateOptions, IServiceProvider> configureOptionsWithServices;
            if (configureOptions == null)
            {
                configureOptionsWithServices = null;
            }
            else
            {
                configureOptionsWithServices = (options, _) => configureOptions(options);
            }

            return builder.AddNegotiate(authenticationScheme, displayName, configureOptionsWithServices);
        }

        /// <summary>
        /// Adds and configures Negotiate authentication.
        /// </summary>
        /// <typeparam name="TService">TService: A service resolved from the IServiceProvider for use when configuring this authentication provider. If you need multiple services then specify IServiceProvider and resolve them directly.</typeparam>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme">The scheme name used to identify the authentication handler internally.</param>
        /// <param name="displayName">The name displayed to users when selecting an authentication handler. The default is null to prevent this from displaying.</param>
        /// <param name="configureOptions">Allows for configuring the authentication handler.</param>
        /// <returns>The original builder.</returns>
        public static AuthenticationBuilder AddNegotiate<TService>(this AuthenticationBuilder builder, string authenticationScheme, string displayName, Action<NegotiateOptions, TService> configureOptions) where TService : class
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<NegotiateOptions>, PostConfigureNegotiateOptions>());
            return builder.AddScheme<NegotiateOptions, NegotiateHandler, TService>(authenticationScheme, displayName, configureOptions);
        }
    }
}
