// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Http;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Security.DataHandler;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.Framework.Logging;

namespace Microsoft.AspNet.Security.Facebook
{
    /// <summary>
    /// ASP.NET middleware for authenticating users using Facebook
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Middleware is not disposable.")]
    public class FacebookAuthenticationMiddleware : AuthenticationMiddleware<FacebookAuthenticationOptions>
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Initializes a <see cref="FacebookAuthenticationMiddleware"/>
        /// </summary>
        /// <param name="next">The next middleware in the application pipeline to invoke</param>
        /// <param name="options">Configuration options for the middleware</param>
        public FacebookAuthenticationMiddleware(
            RequestDelegate next,
            IDataProtectionProvider dataProtectionProvider,
            ILoggerFactory loggerFactory,
            IServiceProvider services,
            FacebookAuthenticationOptions options)
            : base(next, options)
        {
            if (string.IsNullOrWhiteSpace(Options.AppId))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, "AppId"));
            }
            if (string.IsNullOrWhiteSpace(Options.AppSecret))
            {
                throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, Resources.Exception_OptionMustBeProvided, "AppSecret"));
            }

            _logger = loggerFactory.Create(typeof(FacebookAuthenticationMiddleware).FullName);

            if (Options.Notifications == null)
            {
                Options.Notifications = new FacebookAuthenticationNotifications();
            }
            if (Options.StateDataFormat == null)
            {
                IDataProtector dataProtector = DataProtectionHelpers.CreateDataProtector(dataProtectionProvider,
                    typeof(FacebookAuthenticationMiddleware).FullName, options.AuthenticationType, "v1");
                Options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }

            _httpClient = new HttpClient(ResolveHttpMessageHandler(Options));
            _httpClient.Timeout = Options.BackchannelTimeout;
            _httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
        }

        /// <summary>
        /// Provides the <see cref="AuthenticationHandler"/> object for processing authentication-related requests.
        /// </summary>
        /// <returns>An <see cref="AuthenticationHandler"/> configured with the <see cref="FacebookAuthenticationOptions"/> supplied to the constructor.</returns>
        protected override AuthenticationHandler<FacebookAuthenticationOptions> CreateHandler()
        {
            return new FacebookAuthenticationHandler(_httpClient, _logger);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Managed by caller")]
        private static HttpMessageHandler ResolveHttpMessageHandler(FacebookAuthenticationOptions options)
        {
            HttpMessageHandler handler = options.BackchannelHttpHandler ??
#if NET45
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
