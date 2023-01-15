// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.WsFederation;

namespace Microsoft.AspNetCore.Authentication.WsFederation;

/// <summary>
/// Used to setup defaults for all <see cref="WsFederationOptions"/>.
/// </summary>
public class WsFederationPostConfigureOptions : IPostConfigureOptions<WsFederationOptions>
{
    private readonly IDataProtectionProvider _dp;

    /// <summary>
    ///
    /// </summary>
    /// <param name="dataProtection"></param>
    public WsFederationPostConfigureOptions(IDataProtectionProvider dataProtection)
    {
        _dp = dataProtection;
    }

    /// <summary>
    /// Invoked to post configure a TOptions instance.
    /// </summary>
    /// <param name="name">The name of the options instance being configured.</param>
    /// <param name="options">The options instance to configure.</param>
    public void PostConfigure(string? name, WsFederationOptions options)
    {
        ArgumentNullException.ThrowIfNull(name);

        options.DataProtectionProvider = options.DataProtectionProvider ?? _dp;

        if (string.IsNullOrEmpty(options.SignOutScheme))
        {
            options.SignOutScheme = options.SignInScheme;
        }

        if (options.StateDataFormat == null)
        {
            var dataProtector = options.DataProtectionProvider.CreateProtector(
                typeof(WsFederationHandler).FullName!, name, "v1");
            options.StateDataFormat = new PropertiesDataFormat(dataProtector);
        }

        if (!options.CallbackPath.HasValue && !string.IsNullOrEmpty(options.Wreply) && Uri.TryCreate(options.Wreply, UriKind.Absolute, out var wreply))
        {
            // Wreply must be a very specific, case sensitive value, so we can't generate it. Instead we generate CallbackPath from it.
            options.CallbackPath = PathString.FromUriComponent(wreply);
        }

        if (string.IsNullOrEmpty(options.TokenValidationParameters.ValidAudience))
        {
            options.TokenValidationParameters.ValidAudience = options.Wtrealm;
        }

        if (options.Backchannel == null)
        {
            options.Backchannel = new HttpClient(options.BackchannelHttpHandler ?? new HttpClientHandler());
            options.Backchannel.DefaultRequestHeaders.UserAgent.ParseAdd("Microsoft ASP.NET Core WsFederation handler");
            options.Backchannel.Timeout = options.BackchannelTimeout;
            options.Backchannel.MaxResponseContentBufferSize = 1024 * 1024 * 10; // 10 MB
        }

        if (options.ConfigurationManager == null)
        {
            if (options.Configuration != null)
            {
                options.ConfigurationManager = new StaticConfigurationManager<WsFederationConfiguration>(options.Configuration);
            }
            else if (!string.IsNullOrEmpty(options.MetadataAddress))
            {
                if (options.RequireHttpsMetadata && !options.MetadataAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("The MetadataAddress must use HTTPS unless disabled for development by setting RequireHttpsMetadata=false.");
                }

                options.ConfigurationManager = new ConfigurationManager<WsFederationConfiguration>(options.MetadataAddress, new WsFederationConfigurationRetriever(),
                    new HttpDocumentRetriever(options.Backchannel) { RequireHttps = options.RequireHttpsMetadata });
            }
        }
    }
}
