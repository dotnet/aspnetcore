// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Test helper that mints DBSC proof JWTs (typ=dbsc+jwt, jwk in header, jti in payload),
/// signed with an ephemeral ES256 or RS256 device key, mirroring what shipping Chrome produces.
/// </summary>
internal sealed class DbscProofKey
{
    private static readonly JsonWebTokenHandler s_handler = new();

    private readonly SecurityKey _signingKey;

    private DbscProofKey(string algorithm, SecurityKey signingKey, string publicJwkJson)
    {
        Algorithm = algorithm;
        _signingKey = signingKey;
        PublicJwkJson = publicJwkJson;
    }

    public string Algorithm { get; }

    /// <summary>The public key as a JWK JSON string, as the server stores it for refresh validation.</summary>
    public string PublicJwkJson { get; }

    public static DbscProofKey CreateEs256()
    {
        var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var p = ecdsa.ExportParameters(includePrivateParameters: false);
        var jwk = new Dictionary<string, object>
        {
            ["kty"] = "EC",
            ["crv"] = "P-256",
            ["x"] = WebEncoders.Base64UrlEncode(p.Q.X!),
            ["y"] = WebEncoders.Base64UrlEncode(p.Q.Y!),
        };
        return new DbscProofKey("ES256", new ECDsaSecurityKey(ecdsa), System.Text.Json.JsonSerializer.Serialize(jwk));
    }

    public static DbscProofKey CreateRs256()
    {
        var rsa = RSA.Create(2048);
        var p = rsa.ExportParameters(includePrivateParameters: false);
        var jwk = new Dictionary<string, object>
        {
            ["kty"] = "RSA",
            ["n"] = WebEncoders.Base64UrlEncode(p.Modulus!),
            ["e"] = WebEncoders.Base64UrlEncode(p.Exponent!),
        };
        return new DbscProofKey("RS256", new RsaSecurityKey(rsa), System.Text.Json.JsonSerializer.Serialize(jwk));
    }

    /// <summary>Creates a signed DBSC proof JWT with the public key in the header (registration shape).</summary>
    public string CreateProof(string jti, bool includeJwkHeader = true, string? authorization = null, DateTimeOffset? expires = null)
    {
        var headerClaims = new Dictionary<string, object> { ["typ"] = "dbsc+jwt" };
        if (includeJwkHeader)
        {
            headerClaims["jwk"] = System.Text.Json.JsonDocument.Parse(PublicJwkJson).RootElement;
        }

        var claims = new Dictionary<string, object> { ["jti"] = jti };
        if (authorization is not null)
        {
            claims["authorization"] = authorization;
        }

        var descriptor = new SecurityTokenDescriptor
        {
            Claims = claims,
            SigningCredentials = new SigningCredentials(_signingKey, Algorithm),
            AdditionalHeaderClaims = headerClaims,
        };

        if (expires is not null)
        {
            descriptor.Expires = expires.Value.UtcDateTime;
        }

        return s_handler.CreateToken(descriptor);
    }

    /// <summary>
    /// Builds an unsigned (empty-signature) compact JWS with caller-controlled header fields, used to
    /// exercise the validator's pre-signature gates (typ, alg, key selection) in isolation.
    /// </summary>
    public static string CreateUnsignedToken(string? typ, string? alg, string? jwkJson, string jti)
    {
        var header = new Dictionary<string, object>();
        if (typ is not null)
        {
            header["typ"] = typ;
        }
        if (alg is not null)
        {
            header["alg"] = alg;
        }
        if (jwkJson is not null)
        {
            header["jwk"] = System.Text.Json.JsonDocument.Parse(jwkJson).RootElement;
        }

        var payload = new Dictionary<string, object> { ["jti"] = jti };

        var headerSegment = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(header)));
        var payloadSegment = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(payload)));
        return $"{headerSegment}.{payloadSegment}.";
    }

    /// <summary>Flips one byte of the signature segment to produce a tampered token.</summary>
    public static string TamperSignature(string jwt)
    {
        var lastDot = jwt.LastIndexOf('.');
        var sig = jwt[(lastDot + 1)..];
        var flipped = sig[0] == 'A' ? 'B' : 'A';
        return string.Concat(jwt.AsSpan(0, lastDot + 1), flipped.ToString(), sig.AsSpan(1));
    }
}
