// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers.Binary;
using System.Buffers.Text;
using System.Formats.Cbor;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class DefaultPasskeyHandlerTest
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

    [Fact]
    public async Task Assertion_CanSucceed()
    {
        var test = new AssertionTest();

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Assertion_Fails_WhenCredentialIdIsMissing()
    {
        var test = new AssertionTest();

        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            Assert.True(credentialJson.Remove("id"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'id'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Assertion_Fails_WhenCredentialIdIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["id"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenCredentialIdIsNotBase64UrlEncoded()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var base64UrlCredentialId = (string)credentialJson["id"]!;
            var rawCredentialId = Base64Url.DecodeFromChars(base64UrlCredentialId);
            var base64CredentialId = Convert.ToBase64String(rawCredentialId) + "==";
            credentialJson["id"] = base64CredentialId;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("base64url string", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenCredentialTypeIsMissing()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            Assert.True(credentialJson.Remove("type"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'type'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Assertion_Fails_WhenCredentialTypeIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["type"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenCredentialTypeIsNotPublicKey()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["type"] = "unexpected-value";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("Expected credential type 'public-key', got 'unexpected-value'", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenCredentialResponseIsMissing()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            Assert.True(credentialJson.Remove("response"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'response'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("\"hello\"")]
    public async Task Assertion_Fails_WhenCredentialResponseIsNotAnObject(string jsonValue)
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenClientDataJsonIsMissing()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var response = credentialJson["response"]!.AsObject();
            Assert.True(response.Remove("clientDataJSON"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'clientDataJSON'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Assertion_Fails_WhenClientDataJsonIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["clientDataJSON"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenClientDataJsonIsEmptyString()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["clientDataJSON"] = "";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsMissing()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var response = credentialJson["response"]!.AsObject();
            Assert.True(response.Remove("authenticatorData"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'authenticatorData'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["authenticatorData"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsNotBase64UrlEncoded()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var base64UrlAuthenticatorData = (string)credentialJson["response"]!["authenticatorData"]!;
            var rawAuthenticatorData = Base64Url.DecodeFromChars(base64UrlAuthenticatorData);
            var base64AuthenticatorData = Convert.ToBase64String(rawAuthenticatorData) + "==";
            credentialJson["response"]!["authenticatorData"] = base64AuthenticatorData;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("base64url string", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsEmptyString()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["authenticatorData"] = "";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator data had an invalid byte count of 0", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenResponseSignatureIsMissing()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var response = credentialJson["response"]!.AsObject();
            Assert.True(response.Remove("signature"));
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("was missing required properties including: 'signature'", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Assertion_Fails_WhenResponseSignatureIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["signature"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenResponseSignatureIsNotBase64UrlEncoded()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            var base64UrlSignature = (string)credentialJson["response"]!["signature"]!;
            var rawSignature = Base64Url.DecodeFromChars(base64UrlSignature);
            var base64Signature = Convert.ToBase64String(rawSignature) + "==";
            credentialJson["response"]!["signature"] = base64Signature;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
        Assert.Contains("base64url string", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenResponseSignatureIsEmptyString()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["signature"] = "";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion signature was invalid", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenResponseSignatureIsInvalid()
    {
        var test = new AssertionTest();
        test.Signature.Transform(signature =>
        {
            // Add some invalid bytes to the signature
            var invalidSignature = (byte[])[.. signature.Span, 0xFF, 0xFF, 0xFF, 0xFF];
            return invalidSignature;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion signature was invalid", result.Failure.Message);
    }

    [Theory]
    [InlineData("42")]
    [InlineData("{}")]
    public async Task Assertion_Fails_WhenResponseUserHandleIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["userHandle"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion credential JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenResponseUserHandleIsNull()
    {
        var test = new AssertionTest();
        test.CredentialJson.TransformAsJsonObject(credentialJson =>
        {
            credentialJson["response"]!["userHandle"] = null;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator response was missing a user handle", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenClientDataJsonTypeIsMissing()
    {
        var test = new AssertionTest();
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
    public async Task Assertion_Fails_WhenClientDataJsonTypeIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
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
    [InlineData("webauthn.create")]
    [InlineData("unexpected-value")]
    public async Task Assertion_Fails_WhenClientDataJsonTypeIsNotExpected(string value)
    {
        var test = new AssertionTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["type"] = value;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("Expected the client data JSON 'type' field to be 'webauthn.get'", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenClientDataJsonChallengeIsMissing()
    {
        var test = new AssertionTest();
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
    public async Task Assertion_Fails_WhenClientDataJsonChallengeIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["challenge"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenClientDataJsonChallengeIsEmptyString()
    {
        var test = new AssertionTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["challenge"] = "";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator response challenge does not match original challenge", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenClientDataJsonChallengeIsNotBase64UrlEncoded()
    {
        var test = new AssertionTest();
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
    public async Task Assertion_Fails_WhenClientDataJsonChallengeIsNotRequestChallenge()
    {
        var test = new AssertionTest();
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
    public async Task Assertion_Fails_WhenClientDataJsonOriginIsMissing()
    {
        var test = new AssertionTest();
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
    public async Task Assertion_Fails_WhenClientDataJsonOriginIsNotString(string jsonValue)
    {
        var test = new AssertionTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["origin"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenClientDataJsonOriginIsEmptyString()
    {
        var test = new AssertionTest();
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
    public async Task Assertion_Fails_WhenClientDataJsonOriginDoesNotMatchTheExpectedOrigin(string expectedOrigin, string returnedOrigin)
    {
        var test = new AssertionTest
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
    public async Task Assertion_Fails_WhenClientDataJsonTokenBindingIsNotObject(string jsonValue)
    {
        var test = new AssertionTest();
        test.ClientDataJson.TransformAsJsonObject(clientDataJson =>
        {
            clientDataJson["tokenBinding"] = JsonNode.Parse(jsonValue);
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The client data JSON had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenClientDataJsonTokenBindingStatusIsMissing()
    {
        var test = new AssertionTest();
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
    public async Task Assertion_Fails_WhenClientDataJsonTokenBindingStatusIsInvalid()
    {
        var test = new AssertionTest();
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

    private sealed class AttestationTest : ConfigurableTestBase<PasskeyAttestationResult>
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

        protected override async Task<PasskeyAttestationResult> RunTestAsync()
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
            var credential = TestCredentialKeyPair.Generate(Algorithm);
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

    private sealed class AssertionTest : ConfigurableTestBase<PasskeyAssertionResult<PocoUser>>
    {
        private static readonly byte[] _defaultChallenge = [1, 2, 3, 4, 5, 6, 7, 8];
        private static readonly byte[] _defaultCredentialId = [1, 2, 3, 4, 5, 6, 7, 8];

        private readonly List<PublicKeyCredentialDescriptor> _allowCredentials = [];

        public IdentityOptions IdentityOptions { get; } = new();
        public string? RpId { get; set; } = "example.com";
        public string? Origin { get; set; } = "https://example.com";
        public PocoUser User { get; set; } = new()
        {
            Id = "df0a3af4-bd65-440f-82bd-5b839e300dcd",
            UserName = "johndoe",
        };
        public bool IsUserIdentified { get; set; }
        public COSEAlgorithmIdentifier Algorithm { get; set; } = COSEAlgorithmIdentifier.ES256;
        public ReadOnlyMemory<byte> Challenge { get; set; } = _defaultChallenge;
        public ReadOnlyMemory<byte> CredentialId { get; set; } = _defaultCredentialId;
        public ComputedValue<AuthenticatorDataArgs> AuthenticatorDataArgs { get; } = new();
        public ComputedValue<ReadOnlyMemory<byte>> AuthenticatorData { get; } = new();
        public ComputedValue<ReadOnlyMemory<byte>> Signature { get; } = new();
        public ComputedJsonObject OriginalOptionsJson { get; } = new();
        public ComputedJsonObject ClientDataJson { get; } = new();
        public ComputedJsonObject CredentialJson { get; } = new();

        public void AddAllowCredentials(string userId)
        {
            _allowCredentials.Add(new()
            {
                Id = BufferSource.FromString(userId),
                Type = "public-key",
                Transports = ["internal"],
            });
        }

        protected override async Task<PasskeyAssertionResult<PocoUser>> RunTestAsync()
        {
            var identityOptions = Options.Create(IdentityOptions);
            var handler = new DefaultPasskeyHandler<PocoUser>(identityOptions);
            var credential = TestCredentialKeyPair.Generate(Algorithm);
            var allowCredentialsJson = JsonSerializer.Serialize(
                _allowCredentials,
                IdentityJsonSerializerContext.Default.IReadOnlyListPublicKeyCredentialDescriptor);
            var originalOptionsJson = OriginalOptionsJson.Compute($$"""
                {
                  "challenge": {{ToBase64UrlJsonValue(Challenge)}},
                  "rpId": {{ToJsonValue(RpId)}},
                  "allowCredentials": {{allowCredentialsJson}},
                  "timeout": 60000,
                  "userVerification": "preferred",
                  "hints": []
                }
                """);
            var authenticatorDataArgs = AuthenticatorDataArgs.Compute(new()
            {
                RpIdHash = SHA256.HashData(Encoding.UTF8.GetBytes(RpId ?? string.Empty)),
                Flags = AuthenticatorDataFlags.UserPresent,
            });
            var authenticatorData = AuthenticatorData.Compute(MakeAuthenticatorData(authenticatorDataArgs));
            var clientDataJson = ClientDataJson.Compute($$"""
                {
                  "challenge": {{ToBase64UrlJsonValue(Challenge)}},
                  "origin": {{ToJsonValue(Origin)}},
                  "type": "webauthn.get"
                }
                """);
            var clientDataJsonBytes = Encoding.UTF8.GetBytes(clientDataJson?.ToString() ?? string.Empty);
            var clientDataHash = SHA256.HashData(clientDataJsonBytes);
            var dataToSign = (byte[])[.. authenticatorData.Span, .. clientDataHash];
            var signature = Signature.Compute(credential.SignData(dataToSign));
            var credentialJson = CredentialJson.Compute($$"""
                {
                  "id": {{ToBase64UrlJsonValue(CredentialId)}},
                  "response": {
                    "authenticatorData": {{ToBase64UrlJsonValue(authenticatorData)}},
                    "clientDataJSON": {{ToBase64UrlJsonValue(clientDataJson)}},
                    "signature": {{ToBase64UrlJsonValue(signature)}},
                    "userHandle": {{ToBase64UrlJsonValue(User.Id)}}
                  },
                  "type": "public-key",
                  "clientExtensionResults": {},
                  "authenticatorAttachment": "platform"
                }
                """);

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Request.Headers.Origin).Returns(new StringValues(Origin));

            var userManager = MockHelpers.MockUserManager<PocoUser>();
            userManager
                .Setup(m => m.FindByIdAsync(User.Id))
                .Returns(Task.FromResult<PocoUser?>(User));
            userManager
                .Setup(m => m.GetPasskeyAsync(It.IsAny<PocoUser>(), It.IsAny<byte[]>()))
                .Returns((PocoUser user, byte[] credentialId) =>
                {
                    if (user != User || !CredentialId.Span.SequenceEqual(credentialId))
                    {
                        return Task.FromResult<UserPasskeyInfo?>(null);
                    }

                    var credentialPublicKey = credential.EncodePublicKeyCbor();

                    // Some properties don't affect validation, so we can
                    // use default values.
                    return Task.FromResult<UserPasskeyInfo?>(new(
                        CredentialId.ToArray(),
                        credentialPublicKey.ToArray(),
                        name: null,
                        createdAt: default,
                        signCount: 0, // TODO: Make configurable
                        transports: null,
                        isUserVerified: true, // TODO: Make configurable
                        isBackupEligible: false, // TODO: Make configurable
                        isBackedUp: false,
                        attestationObject: [],
                        clientDataJson: []
                    ));
                });

            if (IsUserIdentified)
            {
                userManager
                    .Setup(m => m.GetUserIdAsync(User))
                    .Returns(Task.FromResult(User.Id));
            }

            var context = new PasskeyAssertionContext<PocoUser>
            {
                CredentialJson = credentialJson,
                OriginalOptionsJson = originalOptionsJson,
                HttpContext = httpContext.Object,
                UserManager = userManager.Object,
                User = IsUserIdentified ? User : null,
            };

            return await handler.PerformAssertionAsync(context);
        }
    }

    private static string ToJsonValue(string? value)
        => value is null ? "null" : $"\"{value}\"";

    private static string ToBase64UrlJsonValue(ReadOnlyMemory<byte>? bytes)
        => !bytes.HasValue ? "null" : $"\"{Base64Url.EncodeToString(bytes.Value.Span)}\"";

    private static string ToBase64UrlJsonValue(string? value)
        => value is null ? "null" : $"\"{Base64Url.EncodeToString(Encoding.UTF8.GetBytes(value))}\"";

    private readonly struct AttestedCredentialDataArgs()
    {
        private static readonly ReadOnlyMemory<byte> _defaultAaguid = new byte[16];

        public ReadOnlyMemory<byte> Aaguid { get; init; } = _defaultAaguid;
        public required ReadOnlyMemory<byte> CredentialId { get; init; }
        public required ReadOnlyMemory<byte> CredentialPublicKey { get; init; }
    }

    private static ReadOnlyMemory<byte> MakeAttestedCredentialData(in AttestedCredentialDataArgs args)
    {
        const int AaguidLength = 16;
        const int CredentialIdLengthLength = 2;
        var length = AaguidLength + CredentialIdLengthLength + args.CredentialId.Length + args.CredentialPublicKey.Length;
        var result = new byte[length];
        var offset = 0;

        args.Aaguid.Span.CopyTo(result.AsSpan(offset, AaguidLength));
        offset += AaguidLength;

        BinaryPrimitives.WriteUInt16BigEndian(result.AsSpan(offset, CredentialIdLengthLength), (ushort)args.CredentialId.Length);
        offset += CredentialIdLengthLength;

        args.CredentialId.Span.CopyTo(result.AsSpan(offset));
        offset += args.CredentialId.Length;

        args.CredentialPublicKey.Span.CopyTo(result.AsSpan(offset));
        offset += args.CredentialPublicKey.Length;

        if (offset != result.Length)
        {
            throw new InvalidOperationException($"Expected attested credential data length '{length}', but got '{offset}'.");
        }

        return result;
    }

    private readonly struct AuthenticatorDataArgs()
    {
        public required AuthenticatorDataFlags Flags { get; init; }
        public required ReadOnlyMemory<byte> RpIdHash { get; init; }
        public ReadOnlyMemory<byte>? AttestedCredentialData { get; init; }
        public ReadOnlyMemory<byte>? Extensions { get; init; }
        public uint SignCount { get; init; } = 1;
    }

    private static ReadOnlyMemory<byte> MakeAuthenticatorData(in AuthenticatorDataArgs args)
    {
        const int RpIdHashLength = 32;
        const int AuthenticatorDataFlagsLength = 1;
        const int SignCountLength = 4;
        var length =
            RpIdHashLength +
            AuthenticatorDataFlagsLength +
            SignCountLength +
            (args.AttestedCredentialData?.Length ?? 0) +
            (args.Extensions?.Length ?? 0);
        var result = new byte[length];
        var offset = 0;

        args.RpIdHash.Span.CopyTo(result.AsSpan(offset, RpIdHashLength));
        offset += RpIdHashLength;

        result[offset] = (byte)args.Flags;
        offset += AuthenticatorDataFlagsLength;

        BinaryPrimitives.WriteUInt32BigEndian(result.AsSpan(offset, SignCountLength), args.SignCount);
        offset += SignCountLength;

        if (args.AttestedCredentialData is { } attestedCredentialData)
        {
            attestedCredentialData.Span.CopyTo(result.AsSpan(offset));
            offset += attestedCredentialData.Length;
        }

        if (args.Extensions is { } extensions)
        {
            extensions.Span.CopyTo(result.AsSpan(offset));
            offset += extensions.Length;
        }

        if (offset != result.Length)
        {
            throw new InvalidOperationException($"Expected authenticator data length '{length}', but got '{offset}'.");
        }

        return result;
    }

    private readonly struct AttestationObjectArgs()
    {
        private static readonly byte[] _defaultAttestationStatement = [0xA0]; // Empty CBOR map

        public int? CborMapLength { get; init; } = 3;
        public string? Format { get; init; } = "none";
        public ReadOnlyMemory<byte>? AttestationStatement { get; init; } = _defaultAttestationStatement;
        public required ReadOnlyMemory<byte>? AuthenticatorData { get; init; }
    }

    private static ReadOnlyMemory<byte> MakeAttestationObject(in AttestationObjectArgs args)
    {
        var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
        writer.WriteStartMap(args.CborMapLength);
        if (args.Format is { } format)
        {
            writer.WriteTextString("fmt");
            writer.WriteTextString(format);
        }
        if (args.AttestationStatement is { } attestationStatement)
        {
            writer.WriteTextString("attStmt");
            writer.WriteEncodedValue(attestationStatement.Span);
        }
        if (args.AuthenticatorData is { } authenticatorData)
        {
            writer.WriteTextString("authData");
            writer.WriteByteString(authenticatorData.Span);
        }
        writer.WriteEndMap();
        return writer.Encode();
    }

    private sealed class TestCredentialKeyPair
    {
        private readonly RSA? _rsa;
        private readonly ECDsa? _ecdsa;
        private readonly COSEAlgorithmIdentifier _alg;
        private readonly COSEKeyType _keyType;
        private readonly COSEEllipticCurve _curve;

        private TestCredentialKeyPair(RSA rsa, COSEAlgorithmIdentifier alg)
        {
            _rsa = rsa;
            _alg = alg;
            _keyType = COSEKeyType.RSA;
        }

        private TestCredentialKeyPair(ECDsa ecdsa, COSEAlgorithmIdentifier alg, COSEEllipticCurve curve)
        {
            _ecdsa = ecdsa;
            _alg = alg;
            _keyType = COSEKeyType.EC2;
            _curve = curve;
        }

        public static TestCredentialKeyPair Generate(COSEAlgorithmIdentifier alg)
        {
            return alg switch
            {
                COSEAlgorithmIdentifier.RS1 or
                COSEAlgorithmIdentifier.RS256 or
                COSEAlgorithmIdentifier.RS384 or
                COSEAlgorithmIdentifier.RS512 or
                COSEAlgorithmIdentifier.PS256 or
                COSEAlgorithmIdentifier.PS384 or
                COSEAlgorithmIdentifier.PS512 => GenerateRsaKeyPair(alg),

                COSEAlgorithmIdentifier.ES256 => GenerateEcKeyPair(alg, ECCurve.NamedCurves.nistP256, COSEEllipticCurve.P256),
                COSEAlgorithmIdentifier.ES384 => GenerateEcKeyPair(alg, ECCurve.NamedCurves.nistP384, COSEEllipticCurve.P384),
                COSEAlgorithmIdentifier.ES512 => GenerateEcKeyPair(alg, ECCurve.NamedCurves.nistP521, COSEEllipticCurve.P521),
                COSEAlgorithmIdentifier.ES256K => GenerateEcKeyPair(alg, ECCurve.CreateFromFriendlyName("secP256k1"), COSEEllipticCurve.P256K),

                _ => throw new NotSupportedException($"Algorithm {alg} is not supported for key pair generation")
            };
        }

        public ReadOnlyMemory<byte> SignData(ReadOnlySpan<byte> data)
        {
            return _keyType switch
            {
                COSEKeyType.RSA => SignRsaData(data),
                COSEKeyType.EC2 => SignEcData(data),
                _ => throw new InvalidOperationException($"Unsupported key type {_keyType}")
            };
        }

        private byte[] SignRsaData(ReadOnlySpan<byte> data)
        {
            if (_rsa is null)
            {
                throw new InvalidOperationException("RSA key is not available for signing");
            }

            var hashAlgorithm = GetHashAlgorithmFromCoseAlg(_alg);
            var padding = GetRsaPaddingFromCoseAlg(_alg);

            return _rsa.SignData(data.ToArray(), hashAlgorithm, padding);
        }

        private byte[] SignEcData(ReadOnlySpan<byte> data)
        {
            if (_ecdsa is null)
            {
                throw new InvalidOperationException("ECDSA key is not available for signing");
            }

            var hashAlgorithm = GetHashAlgorithmFromCoseAlg(_alg);

            // Note: WebAuthn expects signature in RFC3279 DER sequence format
            return _ecdsa.SignData(data.ToArray(), hashAlgorithm, DSASignatureFormat.Rfc3279DerSequence);
        }

        private static TestCredentialKeyPair GenerateRsaKeyPair(COSEAlgorithmIdentifier alg)
        {
            const int KeySize = 2048;
            var rsa = RSA.Create(KeySize);
            return new TestCredentialKeyPair(rsa, alg);
        }

        private static TestCredentialKeyPair GenerateEcKeyPair(COSEAlgorithmIdentifier alg, ECCurve curve, COSEEllipticCurve coseCurve)
        {
            var ecdsa = ECDsa.Create(curve);
            return new TestCredentialKeyPair(ecdsa, alg, coseCurve);
        }

        public ReadOnlyMemory<byte> EncodePublicKeyCbor()
            => _keyType switch
            {
                COSEKeyType.RSA => EncodeCoseRsaPublicKey(_rsa!, _alg),
                COSEKeyType.EC2 => EncodeCoseEcPublicKey(_ecdsa!, _alg, _curve),
                _ => throw new InvalidOperationException($"Unsupported key type {_keyType}")
            };

        private static byte[] EncodeCoseRsaPublicKey(RSA rsa, COSEAlgorithmIdentifier alg)
        {
            var parameters = rsa.ExportParameters(false);

            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(4); // kty, alg, n, e

            writer.WriteInt32((int)COSEKeyParameter.KeyType);
            writer.WriteInt32((int)COSEKeyType.RSA);

            writer.WriteInt32((int)COSEKeyParameter.Alg);
            writer.WriteInt32((int)alg);

            writer.WriteInt32((int)COSEKeyParameter.N);
            writer.WriteByteString(parameters.Modulus!);

            writer.WriteInt32((int)COSEKeyParameter.E);
            writer.WriteByteString(parameters.Exponent!);

            writer.WriteEndMap();
            return writer.Encode();
        }

        private static byte[] EncodeCoseEcPublicKey(ECDsa ecdsa, COSEAlgorithmIdentifier alg, COSEEllipticCurve curve)
        {
            var parameters = ecdsa.ExportParameters(false);

            var writer = new CborWriter(CborConformanceMode.Ctap2Canonical);
            writer.WriteStartMap(5); // kty, alg, crv, x, y

            writer.WriteInt32((int)COSEKeyParameter.KeyType);
            writer.WriteInt32((int)COSEKeyType.EC2);

            writer.WriteInt32((int)COSEKeyParameter.Alg);
            writer.WriteInt32((int)alg);

            writer.WriteInt32((int)COSEKeyParameter.Crv);
            writer.WriteInt32((int)curve);

            writer.WriteInt32((int)COSEKeyParameter.X);
            writer.WriteByteString(parameters.Q.X!);

            writer.WriteInt32((int)COSEKeyParameter.Y);
            writer.WriteByteString(parameters.Q.Y!);

            writer.WriteEndMap();
            return writer.Encode();
        }

        private static HashAlgorithmName GetHashAlgorithmFromCoseAlg(COSEAlgorithmIdentifier alg)
        {
            return alg switch
            {
                COSEAlgorithmIdentifier.RS1 => HashAlgorithmName.SHA1,
                COSEAlgorithmIdentifier.ES256 => HashAlgorithmName.SHA256,
                COSEAlgorithmIdentifier.ES384 => HashAlgorithmName.SHA384,
                COSEAlgorithmIdentifier.ES512 => HashAlgorithmName.SHA512,
                COSEAlgorithmIdentifier.PS256 => HashAlgorithmName.SHA256,
                COSEAlgorithmIdentifier.PS384 => HashAlgorithmName.SHA384,
                COSEAlgorithmIdentifier.PS512 => HashAlgorithmName.SHA512,
                COSEAlgorithmIdentifier.RS256 => HashAlgorithmName.SHA256,
                COSEAlgorithmIdentifier.RS384 => HashAlgorithmName.SHA384,
                COSEAlgorithmIdentifier.RS512 => HashAlgorithmName.SHA512,
                COSEAlgorithmIdentifier.ES256K => HashAlgorithmName.SHA256,
                _ => throw new InvalidOperationException($"Unsupported algorithm: {alg}")
            };
        }

        private static RSASignaturePadding GetRsaPaddingFromCoseAlg(COSEAlgorithmIdentifier alg)
        {
            return alg switch
            {
                COSEAlgorithmIdentifier.PS256 or
                COSEAlgorithmIdentifier.PS384 or
                COSEAlgorithmIdentifier.PS512 => RSASignaturePadding.Pss,

                COSEAlgorithmIdentifier.RS1 or
                COSEAlgorithmIdentifier.RS256 or
                COSEAlgorithmIdentifier.RS384 or
                COSEAlgorithmIdentifier.RS512 => RSASignaturePadding.Pkcs1,

                _ => throw new InvalidOperationException($"Unsupported RSA algorithm: {alg}")
            };
        }

        private enum COSEKeyType
        {
            OKP = 1,
            EC2 = 2,
            RSA = 3,
            Symmetric = 4
        }

        private enum COSEKeyParameter
        {
            Crv = -1,
            K = -1,
            X = -2,
            Y = -3,
            D = -4,
            N = -1,
            E = -2,
            KeyType = 1,
            KeyId = 2,
            Alg = 3,
            KeyOps = 4,
            BaseIV = 5
        }

        private enum COSEEllipticCurve
        {
            Reserved = 0,
            P256 = 1,
            P384 = 2,
            P521 = 3,
            X25519 = 4,
            X448 = 5,
            Ed25519 = 6,
            Ed448 = 7,
            P256K = 8,
        }
    }

    private abstract class ConfigurableTestBase<TResult>
    {
        private bool _hasStarted;

        public Task<TResult> RunAsync()
        {
            if (_hasStarted)
            {
                throw new InvalidOperationException("The test can only be run once.");
            }

            _hasStarted = true;
            return RunTestAsync();
        }

        protected abstract Task<TResult> RunTestAsync();
    }

    private class ComputedValue<TValue>
    {
        private bool _isComputed;
        private TValue? _computedValue;
        private Func<TValue, TValue?>? _transformFunc;

        public TValue GetValue()
        {
            if (!_isComputed)
            {
                throw new InvalidOperationException("Cannot get the value because it has not yet been computed.");
            }

            return _computedValue!;
        }

        public virtual TValue Compute(TValue initialValue)
        {
            if (_isComputed)
            {
                throw new InvalidOperationException("Cannot compute a value multiple times.");
            }

            if (_transformFunc is not null)
            {
                initialValue = _transformFunc(initialValue) ?? initialValue;
            }

            _isComputed = true;
            _computedValue = initialValue;
            return _computedValue;
        }

        public virtual void Transform(Func<TValue, TValue?> transform)
        {
            if (_transformFunc is not null)
            {
                throw new InvalidOperationException("Cannot transform a value multiple times.");
            }

            _transformFunc = transform;
        }
    }

    private sealed class ComputedJsonObject : ComputedValue<string>
    {
        private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
        {
            WriteIndented = true,
        };

        private JsonElement? _jsonElementValue;

        public JsonElement GetValueAsJsonElement()
        {
            if (_jsonElementValue is null)
            {
                var rawValue = GetValue() ?? throw new InvalidOperationException("Cannot get the value as a JSON element because it is null.");
                try
                {
                    _jsonElementValue = JsonSerializer.Deserialize<JsonElement>(rawValue, _jsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("Cannot get the value as a JSON element because it is not valid JSON.", ex);
                }
            }

            return _jsonElementValue.Value;
        }

        public void TransformAsJsonObject(Action<JsonObject> transform)
        {
            Transform(value =>
            {
                try
                {
                    var jsonObject = JsonNode.Parse(value)?.AsObject()
                        ?? throw new InvalidOperationException("Could not transform the JSON value because it was unexpectedly null.");
                    transform(jsonObject);
                    return jsonObject.ToJsonString(_jsonSerializerOptions);
                }
                catch (JsonException ex)
                {
                    throw new InvalidOperationException("Could not transform the value because it was not valid JSON.", ex);
                }
            });
        }
    }
}
