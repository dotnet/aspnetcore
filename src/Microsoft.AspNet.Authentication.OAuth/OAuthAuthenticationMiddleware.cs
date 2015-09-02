// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.DataProtection;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;

namespace Microsoft.AspNet.Authentication.OAuth
{
    /// <summary>
    /// An ASP.NET middleware for authenticating users using OAuth services.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Middleware are not disposable.")]
    public class OAuthAuthenticationMiddleware<TOptions> : AuthenticationMiddleware<TOptions> where TOptions : OAuthAuthenticationOptions, new()
    {
        /// <summary>
        /// Initializes a new <see cref="OAuthAuthenticationMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the HTTP pipeline to invoke.</param>
        /// <param name="dataProtectionProvider"></param>
        /// <param name="loggerFactory"></param>
        /// <param name="options">Configuration options for the middleware.</param>
        public OAuthAuthenticationMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] IDataProtectionProvider dataProtectionProvider,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IUrlEncoder encoder,
            [NotNull] IOptions<SharedAuthenticationOptions> sharedOptions,
            [NotNull] IOptions<TOptions> options,
            ConfigureOptions<TOptions> configureOptions = null)
            : base(next, options, loggerFactory, encoder, configureOptions)
        {
            // todo: review error handling
            if (string.IsNullOrEmpty(Options.AuthenticationScheme))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(Options.AuthenticationScheme)));
            }

            if (string.IsNullOrEmpty(Options.ClientId))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(Options.ClientId)));
            }

            if (string.IsNullOrEmpty(Options.ClientSecret))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(Options.ClientSecret)));
            }

            if (string.IsNullOrEmpty(Options.AuthorizationEndpoint))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(Options.AuthorizationEndpoint)));
            }

            if (string.IsNullOrEmpty(Options.TokenEndpoint))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, nameof(Options.TokenEndpoint)));
            }

            if (Options.Notifications == null)
            {
                Options.Notifications = new OAuthAuthenticationNotifications();
            }

            if (Options.StateDataFormat == null)
            {
                var dataProtector = dataProtectionProvider.CreateProtector(
                    GetType().FullName, Options.AuthenticationScheme, "v1");
                Options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }

            Backchannel = new HttpClient(ResolveHttpMessageHandler(Options));
            Backchannel.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET OAuth middleware");
            Backchannel.Timeout = Options.BackchannelTimeout;
            Backchannel.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB

            if (string.IsNullOrEmpty(Options.SignInScheme))
            {
                Options.SignInScheme = sharedOptions.Value.SignInScheme;
            }
        }

        protected HttpClient Backchannel { get; private set; }

        /// <summary>
        /// Provides the <see cref="AuthenticationHandler"/> object for processing authentication-related requests.
        /// </summary>
        /// <returns>An <see cref="AuthenticationHandler"/> configured with the <see cref="OAuthAuthenticationOptions"/> supplied to the constructor.</returns>
        protected override AuthenticationHandler<TOptions> CreateHandler()
        {
            return new OAuthAuthenticationHandler<TOptions>(Backchannel);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Managed by caller")]
        private static HttpMessageHandler ResolveHttpMessageHandler(OAuthAuthenticationOptions options)
        {
            HttpMessageHandler handler = options.BackchannelHttpHandler ??
#if DNX451
                new WebRequestHandler();
            // If they provided a validator, apply it or fail.
            if (options.BackchannelCertificateValidator != null)
            {
                // Set the cert validate callback
                var webRequestHandler = handler as WebRequestHandler;
                if (webRequestHandler == null)
                {
                    throw new InvalidOperationException(Resources.Exception_ValidatorHandlerMismatch);
                }
                webRequestHandler.ServerCertificateValidationCallback = options.BackchannelCertificateValidator.Validate;
            }
#else
                new WinHttpHandler();
#endif
            return handler;
        }
    }
}
