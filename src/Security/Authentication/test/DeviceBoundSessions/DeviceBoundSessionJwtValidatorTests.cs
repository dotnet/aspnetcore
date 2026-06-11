// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.DeviceBoundSessions;

public class DeviceBoundSessionJwtValidatorTests
{
    [Fact]
    public async Task ValidateAsync_ValidEs256Proof_WithJwkHeader_Succeeds()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("challenge-1");

        var result = await DeviceBoundSessionJwtValidator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.NotNull(result);
        Assert.Equal("ES256", result.Algorithm);
        Assert.Equal("challenge-1", result.Challenge);
    }

    [Fact]
    public async Task ValidateAsync_ValidRs256Proof_WithJwkHeader_Succeeds()
    {
        var key = DbscProofKey.CreateRs256();
        var proof = key.CreateProof("challenge-2");

        var result = await DeviceBoundSessionJwtValidator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.NotNull(result);
        Assert.Equal("RS256", result.Algorithm);
        Assert.Equal("challenge-2", result.Challenge);
    }

    [Fact]
    public async Task ValidateAsync_RefreshShape_UsesProvidedStoredKey_WhenJwkHeaderOmitted()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("challenge-3", includeJwkHeader: false);

        var result = await DeviceBoundSessionJwtValidator.ValidateAsync(proof, key.PublicJwkJson, expectedChallenge: null);

        Assert.NotNull(result);
        Assert.Equal("challenge-3", result.Challenge);
    }

    [Fact]
    public async Task ValidateAsync_TamperedSignature_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = DbscProofKey.TamperSignature(key.CreateProof("challenge-4"));

        var result = await DeviceBoundSessionJwtValidator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_WrongStoredKey_ReturnsNull()
    {
        var signingKey = DbscProofKey.CreateEs256();
        var otherKey = DbscProofKey.CreateEs256();
        var proof = signingKey.CreateProof("challenge-5", includeJwkHeader: false);

        var result = await DeviceBoundSessionJwtValidator.ValidateAsync(proof, otherKey.PublicJwkJson, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_MissingJwkHeaderAndNoStoredKey_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("challenge-6", includeJwkHeader: false);

        var result = await DeviceBoundSessionJwtValidator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_MalformedToken_ReturnsNull()
    {
        var result = await DeviceBoundSessionJwtValidator.ValidateAsync("not-a-jwt", publicKeyJwk: null, expectedChallenge: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task ValidateAsync_ChallengeMismatch_WhenExpectedChallengeProvided_ReturnsNull()
    {
        var key = DbscProofKey.CreateEs256();
        var proof = key.CreateProof("actual-challenge");

        var result = await DeviceBoundSessionJwtValidator.ValidateAsync(proof, publicKeyJwk: null, expectedChallenge: "different-challenge");

        Assert.Null(result);
    }
}
