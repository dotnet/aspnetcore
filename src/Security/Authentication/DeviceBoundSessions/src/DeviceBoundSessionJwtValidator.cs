// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Validates DBSC proof JWTs (typ: "dbsc+jwt") signed with ES256 or RS256.
/// </summary>
internal static class DeviceBoundSessionJwtValidator
{
    /// <summary>
    /// Validates a DBSC proof JWT and extracts its claims.
    /// </summary>
    /// <param name="jwt">The raw JWT string.</param>
    /// <param name="publicKeyJwk">The JWK JSON of the public key to validate against. If null, extracts from JWT header.</param>
    /// <param name="expectedChallenge">The expected challenge value (jti claim).</param>
    /// <returns>The parsed result, or null if validation fails.</returns>
    public static DeviceBoundSessionJwtResult? Validate(string jwt, string? publicKeyJwk, string? expectedChallenge)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var headerJson = Base64UrlDecode(parts[0]);
        var payloadJson = Base64UrlDecode(parts[1]);
        var signature = Base64UrlDecodeBytes(parts[2]);

        if (headerJson is null || payloadJson is null || signature is null)
        {
            return null;
        }

        // Parse header
        JsonElement header;
        try
        {
            header = JsonDocument.Parse(headerJson).RootElement;
        }
        catch (JsonException)
        {
            return null;
        }

        // Validate typ
        if (!header.TryGetProperty("typ", out var typElement) ||
            !string.Equals(typElement.GetString(), "dbsc+jwt", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Get algorithm
        if (!header.TryGetProperty("alg", out var algElement))
        {
            return null;
        }
        var algorithm = algElement.GetString();

        // Get JWK - from parameter or from header (registration)
        string? jwkJson = publicKeyJwk;
        if (jwkJson is null)
        {
            if (!header.TryGetProperty("jwk", out var jwkElement))
            {
                return null;
            }
            jwkJson = jwkElement.GetRawText();
        }

        // Validate signature
        var signedData = Encoding.ASCII.GetBytes($"{parts[0]}.{parts[1]}");
        if (!VerifySignature(algorithm, jwkJson, signedData, signature))
        {
            return null;
        }

        // Parse payload
        JsonElement payload;
        try
        {
            payload = JsonDocument.Parse(payloadJson).RootElement;
        }
        catch (JsonException)
        {
            return null;
        }

        // Validate jti (challenge)
        string? jti = null;
        if (payload.TryGetProperty("jti", out var jtiElement))
        {
            jti = jtiElement.GetString();
        }

        if (expectedChallenge is not null && !string.Equals(jti, expectedChallenge, StringComparison.Ordinal))
        {
            return null;
        }

        // Extract authorization claim if present
        string? authorization = null;
        if (payload.TryGetProperty("authorization", out var authElement))
        {
            authorization = authElement.GetString();
        }

        return new DeviceBoundSessionJwtResult
        {
            Algorithm = algorithm,
            PublicKeyJwk = jwkJson,
            Challenge = jti,
            Authorization = authorization
        };
    }

    /// <summary>
    /// Extracts the JWK from a DBSC registration JWT header without full validation.
    /// Used during registration when we don't yet have a stored key.
    /// </summary>
    public static string? ExtractPublicKeyJwk(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length != 3)
        {
            return null;
        }

        var headerJson = Base64UrlDecode(parts[0]);
        if (headerJson is null)
        {
            return null;
        }

        try
        {
            var header = JsonDocument.Parse(headerJson).RootElement;
            if (header.TryGetProperty("jwk", out var jwkElement))
            {
                return jwkElement.GetRawText();
            }
        }
        catch (JsonException)
        {
            // Ignore parse errors
        }

        return null;
    }

    private static bool VerifySignature(string? algorithm, string jwkJson, byte[] signedData, byte[] signature)
    {
        return algorithm switch
        {
            "ES256" => VerifyES256(jwkJson, signedData, signature),
            "RS256" => VerifyRS256(jwkJson, signedData, signature),
            _ => false
        };
    }

    private static bool VerifyES256(string jwkJson, byte[] signedData, byte[] signature)
    {
        try
        {
            using var doc = JsonDocument.Parse(jwkJson);
            var jwk = doc.RootElement;

            if (!jwk.TryGetProperty("kty", out var kty) || kty.GetString() != "EC")
            {
                return false;
            }
            if (!jwk.TryGetProperty("crv", out var crv) || crv.GetString() != "P-256")
            {
                return false;
            }
            if (!jwk.TryGetProperty("x", out var xProp) || !jwk.TryGetProperty("y", out var yProp))
            {
                return false;
            }

            var x = Base64UrlDecodeBytes(xProp.GetString()!);
            var y = Base64UrlDecodeBytes(yProp.GetString()!);
            if (x is null || y is null)
            {
                return false;
            }

            var parameters = new ECParameters
            {
                Curve = ECCurve.NamedCurves.nistP256,
                Q = new ECPoint { X = x, Y = y }
            };

            using var ecdsa = ECDsa.Create(parameters);
            return ecdsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence)
                || ecdsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool VerifyRS256(string jwkJson, byte[] signedData, byte[] signature)
    {
        try
        {
            using var doc = JsonDocument.Parse(jwkJson);
            var jwk = doc.RootElement;

            if (!jwk.TryGetProperty("kty", out var kty) || kty.GetString() != "RSA")
            {
                return false;
            }
            if (!jwk.TryGetProperty("n", out var nProp) || !jwk.TryGetProperty("e", out var eProp))
            {
                return false;
            }

            var n = Base64UrlDecodeBytes(nProp.GetString()!);
            var e = Base64UrlDecodeBytes(eProp.GetString()!);
            if (n is null || e is null)
            {
                return false;
            }

            var parameters = new RSAParameters
            {
                Modulus = n,
                Exponent = e
            };

            using var rsa = RSA.Create(parameters);
            return rsa.VerifyData(signedData, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string? Base64UrlDecode(string input)
    {
        var bytes = Base64UrlDecodeBytes(input);
        return bytes is null ? null : Encoding.UTF8.GetString(bytes);
    }

    private static byte[]? Base64UrlDecodeBytes(string input)
    {
        // Replace URL-safe characters and add padding
        var base64 = input.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }

        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}

/// <summary>
/// Result of validating a DBSC proof JWT.
/// </summary>
internal sealed class DeviceBoundSessionJwtResult
{
    public required string? Algorithm { get; init; }
    public required string PublicKeyJwk { get; init; }
    public string? Challenge { get; init; }
    public string? Authorization { get; init; }
}
