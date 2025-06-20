// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
