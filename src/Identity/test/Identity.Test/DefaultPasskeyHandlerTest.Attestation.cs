// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public partial class DefaultPasskeyHandlerTest
{
    [Fact]
    public async Task Attestation_CanSucceed()
    {
        var test = new AttestationTest();

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Attestation_Fails_WhenCredentialIdIsMissing()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            Assert.True(credentialJson.Remove("id"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'id'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Attestation_Fails_WhenCredentialIdIsNotString(string jsonValue)
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["id"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenCredentialIdIsNotBase64UrlEncoded()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var base64UrlCredentialId = (string)credentialJson["id"]!;
            var rawCredentialId = Base64Url.DecodeFromChars(base64UrlCredentialId);
            var base64CredentialId = Convert.ToBase64String(rawCredentialId) + "==";
            credentialJson["id"] = base64CredentialId;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("base64url string", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenCredentialTypeIsMissing()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            Assert.True(credentialJson.Remove("type"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'type'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Attestation_Fails_WhenCredentialTypeIsNotString(string jsonValue)
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["type"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenCredentialTypeIsNotPublicKey()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["type"] = "unexpected-value";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("Expected credential type 'public-key', got 'unexpected-value'", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenCredentialResponseIsMissing()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            Assert.True(credentialJson.Remove("response"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'response'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("\"hello\"")]
    public async Task Attestation_Fails_WhenCredentialResponseIsNotAnObject(string jsonValue)
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonIsMissing()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var response = credentialJson["response"]!.AsObject();
            Assert.True(response.Remove("clientDataJSON"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'clientDataJSON'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Attestation_Fails_WhenClientDataJsonIsNotString(string jsonValue)
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["clientDataJSON"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonIsEmptyString()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["clientDataJSON"] = "";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestationObjectIsMissing()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var response = credentialJson["response"]!.AsObject();
            Assert.True(response.Remove("attestationObject"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'attestationObject'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Attestation_Fails_WhenAttestationObjectIsNotString(string jsonValue)
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["attestationObject"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestationObjectIsEmptyString()
    {
        var test = new AttestationTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["attestationObject"] = "";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation object had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonTypeIsMissing()
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            Assert.True(clientDataJson.Remove("type"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'type'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Attestation_Fails_WhenClientDataJsonTypeIsNotString(string jsonValue)
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["type"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("webauthn.get")]
    [InlineData("unexpected-value")]
    public async Task Attestation_Fails_WhenClientDataJsonTypeIsNotExpected(string value)
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["type"] = value;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("Expected the client data JSON 'type' field to be 'webauthn.create'", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonChallengeIsMissing()
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            Assert.True(clientDataJson.Remove("challenge"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'challenge'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Attestation_Fails_WhenClientDataJsonChallengeIsNotString(string jsonValue)
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["challenge"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonChallengeIsEmptyString()
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["challenge"] = "";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator response challenge does not match original challenge", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonChallengeIsNotBase64UrlEncoded()
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            var base64UrlChallenge = (string)clientDataJson["challenge"]!;
            var rawChallenge = Base64Url.DecodeFromChars(base64UrlChallenge);
            var base64Challenge = Convert.ToBase64String(rawChallenge) + "==";
            clientDataJson["challenge"] = base64Challenge;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
        Assert.Contains("base64url string", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonChallengeIsNotRequestChallenge()
    {
        var test = new AttestationTest();
        var modifiedChallenge = (byte[])[.. test.Challenge.Span];
        for (var i = 0; i < modifiedChallenge.Length; i++)
        {
            modifiedChallenge[i]++;
        }

        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["challenge"] = Base64Url.EncodeToString(modifiedChallenge);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator response challenge does not match original challenge", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonOriginIsMissing()
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            Assert.True(clientDataJson.Remove("origin"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'origin'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Attestation_Fails_WhenClientDataJsonOriginIsNotString(string jsonValue)
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["origin"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonOriginIsEmptyString()
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["origin"] = "";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator response had an invalid origin ''", result.Failure.Message);
    }

    [Theory]
    [InlineData("https://example.com", "http://example.com")]
    [InlineData("http://example.com", "https://example.com")]
    [InlineData("https://example.com", "https://foo.example.com")]
    [InlineData("https://example.com", "https://example.com:5000")]
    public async Task Attestation_Fails_WhenClientDataJsonOriginDoesNotMatchTheExpectedOrigin(string expectedOrigin, string returnedOrigin)
    {
        var test = new AttestationTest
        {
            Origin = expectedOrigin,
        };
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["origin"] = returnedOrigin;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith($"The authenticator response had an invalid origin '{returnedOrigin}'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"hello\"")]
    public async Task Attestation_Fails_WhenClientDataJsonTokenBindingIsNotObject(string jsonValue)
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["tokenBinding"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonTokenBindingStatusIsMissing()
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["tokenBinding"] = JsonNode.Parse("{}");
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'status'", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenClientDataJsonTokenBindingStatusIsInvalid()
    {
        var test = new AttestationTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["tokenBinding"] = JsonNode.Parse("""
                {
                  "status": "unexpected-value"
                }
                """);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("Invalid token binding status 'unexpected-value'", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Succeeds_WhenAuthDataContainsExtensionData()
    {
        var test = new AttestationTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.HasExtensionData,
            Extensions = (byte[])[0xA0] // Empty CBOR map.
        });

        var result = await test.RunAsync();
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAuthDataIsNotBackupEligibleButBackedUp()
    {
        var test = new AttestationTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = (args.Flags | AuthenticatorDataFlags.BackedUp) & ~AuthenticatorDataFlags.BackupEligible,
        });

        var result = await test.RunAsync();
        Assert.False(result.Succeeded);
        Assert.StartsWith("The credential is backed up, but the authenticator data flags did not have the 'BackupEligible' flag", result.Failure.Message);
    }

    [Theory]
    [InlineData(PasskeyOptions.CredentialBackupPolicy.Allowed)]
    [InlineData(PasskeyOptions.CredentialBackupPolicy.Required)]
    public async Task Attestation_Succeeds_WhenAuthDataIsBackupEligible(PasskeyOptions.CredentialBackupPolicy backupEligibility)
    {
        var test = new AttestationTest();
        test.IdentityOptions.Passkey.BackupEligibleCredentialPolicy = backupEligibility;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible,
        });

        var result = await test.RunAsync();
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAuthDataIsBackupEligibleButDisallowed()
    {
        var test = new AttestationTest();
        test.IdentityOptions.Passkey.BackupEligibleCredentialPolicy = PasskeyOptions.CredentialBackupPolicy.Disallowed;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Credential backup eligibility is disallowed, but the credential was eligible for backup",
            result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAuthDataIsNotBackupEligibleButRequired()
    {
        var test = new AttestationTest();
        test.IdentityOptions.Passkey.BackupEligibleCredentialPolicy = PasskeyOptions.CredentialBackupPolicy.Required;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags & ~AuthenticatorDataFlags.BackupEligible,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Credential backup eligibility is required, but the credential was not eligible for backup",
            result.Failure.Message);
    }

    [Theory]
    [InlineData(PasskeyOptions.CredentialBackupPolicy.Allowed)]
    [InlineData(PasskeyOptions.CredentialBackupPolicy.Required)]
    public async Task Attestation_Fails_WhenAuthDataIsBackedUp(PasskeyOptions.CredentialBackupPolicy backedUpPolicy)
    {
        var test = new AttestationTest();
        test.IdentityOptions.Passkey.BackedUpCredentialPolicy = backedUpPolicy;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible | AuthenticatorDataFlags.BackedUp,
        });

        var result = await test.RunAsync();
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAuthDataIsBackedUpButDisallowed()
    {
        var test = new AttestationTest();
        test.IdentityOptions.Passkey.BackedUpCredentialPolicy = PasskeyOptions.CredentialBackupPolicy.Disallowed;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible | AuthenticatorDataFlags.BackedUp,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Credential backup is disallowed, but the credential was backed up",
            result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAuthDataIsNotBackedUpButRequired()
    {
        var test = new AttestationTest();
        test.IdentityOptions.Passkey.BackedUpCredentialPolicy = PasskeyOptions.CredentialBackupPolicy.Required;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags & ~AuthenticatorDataFlags.BackedUp,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Credential backup is required, but the credential was not backed up",
            result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestationObjectIsNotCborEncoded()
    {
        var test = new AttestationTest();
        test.AttestationObject.Transform(bytes => Encoding.UTF8.GetBytes("Not a CBOR map"));

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation object had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestationObjectFmtIsMissing()
    {
        var test = new AttestationTest();
        test.AttestationObjectArgs.Transform(args => args with
        {
            Format = null,
            CborMapLength = args.CborMapLength - 1, // Because of the removed format
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation object did not include an attestation statement format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestationObjectStmtFieldIsMissing()
    {
        var test = new AttestationTest();
        test.AttestationObjectArgs.Transform(args => args with
        {
            AttestationStatement = null,
            CborMapLength = args.CborMapLength - 1, // Because of the removed attestation statement
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation object did not include an attestation statement", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestationObjectAuthDataFieldIsMissing()
    {
        var test = new AttestationTest();
        test.AttestationObjectArgs.Transform(args => args with
        {
            AuthenticatorData = null,
            CborMapLength = args.CborMapLength - 1, // Because of the removed authenticator data
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attestation object did not include authenticator data", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestationObjectAuthDataFieldIsEmpty()
    {
        var test = new AttestationTest();
        test.AttestationObjectArgs.Transform(args => args with
        {
            AuthenticatorData = ReadOnlyMemory<byte>.Empty,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator data had an invalid byte count of 0", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestedCredentialDataIsPresentButWithoutFlag()
    {
        var test = new AttestationTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            // Remove the flag without removing the attested credential data
            Flags = args.Flags & ~AuthenticatorDataFlags.HasAttestedCredentialData,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator data had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestedCredentialDataIsNotPresentButWithFlag()
    {
        var test = new AttestationTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            // Remove the attested credential data without changing the flags
            AttestedCredentialData = null,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The attested credential data had an invalid byte count of 0", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestedCredentialDataIsNotPresent()
    {
        var test = new AttestationTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags & ~AuthenticatorDataFlags.HasAttestedCredentialData,
            AttestedCredentialData = null,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("No attested credential data was provided by the authenticator", result.Failure.Message);
    }

    [Fact]
    public async Task Attestation_Fails_WhenAttestedCredentialDataHasExtraBytes()
    {
        var test = new AttestationTest();
        test.AttestedCredentialData.Transform(attestedCredentialData =>
        {
            return (byte[])[.. attestedCredentialData.Span, 0xFF, 0xFF, 0xFF, 0xFF];
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator data had an invalid format", result.Failure.Message);
    }

    [Theory]
    [InlineData((int)COSEAlgorithmIdentifier.PS256)]
    [InlineData((int)COSEAlgorithmIdentifier.PS384)]
    [InlineData((int)COSEAlgorithmIdentifier.PS512)]
    [InlineData((int)COSEAlgorithmIdentifier.RS256)]
    [InlineData((int)COSEAlgorithmIdentifier.RS384)]
    [InlineData((int)COSEAlgorithmIdentifier.RS512)]
    [InlineData((int)COSEAlgorithmIdentifier.ES256)]
    [InlineData((int)COSEAlgorithmIdentifier.ES384)]
    [InlineData((int)COSEAlgorithmIdentifier.ES512)]
    public async Task Attestation_Succeeds_WithSupportedAlgorithms(int algorithm)
    {
        var test = new AttestationTest
        {
            Algorithm = (COSEAlgorithmIdentifier)algorithm,
        };

        // Only include the specific algorithm we're testing,
        // just to sanity check that we're using the algorithm we expect
        test.SupportedPublicKeyCredentialParameters.Transform(_ => [new((COSEAlgorithmIdentifier)algorithm)]);

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData((int)COSEAlgorithmIdentifier.PS256)]
    [InlineData((int)COSEAlgorithmIdentifier.PS384)]
    [InlineData((int)COSEAlgorithmIdentifier.PS512)]
    [InlineData((int)COSEAlgorithmIdentifier.RS256)]
    [InlineData((int)COSEAlgorithmIdentifier.RS384)]
    [InlineData((int)COSEAlgorithmIdentifier.RS512)]
    [InlineData((int)COSEAlgorithmIdentifier.ES256)]
    [InlineData((int)COSEAlgorithmIdentifier.ES384)]
    [InlineData((int)COSEAlgorithmIdentifier.ES512)]
    public async Task Attestation_Fails_WhenAlgorithmIsNotSupported(int algorithm)
    {
        var test = new AttestationTest
        {
            Algorithm = (COSEAlgorithmIdentifier)algorithm,
        };
        test.SupportedPublicKeyCredentialParameters.Transform(parameters =>
        {
            // Exclude the specific algorithm we're testing, which should cause the failure
            return [.. parameters.Where(p => p.Alg != (COSEAlgorithmIdentifier)algorithm)];
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The credential public key algorithm does not match any of the supported algorithms", result.Failure.Message);
    }

    private sealed class AttestationTest : PasskeyTestBase<PasskeyAttestationResult>
    {
        private static readonly byte[] _defaultChallenge = [1, 2, 3, 4, 5, 6, 7, 8];
        private static readonly byte[] _defaultCredentialId = [1, 2, 3, 4, 5, 6, 7, 8];

        public IdentityOptions IdentityOptions { get; } = new();
        public string? RpId { get; set; } = "example.com";
        public string? RpName { get; set; } = "Example";
        public string? UserId { get; set; } = "df0a3af4-bd65-440f-82bd-5b839e300dcd";
        public string? UserName { get; set; } = "johndoe";
        public string? UserDisplayName { get; set; } = "John Doe";
        public string? Origin { get; set; } = "https://example.com";
        public COSEAlgorithmIdentifier Algorithm { get; set; } = COSEAlgorithmIdentifier.ES256;
        public ReadOnlyMemory<byte> Challenge { get; set; } = _defaultChallenge;
        public ReadOnlyMemory<byte> CredentialId { get; set; } = _defaultCredentialId;
        public ComputedValue<IReadOnlyList<PublicKeyCredentialParameters>> SupportedPublicKeyCredentialParameters { get; } = new();
        public ComputedValue<AttestedCredentialDataArgs> AttestedCredentialDataArgs { get; } = new();
        public ComputedValue<AuthenticatorDataArgs> AuthenticatorDataArgs { get; } = new();
        public ComputedValue<AttestationObjectArgs> AttestationObjectArgs { get; } = new();
        public ComputedValue<ReadOnlyMemory<byte>> AttestedCredentialData { get; } = new();
        public ComputedValue<ReadOnlyMemory<byte>> AuthenticatorData { get; } = new();
        public ComputedValue<ReadOnlyMemory<byte>> AttestationObject { get; } = new();
        public ComputedJsonObject OriginalOptionsJson { get; } = new();
        public ComputedJsonObject ClientDataJson { get; } = new();
        public ComputedJsonObject CredentialJson { get; } = new();

        protected override async Task<PasskeyAttestationResult> RunCoreAsync()
        {
            var identityOptions = Options.Create(IdentityOptions);
            var handler = new DefaultPasskeyHandler<PocoUser>(identityOptions);
            var supportedPublicKeyCredentialParameters = SupportedPublicKeyCredentialParameters.Compute(
                PublicKeyCredentialParameters.AllSupportedParameters);
            var pubKeyCredParamsJson = JsonSerializer.Serialize(
                supportedPublicKeyCredentialParameters,
                IdentityJsonSerializerContext.Default.IReadOnlyListPublicKeyCredentialParameters);
            var originalOptionsJson = OriginalOptionsJson.Compute($$"""
                {
                  "rp": {
                    "name": {{ToJsonValue(RpName)}},
                    "id": {{ToJsonValue(RpId)}}
                  },
                  "user": {
                    "id": {{ToBase64UrlJsonValue(UserId)}},
                    "name": {{ToJsonValue(UserName)}},
                    "displayName": {{ToJsonValue(UserDisplayName)}}
                  },
                  "challenge": {{ToBase64UrlJsonValue(Challenge)}},
                  "pubKeyCredParams": {{pubKeyCredParamsJson}},
                  "timeout": 60000,
                  "excludeCredentials": [],
                  "attestation": "none",
                  "hints": [],
                  "extensions": {}
                }
                """);
            var credential = CredentialKeyPair.Generate(Algorithm);
            var credentialPublicKey = credential.EncodePublicKeyCbor();
            var attestedCredentialDataArgs = AttestedCredentialDataArgs.Compute(new()
            {
                CredentialId = CredentialId,
                CredentialPublicKey = credentialPublicKey,
            });
            var attestedCredentialData = AttestedCredentialData.Compute(MakeAttestedCredentialData(attestedCredentialDataArgs));
            var authenticatorDataArgs = AuthenticatorDataArgs.Compute(new()
            {
                RpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(RpId ?? string.Empty)),
                AttestedCredentialData = attestedCredentialData,
                Flags = AuthenticatorDataFlags.UserPresent | AuthenticatorDataFlags.HasAttestedCredentialData,
            });
            var authenticatorData = AuthenticatorData.Compute(MakeAuthenticatorData(authenticatorDataArgs));
            var attestationObjectArgs = AttestationObjectArgs.Compute(new()
            {
                AuthenticatorData = authenticatorData,
            });
            var attestationObject = AttestationObject.Compute(MakeAttestationObject(attestationObjectArgs));
            var clientDataJson = ClientDataJson.Compute($$"""
                {
                  "challenge": {{ToBase64UrlJsonValue(Challenge)}},
                  "origin": {{ToJsonValue(Origin)}},
                  "type": "webauthn.create"
                }
                """);
            var credentialJson = CredentialJson.Compute($$"""
                {
                  "id": {{ToBase64UrlJsonValue(CredentialId)}},
                  "response": {
                    "attestationObject": {{ToBase64UrlJsonValue(attestationObject)}},
                    "clientDataJSON": {{ToBase64UrlJsonValue(clientDataJson)}},
                    "transports": [
                      "internal"
                    ]
                  },
                  "type": "public-key",
                  "clientExtensionResults": {},
                  "authenticatorAttachment": "platform"
                }
                """);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Request.Headers.Origin).Returns(new StringValues(Origin));

            var userManager = MockHelpers.MockUserManager<PocoUser>();

            var context = new PasskeyAttestationContext<PocoUser>
            {
                CredentialJson = credentialJson,
                OriginalOptionsJson = originalOptionsJson,
                HttpContext = httpContext.Object,
                UserManager = userManager.Object,
            };

            return await handler.PerformAttestationAsync(context);
        }
    }
}
