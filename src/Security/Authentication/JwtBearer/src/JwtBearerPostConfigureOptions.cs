// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Microsoft.AspNetCore.Authentication.JwtBearer;

/// <summary>
/// Used to setup defaults for all <see cref="JwtBearerOptions"/>.
/// </summary>
public class JwtBearerPostConfigureOptions : IPostConfigureOptions<JwtBearerOptions>
{
    /// <summary>
    /// Invoked to post configure a JwtBearerOptions instance.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    public void PostConfigure(string? name, JwtBearerOptions options)
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

                if (options.Backchannel == null)
                {
                    options.Backchannel = new HttpClient(options.BackchannelHttpHandler ?? new HttpClientHandler());
                    options.Backchannel.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core JwtBearer handler");
                    options.Backchannel.Timeout = options.BackchannelTimeout;
                    options.Backchannel.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
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
}
