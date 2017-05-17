// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.JwtBearer
{
    /// <summary>
    /// Used to setup defaults for all <see cref="JwtBearerOptions"/>.
    /// </summary>
    public class JwtBearerInitializer : IInitializeOptions<JwtBearerOptions>
    {
        /// <summary>
        /// Invoked to initialize a JwtBearerOptions instance.
        /// </summary>
        /// <param name="name">The name of the options instance being initialized.</param>
        /// <param name="options">The options instance to initialize.</param>
        public void Initialize(string name, JwtBearerOptions options)
        {
            if (string.IsNullOrEmpty(options.TokenValidationParameters.ValidAudience) && !string.IsNullOrEmpty(options.Audience))
            {
                options.TokenValidationParameters.ValidAudience = options.Audience;
            }

            if (options.ConfigurationManager == null)
            {
                if (options.Configuration != null)
                {
                    options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(options.Configuration);
                }
                else if (!(string.IsNullOrEmpty(options.MetadataAddress) && string.IsNullOrEmpty(options.Authority)))
                {
                    if (string.IsNullOrEmpty(options.MetadataAddress) && !string.IsNullOrEmpty(options.Authority))
                    {
                        options.MetadataAddress = options.Authority;
                        if (!options.MetadataAddress.EndsWith("/", StringComparison.Ordinal))
                        {
                            options.MetadataAddress += "/";
                        }

                        options.MetadataAddress += ".well-known/openid-configuration";
                    }

                    if (options.RequireHttpsMetadata && !options.MetadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("The MetadataAddress or Authority must use HTTPS unless disabled for development by setting RequireHttpsMetadata=false.");
                    }

                    var httpClient = new HttpClient(options.BackchannelHttpHandler ?? new HttpClientHandler());
                    httpClient.Timeout = options.BackchannelTimeout;
                    httpClient.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB

                    options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(options.MetadataAddress, new OpenIdConnectConfigurationRetriever(),
                        new HttpDocumentRetriever(httpClient) { RequireHttps = options.RequireHttpsMetadata });
                }
            }
        }
    }
}
