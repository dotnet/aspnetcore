// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace SecurityWebSite;

public static class BearerAuth
{
    static BearerAuth()
    {
        Key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('a', 128)));
        Credentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256);
    }

    public static readonly SymmetricSecurityKey Key;
    public static readonly SigningCredentials Credentials;
    public static readonly string Issuer = "issuer.contoso.com";
    public static readonly string Audience = "audience.contoso.com";

    public static TokenValidationParameters CreateTokenValidationParameters()
    {
        return new TokenValidationParameters()
        {
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = Key,
        };
    }

    public static string GetTokenText(IEnumerable<Claim> claims)
    {
        var token = new JwtSecurityToken(Issuer, Audience, claims, expires: DateTime.Now.AddMinutes(30), signingCredentials: Credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
