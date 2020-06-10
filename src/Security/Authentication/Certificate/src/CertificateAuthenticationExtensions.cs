// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.AspNetCore.Authentication;

using Microsoft.AspNetCore.Authentication.Certificate;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods to add Certificate authentication capabilities to an HTTP application pipeline.
    /// </summary>
    public static class CertificateAuthenticationAppBuilderExtensions
    {
        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificate(this AuthenticationBuilder builder)
            => builder.AddCertificate(CertificateAuthenticationDefaults.AuthenticationScheme);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme"></param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificate(this AuthenticationBuilder builder, string authenticationScheme)
            => builder.AddCertificate(authenticationScheme, configureOptions: (Action<CertificateAuthenticationOptions, IServiceProvider>)null);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions"></param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificate(this AuthenticationBuilder builder, Action<CertificateAuthenticationOptions> configureOptions)
            => builder.AddCertificate(CertificateAuthenticationDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <typeparam name="TService">TService: A service resolved from the IServiceProvider for use when configuring this authentication provider. If you need multiple services then specify IServiceProvider and resolve them directly.</typeparam>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions"></param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificate<TService>(this AuthenticationBuilder builder, Action<CertificateAuthenticationOptions, TService> configureOptions) where TService : class
            => builder.AddCertificate(CertificateAuthenticationDefaults.AuthenticationScheme, configureOptions);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme"></param>
        /// <param name="configureOptions"></param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificate(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<CertificateAuthenticationOptions> configureOptions)
        {
            Action<CertificateAuthenticationOptions, IServiceProvider> configureOptionsWithServices;
            if (configureOptions == null)
            {
                configureOptionsWithServices = null;
            }
            else
            {
                configureOptionsWithServices = (options, _) => configureOptions(options);
            }

            return builder.AddCertificate(authenticationScheme, configureOptionsWithServices);
        }

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <typeparam name="TService">TService: A service resolved from the IServiceProvider for use when configuring this authentication provider. If you need multiple services then specify IServiceProvider and resolve them directly.</typeparam>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="authenticationScheme"></param>
        /// <param name="configureOptions"></param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificate<TService>(
            this AuthenticationBuilder builder,
            string authenticationScheme,
            Action<CertificateAuthenticationOptions, TService> configureOptions) where TService : class
            => builder.AddScheme<CertificateAuthenticationOptions, CertificateAuthenticationHandler, TService>(authenticationScheme, configureOptions);

        /// <summary>
        /// Adds certificate authentication.
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions"></param>
        /// <returns>The <see cref="AuthenticationBuilder"/>.</returns>
        public static AuthenticationBuilder AddCertificateCache(
            this AuthenticationBuilder builder,
            Action<CertificateValidationCacheOptions> configureOptions = null)
        {
            builder.Services.AddSingleton<ICertificateValidationCache, CertificateValidationCache>();
            if (configureOptions != null)
            {
                builder.Services.Configure(configureOptions);
            }
            return builder;
        }
    }
}
