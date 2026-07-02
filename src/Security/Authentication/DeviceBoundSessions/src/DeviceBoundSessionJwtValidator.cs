// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

/// <summary>
/// Validates DBSC proof JWTs (typ: "dbsc+jwt") signed with ES256 or RS256.
/// </summary>
internal sealed class DeviceBoundSessionJwtValidator
{
    private static readonly JsonWebTokenHandler _tokenHandler = new();
    private readonly ILogger<DeviceBoundSessionJwtValidator> _logger;

    public DeviceBoundSessionJwtValidator(ILogger<DeviceBoundSessionJwtValidator> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates a DBSC proof JWT and extracts its claims.
    /// </summary>
    /// <param name="jwt">The raw JWT string.</param>
    /// <param name="publicKeyJwk">The JWK JSON of the public key to validate against. If null, extracts from JWT header.</param>
    /// <param name="expectedChallenge">The expected challenge value (jti claim).</param>
    /// <returns>The parsed result, or null if validation fails.</returns>
    public async Task<DeviceBoundSessionJwtResult?> ValidateAsync(string jwt, string? publicKeyJwk, string? expectedChallenge)
    {
        JsonWebToken token;
        try
        {
            token = new JsonWebToken(jwt);
        }
        catch (ArgumentException)
        {
            _logger.ProofMalformed();
            return null;
        }

        if (!token.TryGetHeaderValue<string>("typ", out var tokenType) ||
            !string.Equals(tokenType, "dbsc+jwt", StringComparison.Ordinal))
        {
            _logger.ProofWrongType();
            return null;
        }

        if (!token.TryGetHeaderValue<string>("alg", out var algorithm) || string.IsNullOrEmpty(algorithm))
        {
            _logger.ProofMissingAlgorithm();
            return null;
        }

        string? jwkJson = publicKeyJwk;
        if (jwkJson is null)
        {
            if (!token.TryGetHeaderValue<JsonElement>("jwk", out var jwkElement))
            {
                _logger.ProofMissingKey();
                return null;
            }

            jwkJson = jwkElement.GetRawText();
        }

        var securityKey = CreateSecurityKey(jwkJson, algorithm);
        if (securityKey is null)
        {
            _logger.ProofUnsupportedKey();
            return null;
        }

        var validationResult = await _tokenHandler.ValidateTokenAsync(jwt, new TokenValidationParameters
        {
            IssuerSigningKey = securityKey,
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = false,
        });

        if (!validationResult.IsValid)
        {
            _logger.ProofSignatureInvalid();
            return null;
        }

        token.TryGetPayloadValue("jti", out string? challenge);
        if (expectedChallenge is not null && !string.Equals(challenge, expectedChallenge, StringComparison.Ordinal))
        {
            _logger.ProofChallengeMismatch();
            return null;
        }

        token.TryGetPayloadValue("authorization", out string? authorization);

        return new DeviceBoundSessionJwtResult
        {
            Algorithm = algorithm,
            PublicKeyJwk = jwkJson,
            Challenge = challenge,
            Authorization = authorization,
        };
    }

    private static SecurityKey? CreateSecurityKey(string jwkJson, string algorithm)
    {
        JsonWebKey jsonWebKey;
        try
        {
            jsonWebKey = new JsonWebKey(jwkJson);
        }
        catch (ArgumentException)
        {
            return null;
        }

        return algorithm switch
        {
            DeviceBoundSessionConstants.Es256 when string.Equals(jsonWebKey.Kty, "EC", StringComparison.Ordinal) && string.Equals(jsonWebKey.Crv, "P-256", StringComparison.Ordinal) => jsonWebKey,
            DeviceBoundSessionConstants.Rs256 when string.Equals(jsonWebKey.Kty, "RSA", StringComparison.Ordinal) => jsonWebKey,
            _ => null,
        };
    }
}
