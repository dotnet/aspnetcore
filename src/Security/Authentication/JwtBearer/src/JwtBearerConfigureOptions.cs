// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication;

internal sealed class JwtBearerConfigureOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IAuthenticationConfigurationProvider _authenticationConfigurationProvider;

    /// <summary>
    /// Initializes a new <see cref="JwtBearerConfigureOptions"/> given the configuration
    /// provided by the <paramref name="configurationProvider"/>.
    /// </summary>
    /// <param name="configurationProvider">An <see cref="IAuthenticationConfigurationProvider"/> instance.</param>\
    public JwtBearerConfigureOptions(IAuthenticationConfigurationProvider configurationProvider)
    {
        _authenticationConfigurationProvider = configurationProvider;
    }

    /// <inheritdoc />
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var configSection = _authenticationConfigurationProvider.GetSchemeConfiguration(name);

        if (configSection is null || !configSection.GetChildren().Any())
        {
            return;
        }

        var issuer = configSection[nameof(TokenValidationParameters.ValidIssuer)];
        var issuers = configSection.GetSection(nameof(TokenValidationParameters.ValidIssuers)).GetChildren().Select(iss => iss.Value).ToList();
        if (issuer is not null)
        {
            issuers.Add(issuer);
        }
        var audience = configSection[nameof(TokenValidationParameters.ValidAudience)];
        var audiences = configSection.GetSection(nameof(TokenValidationParameters.ValidAudiences)).GetChildren().Select(aud => aud.Value).ToList();
        if (audience is not null)
        {
            audiences.Add(audience);
        }

        options.Authority = configSection[nameof(options.Authority)];
        options.BackchannelTimeout = TimeSpan.TryParse(configSection[nameof(options.BackchannelTimeout)], out var backChannelTimeout)
            ? backChannelTimeout
            : options.BackchannelTimeout;
        options.Challenge = configSection[nameof(options.Challenge)] ?? options.Challenge;
        options.ForwardAuthenticate = configSection[nameof(options.ForwardAuthenticate)];
        options.ForwardChallenge = configSection[nameof(options.ForwardChallenge)];
        options.ForwardDefault = configSection[nameof(options.ForwardDefault)];
        options.ForwardForbid = configSection[nameof(options.ForwardForbid)];
        options.ForwardSignIn = configSection[nameof(options.ForwardSignIn)];
        options.ForwardSignOut = configSection[nameof(options.ForwardSignOut)];
        options.IncludeErrorDetails = bool.TryParse(configSection[nameof(options.IncludeErrorDetails)], out var includeErrorDetails)
            ? includeErrorDetails
            : options.IncludeErrorDetails;
        options.MapInboundClaims = bool.TryParse(configSection[nameof(options.MapInboundClaims)], out var mapInboundClaims)
            ? mapInboundClaims
            : options.MapInboundClaims;
        options.MetadataAddress = configSection[nameof(options.MetadataAddress)] ?? options.MetadataAddress;
        options.RefreshInterval = TimeSpan.TryParse(configSection[nameof(options.RefreshInterval)], out var refreshInternval)
            ? refreshInternval
            : options.RefreshInterval;
        options.RefreshOnIssuerKeyNotFound = bool.TryParse(configSection[nameof(options.RefreshOnIssuerKeyNotFound)], out var refreshOnIssuerKeyNotFound)
            ? refreshOnIssuerKeyNotFound
            : options.RefreshOnIssuerKeyNotFound;
        options.RequireHttpsMetadata = bool.TryParse(configSection[nameof(options.RequireHttpsMetadata)], out var requireHttpsMetadata)
            ? requireHttpsMetadata
            : options.RequireHttpsMetadata;
        options.SaveToken = bool.TryParse(configSection[nameof(options.SaveToken)], out var saveToken)
            ? saveToken
            : options.SaveToken;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = issuers.Count > 0,
            ValidIssuers = issuers,
            ValidateAudience = audiences.Count > 0,
            ValidAudiences = audiences,
            ValidateIssuerSigningKey = true,
            // Even if we have multiple issuers, they must all resolve to the same signing
            // key under the hood so we just use the first one.
            IssuerSigningKey = GetIssuerSigningKey(configSection, issuers.FirstOrDefault()),
        };
    }

    private static SecurityKey GetIssuerSigningKey(IConfiguration configuration, string? issuer)
    {
        var jwtKeyMaterialSecret = configuration[$"{issuer}:KeyMaterial"];
        var jwtKeyMaterial = !string.IsNullOrEmpty(jwtKeyMaterialSecret)
            ? Convert.FromBase64String(jwtKeyMaterialSecret)
            : RandomNumberGenerator.GetBytes(32);
        return new SymmetricSecurityKey(jwtKeyMaterial);
    }

    /// <inheritdoc />
    public void Configure(JwtBearerOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}
