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

    [Fact]
    public async Task Assertion_Succeeds_WhenUserVerificationIsRequiredAndUserIsVerified()
    {
        var test = new AssertionTest();
        test.OriginalOptionsJson.TransformAsJsonObject(optionsJson =>
        {
            optionsJson["userVerification"] = "required";
        });
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.UserVerified,
        });

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Assertion_Succeeds_WhenUserVerificationIsDiscouragedAndUserIsVerified()
    {
        var test = new AssertionTest();
        test.OriginalOptionsJson.TransformAsJsonObject(optionsJson =>
        {
            optionsJson["userVerification"] = "discouraged";
        });
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.UserVerified,
        });

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Assertion_Fails_WhenUserVerificationIsRequiredAndUserIsNotVerified()
    {
        var test = new AssertionTest();
        test.OriginalOptionsJson.TransformAsJsonObject(optionsJson =>
        {
            optionsJson["userVerification"] = "required";
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "User verification is required, but the authenticator data flags did not have the 'UserVerified' flag",
            result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenUserIsNotPresent()
    {
        var test = new AssertionTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags & ~AuthenticatorDataFlags.UserPresent,
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator data flags did not include the 'UserPresent' flag", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Succeeds_WhenAuthenticatorDataContainsExtensionData()
    {
        var test = new AssertionTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.HasExtensionData,
            Extensions = (byte[])[0xA0] // Empty CBOR map.
        });

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataContainsExtraBytes()
    {
        var test = new AssertionTest();
        test.AuthenticatorData.Transform(authenticatorData =>
        {
            return (byte[])[.. authenticatorData.Span, 0xFF, 0xFF, 0xFF, 0xFF];
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator data had an invalid format", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataRpIdHashIsInvalid()
    {
        var test = new AssertionTest();
        test.AuthenticatorDataArgs.Transform(args =>
        {
            var newRpIdHash = args.RpIdHash.ToArray();
            newRpIdHash[0]++;
            return args with { RpIdHash = newRpIdHash };
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The authenticator data included an invalid Relying Party ID hash", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataClientDataHashIsInvalid()
    {
        var test = new AssertionTest();
        test.ClientDataHash.Transform(clientDataHash =>
        {
            var newClientDataHash = clientDataHash.ToArray();
            newClientDataHash[0]++;
            return newClientDataHash;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The assertion signature was invalid", result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Succeeds_WhenSignCountIsZero()
    {
        var test = new AssertionTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            SignCount = 0, // Normally 1
        });

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    // Having both sign counts be '0' is allowed, per the above test case,
    // so we don't test for its invalidity here.
    [Theory]
    [InlineData(42, 42)]
    [InlineData(41, 42)]
    [InlineData(0, 1)]
    public async Task Assertion_Fails_WhenAuthenticatorDataSignCountLessThanOrEqualToStoredSignCount(
        uint authenticatorDataSignCount,
        uint storedSignCount)
    {
        var test = new AssertionTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            SignCount = authenticatorDataSignCount,
        });
        test.StoredPasskey.Transform(passkey =>
        {
            passkey.SignCount = storedSignCount;
            return passkey;
        });

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "The authenticator's signature counter is unexpectedly less than or equal to the stored signature counter",
            result.Failure.Message);
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
    public async Task Assertion_Succeeds_WithSupportedAlgorithms(int algorithm)
    {
        var test = new AssertionTest
        {
            Algorithm = (COSEAlgorithmIdentifier)algorithm,
        };

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsNotBackupEligibleButBackedUp()
    {
        var test = new AssertionTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = (args.Flags | AuthenticatorDataFlags.BackedUp) & ~AuthenticatorDataFlags.BackupEligible,
        });

        // This test simulates an RP policy failure, not a mismatch between the stored passkey
        // and the authenticator data flags, so we'll make the stored passkey match the
        // authenticator data flags
        test.IsStoredPasskeyBackedUp = true;
        test.IsStoredPasskeyBackupEligible = false;

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith("The credential is backed up, but the authenticator data flags did not have the 'BackupEligible' flag", result.Failure.Message);
    }

    [Theory]
    [InlineData(PasskeyOptions.CredentialBackupPolicy.Allowed)]
    [InlineData(PasskeyOptions.CredentialBackupPolicy.Required)]
    public async Task Assertion_Succeeds_WhenAuthenticatorDataIsBackupEligible(PasskeyOptions.CredentialBackupPolicy backupEligibility)
    {
        var test = new AssertionTest();
        test.IdentityOptions.Passkey.BackupEligibleCredentialPolicy = backupEligibility;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible,
        });

        // This test simulates an RP policy failure, not a mismatch between the stored passkey
        // and the authenticator data flags, so we'll make the stored passkey match the
        // authenticator data flags
        test.IsStoredPasskeyBackupEligible = true;

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsBackupEligibleButDisallowed()
    {
        var test = new AssertionTest();
        test.IdentityOptions.Passkey.BackupEligibleCredentialPolicy = PasskeyOptions.CredentialBackupPolicy.Disallowed;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible,
        });

        // This test simulates an RP policy failure, not a mismatch between the stored passkey
        // and the authenticator data flags, so we'll make the stored passkey match the
        // authenticator data flags
        test.IsStoredPasskeyBackupEligible = true;

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Credential backup eligibility is disallowed, but the credential was eligible for backup",
            result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsNotBackupEligibleButRequired()
    {
        var test = new AssertionTest();
        test.IdentityOptions.Passkey.BackupEligibleCredentialPolicy = PasskeyOptions.CredentialBackupPolicy.Required;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags & ~AuthenticatorDataFlags.BackupEligible,
        });

        // This test simulates an RP policy failure, not a mismatch between the stored passkey
        // and the authenticator data flags, so we'll make the stored passkey match the
        // authenticator data flags
        test.IsStoredPasskeyBackupEligible = false;

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Credential backup eligibility is required, but the credential was not eligible for backup",
            result.Failure.Message);
    }

    [Theory]
    [InlineData(PasskeyOptions.CredentialBackupPolicy.Allowed)]
    [InlineData(PasskeyOptions.CredentialBackupPolicy.Required)]
    public async Task Attestation_Fails_WhenAuthenticatorDataIsBackedUp(PasskeyOptions.CredentialBackupPolicy backedUpPolicy)
    {
        var test = new AssertionTest();
        test.IdentityOptions.Passkey.BackedUpCredentialPolicy = backedUpPolicy;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible | AuthenticatorDataFlags.BackedUp,
        });

        // This test simulates an RP policy failure, not a mismatch between the stored passkey
        // and the authenticator data flags, so we'll make the stored passkey match the
        // authenticator data flags
        test.IsStoredPasskeyBackupEligible = true;
        test.IsStoredPasskeyBackedUp = true;

        var result = await test.RunAsync();

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsBackedUpButDisallowed()
    {
        var test = new AssertionTest();
        test.IdentityOptions.Passkey.BackedUpCredentialPolicy = PasskeyOptions.CredentialBackupPolicy.Disallowed;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible | AuthenticatorDataFlags.BackedUp,
        });

        // This test simulates an RP policy failure, not a mismatch between the stored passkey
        // and the authenticator data flags, so we'll make the stored passkey match the
        // authenticator data flags
        test.IsStoredPasskeyBackupEligible = true;
        test.IsStoredPasskeyBackedUp = true;

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Credential backup is disallowed, but the credential was backed up",
            result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsNotBackedUpButRequired()
    {
        var test = new AssertionTest();
        test.IdentityOptions.Passkey.BackedUpCredentialPolicy = PasskeyOptions.CredentialBackupPolicy.Required;
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags & ~AuthenticatorDataFlags.BackedUp,
        });

        // This test simulates an RP policy failure, not a mismatch between the stored passkey
        // and the authenticator data flags, so we'll make the stored passkey match the
        // authenticator data flags
        test.IsStoredPasskeyBackedUp = false;

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "Credential backup is required, but the credential was not backed up",
            result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsNotBackupEligibleButStoredPasskeyIs()
    {
        var test = new AssertionTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags & ~AuthenticatorDataFlags.BackupEligible,
        });
        test.IsStoredPasskeyBackupEligible = true;

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "The stored credential is eligible for backup, but the provided credential was unexpectedly ineligible for backup.",
            result.Failure.Message);
    }

    [Fact]
    public async Task Assertion_Fails_WhenAuthenticatorDataIsBackupEligibleButStoredPasskeyIsNot()
    {
        var test = new AssertionTest();
        test.AuthenticatorDataArgs.Transform(args => args with
        {
            Flags = args.Flags | AuthenticatorDataFlags.BackupEligible,
        });
        test.IsStoredPasskeyBackupEligible = false;

        var result = await test.RunAsync();

        Assert.False(result.Succeeded);
        Assert.StartsWith(
            "The stored credential is ineligible for backup, but the provided credential was unexpectedly eligible for backup",
            result.Failure.Message);
    }

    private sealed class AssertionTest : PasskeyTestBase<PasskeyAssertionResult<PocoUser>>
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
        public bool IsStoredPasskeyBackupEligible { get; set; }
        public bool IsStoredPasskeyBackedUp { get; set; }
        public COSEAlgorithmIdentifier Algorithm { get; set; } = COSEAlgorithmIdentifier.ES256;
        public ReadOnlyMemory<byte> Challenge { get; set; } = _defaultChallenge;
        public ReadOnlyMemory<byte> CredentialId { get; set; } = _defaultCredentialId;
        public ComputedValue<AuthenticatorDataArgs> AuthenticatorDataArgs { get; } = new();
        public ComputedValue<ReadOnlyMemory<byte>> AuthenticatorData { get; } = new();
        public ComputedValue<ReadOnlyMemory<byte>> ClientDataHash { get; } = new();
        public ComputedValue<ReadOnlyMemory<byte>> Signature { get; } = new();
        public ComputedJsonObject OriginalOptionsJson { get; } = new();
        public ComputedJsonObject ClientDataJson { get; } = new();
        public ComputedJsonObject CredentialJson { get; } = new();
        public ComputedValue<UserPasskeyInfo> StoredPasskey { get; } = new();

        public void AddAllowCredentials(string userId)
        {
            _allowCredentials.Add(new()
            {
                Id = BufferSource.FromString(userId),
                Type = "public-key",
                Transports = ["internal"],
            });
        }

        protected override async Task<PasskeyAssertionResult<PocoUser>> RunCoreAsync()
        {
            var identityOptions = Options.Create(IdentityOptions);
            var handler = new DefaultPasskeyHandler<PocoUser>(identityOptions);
            var credential = CredentialKeyPair.Generate(Algorithm);
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
            var clientDataHash = ClientDataHash.Compute(SHA256.HashData(clientDataJsonBytes));
            var dataToSign = (byte[])[.. authenticatorData.Span, .. clientDataHash.Span];
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

            var credentialPublicKey = credential.EncodePublicKeyCbor();
            var storedPasskey = StoredPasskey.Compute(new(
                CredentialId.ToArray(),
                credentialPublicKey.ToArray(),
                name: null,
                createdAt: default,
                signCount: 0,
                transports: null,
                isUserVerified: true,
                isBackupEligible: IsStoredPasskeyBackupEligible,
                isBackedUp: IsStoredPasskeyBackedUp,
                attestationObject: [],
                clientDataJson: []));

            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.Request.Headers.Origin).Returns(new StringValues(Origin));

            var userManager = MockHelpers.MockUserManager<PocoUser>();
            userManager
                .Setup(m => m.FindByIdAsync(User.Id))
                .Returns(Task.FromResult<PocoUser?>(User));
            userManager
                .Setup(m => m.GetPasskeyAsync(It.IsAny<PocoUser>(), It.IsAny<byte[]>()))
                .Returns((PocoUser user, byte[] credentialId) => Task.FromResult(
                    user == User && CredentialId.Span.SequenceEqual(credentialId)
                        ? storedPasskey
                        : null));

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
}
