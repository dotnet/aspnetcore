// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.Internal;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.Framework.WebEncoders;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNet.Authentication.JwtBearer
{
    /// <summary>
    /// Bearer authentication middleware component which is added to an HTTP pipeline. This class is not
    /// created by application code directly, instead it is added by calling the the IAppBuilder UseJwtBearerAuthentication
    /// extension method.
    /// </summary>
    public class JwtBearerAuthenticationMiddleware : AuthenticationMiddleware<JwtBearerAuthenticationOptions>
    {
        /// <summary>
        /// Bearer authentication component which is added to an HTTP pipeline. This constructor is not
        /// called by application code directly, instead it is added by calling the the IAppBuilder UseJwtBearerAuthentication 
        /// extension method.
        /// </summary>
        public JwtBearerAuthenticationMiddleware(
            [NotNull] RequestDelegate next,
            [NotNull] ILoggerFactory loggerFactory,
            [NotNull] IUrlEncoder encoder,
            [NotNull] IOptions<JwtBearerAuthenticationOptions> options,
            ConfigureOptions<JwtBearerAuthenticationOptions> configureOptions)
            : base(next, options, loggerFactory, encoder, configureOptions)
        {
            if (Options.Events == null)
            {
                Options.Events = new JwtBearerAuthenticationEvents();
            }

            if (string.IsNullOrEmpty(Options.TokenValidationParameters.ValidAudience) && !string.IsNullOrEmpty(Options.Audience))
            {
                Options.TokenValidationParameters.ValidAudience = Options.Audience;
            }

            if (Options.ConfigurationManager == null)
            {
                if (Options.Configuration != null)
                {
                    Options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(Options.Configuration);
                }
                else if (!(string.IsNullOrEmpty(Options.MetadataAddress) && string.IsNullOrEmpty(Options.Authority)))
                {
                    if (string.IsNullOrEmpty(Options.MetadataAddress) && !string.IsNullOrEmpty(Options.Authority))
                    {
                        Options.MetadataAddress = Options.Authority;
                        if (!Options.MetadataAddress.EndsWith("/", StringComparison.Ordinal))
                        {
                            Options.MetadataAddress += "/";
                        }

                        Options.MetadataAddress += ".well-known/openid-configuration";
                    }

                    var httpClient = new HttpClient(ResolveHttpMessageHandler(Options));
                    httpClient.Timeout = Options.BackchannelTimeout;
                    httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB

                    Options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(Options.MetadataAddress, new OpenIdConnectConfigurationRetriever(), httpClient);
                }
            }
        }

        /// <summary>
        /// Called by the AuthenticationMiddleware base class to create a per-request handler. 
        /// </summary>
        /// <returns>A new instance of the request handler</returns>
        protected override AuthenticationHandler<JwtBearerAuthenticationOptions> CreateHandler()
        {
            return new JwtBearerAuthenticationHandler();
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Managed by caller")]
        private static HttpMessageHandler ResolveHttpMessageHandler(JwtBearerAuthenticationOptions options)
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
            new HttpClientHandler();
#endif
            return handler;
        }
    }
}
