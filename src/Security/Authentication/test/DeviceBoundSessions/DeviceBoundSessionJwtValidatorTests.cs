// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionJwtValidatorTests
{
    private static readonly DeviceBoundSessionJwtValidator Validator = new(NullLogger<DeviceBoundSessionJwtValidator>.Instance);

    [Fact]
    public async Task ValidateAsync_ValidEs256Proof_WithJwkHeader_Succeeds()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("challenge-1");

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.NotNull(result);
        Assert.Equal("ES256", result.Algorithm);
        Assert.Equal("challenge-1", result.Challenge);
    }

    [Fact]
    public async Task ValidateAsync_ValidRs256Proof_WithJwkHeader_Succeeds()
    {
        var key = DbscProofKey.CreateRs256();
        var proof = key.CreateProof("challenge-2");

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.NotNull(result);
        Assert.Equal("RS256", result.Algorithm);
        Assert.Equal("challenge-2", result.Challenge);
    }

    [Fact]
    public async Task ValidateAsync_RefreshShape_UsesProvidedStoredKey_WhenJwkHeaderOmitted()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("challenge-3", includeJwkHeader: false);

        var result = await Validator.ValidateAsync(proof, key.PublicJwkJson, expectedChallenge: null);

        Assert.NotNull(result);
        Assert.Equal("challenge-3", result.Challenge);
    }

    [Fact]
    public async Task ValidateAsync_TamperedSignature_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = DbscProofKey.TamperSignature(key.CreateProof("challenge-4"));

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_WrongStoredKey_ReturnsNull()
    {
        var signingKey = DbscProofKey.CreateEs256();
        var otherKey = DbscProofKey.CreateEs256();
        var proof = signingKey.CreateProof("challenge-5", includeJwkHeader: false);

        var result = await Validator.ValidateAsync(proof, otherKey.PublicJwkJson, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_MissingJwkHeaderAndNoStoredKey_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("challenge-6", includeJwkHeader: false);

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_MalformedToken_ReturnsNull()
    {
        var result = await Validator.ValidateAsync("not-a-jwt", publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_ChallengeMismatch_WhenExpectedChallengeProvided_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("actual-challenge");

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: "different-challenge");

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_WrongTyp_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var token = DbscProofKey.CreateUnsignedToken(typ: "JWT", alg: "ES256", jwkJson: key.PublicJwkJson, jti: "c");

        var result = await Validator.ValidateAsync(token, publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_MissingAlg_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var token = DbscProofKey.CreateUnsignedToken(typ: "dbsc+jwt", alg: null, jwkJson: key.PublicJwkJson, jti: "c");

        var result = await Validator.ValidateAsync(token, publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("none")]
    [InlineData("HS256")]
    [InlineData("ES384")]
    [InlineData("RS384")]
    public async Task ValidateAsync_UnsupportedAlg_ReturnsNull(string alg)
    {
        var key = DbscProofKey.CreateEs256();
        var token = DbscProofKey.CreateUnsignedToken(typ: "dbsc+jwt", alg: alg, jwkJson: key.PublicJwkJson, jti: "c");

        var result = await Validator.ValidateAsync(token, publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_AlgKeyTypeMismatch_ReturnsNull()
    {
        // Proof is signed ES256 but validated against an RSA stored key: the EC switch arm's
        // kty guard rejects the mismatch before signature validation.
        var ecKey = DbscProofKey.CreateEs256();
        var rsaKey = DbscProofKey.CreateRs256();
        var proof = ecKey.CreateProof("c", includeJwkHeader: false);

        var result = await Validator.ValidateAsync(proof, rsaKey.PublicJwkJson, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_MalformedStoredJwk_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("c", includeJwkHeader: false);

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: "{ not valid json", expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_ExpectedChallengeMatches_Succeeds()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("the-challenge");

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: "the-challenge");

        Assert.NotNull(result);
        Assert.Equal("the-challenge", result.Challenge);
    }

    [Fact]
    public async Task ValidateAsync_ExtractsAuthorizationClaim()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("c", authorization: "auth-token-value");

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.NotNull(result);
        Assert.Equal("auth-token-value", result.Authorization);
    }

    [Fact]
    public async Task ValidateAsync_ExpiredProof_StillValidates_BecauseLifetimeNotChecked()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("c", expires: DateTimeOffset.UtcNow.AddMinutes(-10));

        var result = await Validator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.NotNull(result);
    }
}
