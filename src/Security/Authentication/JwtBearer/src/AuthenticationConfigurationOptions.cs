// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication;

internal class AuthenticationConfigurationOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly IAuthenticationConfigurationProvider _configurationProvider;

    /// <summary>
    /// Initializes a new <see cref="AuthenticationConfigurationOptions"/> given the configuration
    /// provided by the <paramref name="configurationProvider"/>.
    /// </summary>
    /// <param name="configurationProvider">An <see cref="IAuthenticationConfigurationProvider"/> instance.</param>
    public AuthenticationConfigurationOptions(IAuthenticationConfigurationProvider configurationProvider)
    {
        _configurationProvider = configurationProvider;
    }

    /// <inheritdoc />
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var configSection = _configurationProvider.GetSection(name);

        if (configSection is null)
        {
            return;
        }

        var issuer = configSection["ClaimsIssuer"];
        var audiences = configSection.GetSection("Audiences").GetChildren().Select(aud => aud.Value).ToArray();
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = issuer is not null,
            ValidIssuers = new[] { issuer },
            ValidateAudience = audiences.Length > 0,
            ValidAudiences = audiences,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetIssuerSigningKey(_configurationProvider.Configuration, issuer),
        };
    }

    private static SecurityKey GetIssuerSigningKey(IConfiguration configuration, string? issuer)
    {
        var jwtKeyMaterialSecret = configuration[$"{issuer}:KeyMaterial"];
        var jwtKeyMaterial = !string.IsNullOrEmpty(jwtKeyMaterialSecret)
            ? Convert.FromBase64String(jwtKeyMaterialSecret)
            : System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
        return new SymmetricSecurityKey(jwtKeyMaterial);
    }

    /// <inheritdoc />
    public void Configure(JwtBearerOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}
