// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.OpenIdConnect
{
    /// <summary>
    /// Used to setup defaults for all <see cref="OpenIdConnectOptions"/>.
    /// </summary>
    public class OpenIdConnectPostConfigureOptions : IPostConfigureOptions<OpenIdConnectOptions>
    {
        private readonly IDataProtectionProvider _dp;

        public OpenIdConnectPostConfigureOptions(IDataProtectionProvider dataProtection)
        {
            _dp = dataProtection;
        }

        /// <summary>
        /// Invoked to post configure a TOptions instance.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The options instance to configure.</param>
        public void PostConfigure(string name, OpenIdConnectOptions options)
        {
            options.DataProtectionProvider = options.DataProtectionProvider ?? _dp;

            if (string.IsNullOrEmpty(options.SignOutScheme))
            {
                options.SignOutScheme = options.SignInScheme;
            }

            if (options.StateDataFormat == null)
            {
                var dataProtector = options.DataProtectionProvider.CreateProtector(
                    typeof(OpenIdConnectHandler).FullName, name, "v1");
                options.StateDataFormat = new PropertiesDataFormat(dataProtector);
            }

            if (options.StringDataFormat == null)
            {
                var dataProtector = options.DataProtectionProvider.CreateProtector(
                    typeof(OpenIdConnectHandler).FullName,
                    typeof(string).FullName,
                    name,
                    "v1");

                options.StringDataFormat = new SecureDataFormat<string>(new StringSerializer(), dataProtector);
            }

            if (string.IsNullOrEmpty(options.TokenValidationParameters.ValidAudience) && !string.IsNullOrEmpty(options.ClientId))
            {
                options.TokenValidationParameters.ValidAudience = options.ClientId;
            }

            if (options.Backchannel == null)
            {
                options.Backchannel = new HttpClient(options.BackchannelHttpHandler ?? new HttpClientHandler());
                options.Backchannel.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core OpenIdConnect handler");
                options.Backchannel.Timeout = options.BackchannelTimeout;
                options.Backchannel.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
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

                    options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(options.MetadataAddress, new OpenIdConnectConfigurationRetriever(),
                        new HttpDocumentRetriever(options.Backchannel) { RequireHttps = options.RequireHttpsMetadata })
                    {
                        RefreshInterval = options.RefreshInterval,
                        AutomaticRefreshInterval = options.AutomaticRefreshInterval,
                    };
                }
            }
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
