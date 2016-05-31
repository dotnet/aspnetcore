// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// ASP.NET Core middleware for obtaining identities using OpenIdConnect protocol.
    /// </summary>
    public class OpenIdConnectMiddleware : AuthenticationMiddleware<OpenIdConnectOptions>
    {
        /// <summary>
        /// Initializes a <see cref="OpenIdConnectMiddleware"/>
        /// </summary>
        /// <param name="next">The next middleware in the middleware pipeline to invoke.</param>
        /// <param name="dataProtectionProvider"> provider for creating a data protector.</param>
        /// <param name="loggerFactory">factory for creating a <see cref="ILogger"/>.</param>
        /// <param name="encoder"></param>
        /// <param name="services"></param>
        /// <param name="sharedOptions"></param>
        /// <param name="options"></param>
        /// <param name="htmlEncoder">The <see cref="HtmlEncoder"/>.</param>
        public OpenIdConnectMiddleware(
            RequestDelegate next,
            IDataProtectionProvider dataProtectionProvider,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            IServiceProvider services,
            IOptions<SharedAuthenticationOptions> sharedOptions,
            IOptions<OpenIdConnectOptions> options,
            HtmlEncoder htmlEncoder)
            : base(next, options, loggerFactory, encoder)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (dataProtectionProvider == null)
            {
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            if (encoder == null)
            {
                throw new ArgumentNullException(nameof(encoder));
            }

            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (sharedOptions == null)
            {
                throw new ArgumentNullException(nameof(sharedOptions));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (htmlEncoder == null)
            {
                throw new ArgumentNullException(nameof(htmlEncoder));
            }

            if (string.IsNullOrEmpty(Options.ClientId))
            {
                throw new ArgumentException("Options.ClientId must be provided", nameof(Options.ClientId));
            }
            
            if (!Options.CallbackPath.HasValue)
            {
                throw new ArgumentException("Options.CallbackPath must be provided.");
            }

            if (string.IsNullOrEmpty(Options.SignInScheme))
            {
                Options.SignInScheme = sharedOptions.Value.SignInScheme;
            }
            if (string.IsNullOrEmpty(Options.SignInScheme))
            {
                throw new ArgumentException("Options.SignInScheme is required.");
            }
            if (string.IsNullOrEmpty(Options.SignOutScheme))
            {
                Options.SignOutScheme = Options.SignInScheme;
            }

            HtmlEncoder = htmlEncoder;

            if (Options.StateDataFormat == null)
            {
                var dataProtector = dataProtectionProvider.CreateProtector(
                    typeof(OpenIdConnectMiddleware).FullName,
                    typeof(string).FullName,
                    Options.AuthenticationScheme,
                    "v1");

                Options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }

            if (Options.StringDataFormat == null)
            {
                var dataProtector = dataProtectionProvider.CreateProtector(
                    typeof(OpenIdConnectMiddleware).FullName,
                    typeof(string).FullName,
                    Options.AuthenticationScheme,
                    "v1");

                Options.StringDataFormat = new SecureDataFormat<string>(new StringSerializer(), dataProtector);
            }

            if (Options.Events == null)
            {
                Options.Events = new OpenIdConnectEvents();
            }

            if (string.IsNullOrEmpty(Options.TokenValidationParameters.ValidAudience) && !string.IsNullOrEmpty(Options.ClientId))
            {
                Options.TokenValidationParameters.ValidAudience = Options.ClientId;
            }

            Backchannel = new HttpClient(Options.BackchannelHttpHandler ?? new HttpClientHandler());
            Backchannel.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core OpenIdConnect middleware");
            Backchannel.Timeout = Options.BackchannelTimeout;
            Backchannel.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB

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

                    if (Options.RequireHttpsMetadata && !Options.MetadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("The MetadataAddress or Authority must use HTTPS unless disabled for development by setting RequireHttpsMetadata=false.");
                    }

                    Options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(Options.MetadataAddress, new OpenIdConnectConfigurationRetriever(),
                        new HttpDocumentRetriever(Backchannel) { RequireHttps = Options.RequireHttpsMetadata });
                }
            }
            
            if (Options.ConfigurationManager == null)
            {
                throw new InvalidOperationException($"Provide {nameof(Options.Authority)}, {nameof(Options.MetadataAddress)}, "
                + $"{nameof(Options.Configuration)}, or {nameof(Options.ConfigurationManager)} to {nameof(OpenIdConnectOptions)}");
            }
        }

        protected HttpClient Backchannel { get; private set; }

        protected HtmlEncoder HtmlEncoder { get; private set; }

        /// <summary>
        /// Provides the <see cref="AuthenticationHandler{T}"/> object for processing authentication-related requests.
        /// </summary>
        /// <returns>An <see cref="AuthenticationHandler{T}"/> configured with the <see cref="OpenIdConnectOptions"/> supplied to the constructor.</returns>
        protected override AuthenticationHandler<OpenIdConnectOptions> CreateHandler()
        {
            return new OpenIdConnectHandler(Backchannel, HtmlEncoder);
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
