// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.JwtBearer.Tools;

internal sealed class JwtIssuer
{
    private readonly SymmetricSecurityKey _signingKey;

    public JwtIssuer(string issuer, byte[] signingKeyMaterial)
    {
        Issuer = issuer;
        _signingKey = new SymmetricSecurityKey(signingKeyMaterial);
    }

    public string Issuer { get; }

    public JwtSecurityToken Create(JwtCreatorOptions options)
    {
        var identity = new GenericIdentity(options.Name);

        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, options.Name));

        var id = Guid.NewGuid().ToString().GetHashCode().ToString("x", CultureInfo.InvariantCulture);
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Jti, id));

        if (options.Scopes is { } scopesToAdd)
        {
            identity.AddClaims(scopesToAdd.Select(s => new Claim("scope", s)));
        }

        if (options.Roles is { } rolesToAdd)
        {
            identity.AddClaims(rolesToAdd.Select(r => new Claim(ClaimTypes.Role, r)));
        }

        if (options.Claims is { Count: > 0 } claimsToAdd)
        {
            identity.AddClaims(claimsToAdd.Select(kvp => new Claim(kvp.Key, kvp.Value)));
        }

        // Although the JwtPayload supports having multiple audiences registered, the
        // creator methods and constructors don't provide a way of setting multiple
        // audiences. Instead, we have to register an `aud` claim for each audience
        // we want to add so that the multiple audiences are populated correctly.
        if (options.Audiences is { Count: > 0} audiences)
        {
            identity.AddClaims(audiences.Select(aud => new Claim(JwtRegisteredClaimNames.Aud, aud)));
        }

        var handler = new JwtSecurityTokenHandler();
        var jwtSigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256Signature);
        var jwtToken = handler.CreateJwtSecurityToken(Issuer, audience: null, identity, options.NotBefore, options.ExpiresOn, issuedAt: DateTime.UtcNow, jwtSigningCredentials);
        return jwtToken;
    }

    public static string WriteToken(JwtSecurityToken token)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(token);
    }

    public static JwtSecurityToken Extract(string token) => new JwtSecurityToken(token);

    public bool IsValid(string encodedToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var tokenValidationParameters = new TokenValidationParameters
        {
            IssuerSigningKey = _signingKey,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true
        };
        if (handler.ValidateToken(encodedToken, tokenValidationParameters, out _).Identity?.IsAuthenticated == true)
        {
            return true;
        }
        return false;
    }
}
