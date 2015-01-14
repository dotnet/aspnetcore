// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens;
using System.Net.Http;
using System.Text;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Security.DataHandler;
using Microsoft.AspNet.Security.DataHandler.Encoder;
using Microsoft.AspNet.Security.DataHandler.Serializer;
using Microsoft.AspNet.Security.DataProtection;
using Microsoft.AspNet.Security.Infrastructure;
using Microsoft.Framework.Logging;
using Microsoft.Framework.OptionsModel;
using Microsoft.IdentityModel.Protocols;

namespace Microsoft.AspNet.Security.OpenIdConnect
{
    /// <summary>
    /// ASP.NET middleware for obtaining identities using OpenIdConnect protocol.
    /// </summary>
    public class OpenIdConnectAuthenticationMiddleware : AuthenticationMiddleware<OpenIdConnectAuthenticationOptions>
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a <see cref="OpenIdConnectAuthenticationMiddleware"/>
        /// </summary>
        /// <param name="next">The next middleware in the ASP.NET pipeline to invoke</param>
        /// <param name="app">The ASP.NET application</param>
        /// <param name="options">Configuration options for the middleware</param>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Managed by caller")]
        public OpenIdConnectAuthenticationMiddleware(
            RequestDelegate next,
            IServiceProvider services,
            IDataProtectionProvider dataProtectionProvider,
            ILoggerFactory loggerFactory,
            IOptions<ExternalAuthenticationOptions> externalOptions,
            IOptions<OpenIdConnectAuthenticationOptions> options,
            ConfigureOptions<OpenIdConnectAuthenticationOptions> configureOptions)
            : base(next, services, options, configureOptions)
        {
            _logger = loggerFactory.Create<OpenIdConnectAuthenticationMiddleware>();

            if (string.IsNullOrWhiteSpace(Options.TokenValidationParameters.AuthenticationType))
            {
                Options.TokenValidationParameters.AuthenticationType = externalOptions.Options.SignInAsAuthenticationType;
            }

            if (Options.StateDataFormat == null)
            {
                var dataProtector = dataProtectionProvider.CreateDataProtector(
                    typeof(OpenIdConnectAuthenticationMiddleware).FullName, 
					typeof(string).FullName,
                    Options.AuthenticationType,
					"v1");

                Options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }

            if (Options.StringDataFormat == null)
            {
                var dataProtector = dataProtectionProvider.CreateDataProtector(
                    typeof(OpenIdConnectAuthenticationMiddleware).FullName,
                    typeof(string).FullName,
                    Options.AuthenticationType,
                    "v1");

                Options.StringDataFormat = new SecureDataFormat<string>(new StringSerializer(), dataProtector, TextEncodings.Base64Url);
            }

            if (Options.SecurityTokenValidators == null)
            {
                Options.SecurityTokenValidators = new Collection<ISecurityTokenValidator> { new JwtSecurityTokenHandler() };
            }
            
            // if the user has not set the AuthorizeCallback, set it from the redirect_uri
            if (!Options.CallbackPath.HasValue)
            {
                Uri redirectUri;
                if (!string.IsNullOrEmpty(Options.RedirectUri) && Uri.TryCreate(Options.RedirectUri, UriKind.Absolute, out redirectUri))
                {
                    // Redirect_Uri must be a very specific, case sensitive value, so we can't generate it. Instead we generate AuthorizeCallback from it.
                    Options.CallbackPath = PathString.FromUriComponent(redirectUri);
                }
            }

            if (Options.Notifications == null)
            {
                Options.Notifications = new OpenIdConnectAuthenticationNotifications();
            }

            if (string.IsNullOrWhiteSpace(Options.TokenValidationParameters.ValidAudience) && !string.IsNullOrWhiteSpace(Options.ClientId))
            {
                Options.TokenValidationParameters.ValidAudience = Options.ClientId;
            }

            if (Options.ConfigurationManager == null)
            {
                if (Options.Configuration != null)
                {
                    Options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(Options.Configuration);
                }
                else if (!(string.IsNullOrWhiteSpace(Options.MetadataAddress) && string.IsNullOrWhiteSpace(Options.Authority)))
                {
                    if (string.IsNullOrWhiteSpace(Options.MetadataAddress) && !string.IsNullOrWhiteSpace(Options.Authority))
                    {
                        Options.MetadataAddress = Options.Authority;
                        if (!Options.MetadataAddress.EndsWith("/", StringComparison.Ordinal))
                        {
                            Options.MetadataAddress += "/";
                        }

                        Options.MetadataAddress += ".well-known/openid-configuration";
                    }

                    HttpClient httpClient = new HttpClient(ResolveHttpMessageHandler(Options));
                    httpClient.Timeout = Options.BackchannelTimeout;
                    httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
                    Options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(Options.MetadataAddress, httpClient);
                }
            }
        }

        /// <summary>
        /// Provides the <see cref="AuthenticationHandler"/> object for processing authentication-related requests.
        /// </summary>
        /// <returns>An <see cref="AuthenticationHandler"/> configured with the <see cref="OpenIdConnectAuthenticationOptions"/> supplied to the constructor.</returns>
        protected override AuthenticationHandler<OpenIdConnectAuthenticationOptions> CreateHandler()
        {
            return new OpenIdConnectAuthenticationHandler(_logger);
        }

        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Managed by caller")]
        private static HttpMessageHandler ResolveHttpMessageHandler(OpenIdConnectAuthenticationOptions options)
        {
            HttpMessageHandler handler = options.BackchannelHttpHandler ??
#if ASPNET50
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

        private class StringSerializer : IDataSerializer<string>
        {
            public string Deserialize(byte[] data)
            {
                return Encoding.UTF8.GetString(data);
            }

            public byte[] Serialize(string model)
            {
                return Encoding.UTF8.GetBytes(model);
            }
        }
    }
}
