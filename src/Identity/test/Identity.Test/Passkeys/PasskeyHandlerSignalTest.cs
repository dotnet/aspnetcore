// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Buffers.Text;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace Microsoft.AspNetCore.Identity.Test;

public class PasskeyHandlerSignalTest
{
    [Fact]
    public async Task CanMakeKnownPasskeysSignalOptions()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManager(user, CreatePasskey([1, 2, 3]), CreatePasskey([4, 5, 6]));
        var handler = CreateHandler(userManager);
        var httpContext = CreateHttpContext("contoso.com", port: 5001);

        var result = await handler.MakeKnownPasskeysSignalOptionsAsync(user, CreateUserEntity(user, "Foo", "Foo Bar"), httpContext);

        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("contoso.com", options.GetProperty("rpId").GetString());
        Assert.Equal(Base64Url.EncodeToString(Encoding.UTF8.GetBytes(user.Id)), options.GetProperty("userId").GetString());
        Assert.Equal("Foo", options.GetProperty("name").GetString());
        Assert.Equal("Foo Bar", options.GetProperty("displayName").GetString());
        Assert.Collection(options.GetProperty("allAcceptedCredentialIds").EnumerateArray(),
            id => Assert.Equal(Base64Url.EncodeToString([1, 2, 3]), id.GetString()),
            id => Assert.Equal(Base64Url.EncodeToString([4, 5, 6]), id.GetString()));
    }

    [Fact]
    public void SupportsKnownPasskeysSignalOptionsIsTrue()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        Assert.True(handler.SupportsKnownPasskeysSignalOptions);
    }

    [Fact]
    public void SupportsKnownPasskeysSignalOptionsIsFalseWhenStoreDoesNotSupportPasskeys()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = MockHelpers.MockUserManager<PocoUser>();
        userManager.Setup(m => m.SupportsUserPasskey).Returns(false);
        var handler = CreateHandler(userManager.Object);

        Assert.False(handler.SupportsKnownPasskeysSignalOptions);
    }

    [Fact]
    public async Task MakeKnownPasskeysSignalOptionsUsesConfiguredServerDomain()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManager(user);
        var handler = CreateHandler(userManager, new() { ServerDomain = "fabrikam.com" });
        var httpContext = CreateHttpContext("contoso.com");

        var result = await handler.MakeKnownPasskeysSignalOptionsAsync(user, CreateUserEntity(user), httpContext);

        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("fabrikam.com", options.GetProperty("rpId").GetString());
    }

    [Fact]
    public async Task MakeKnownPasskeysSignalOptionsWithoutPasskeysReturnsEmptyCredentialList()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeKnownPasskeysSignalOptionsAsync(user, CreateUserEntity(user), CreateHttpContext());

        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Empty(options.GetProperty("allAcceptedCredentialIds").EnumerateArray());
    }

    [Fact]
    public async Task MakeKnownPasskeysSignalOptionsThrowsWhenUserEntityIdDoesNotMatchUser()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));
        var userEntity = new PasskeyUserEntity
        {
            Id = "some-other-id",
            Name = "Foo",
            DisplayName = "Foo",
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.MakeKnownPasskeysSignalOptionsAsync(user, userEntity, CreateHttpContext()));

        Assert.Equal($"The user entity ID 'some-other-id' does not match the ID '{user.Id}' of the specified user.", ex.Message);
    }

    [Fact]
    public async Task CanMakeUnknownPasskeySignalOptions()
    {
        var user = new PocoUser { UserName = "Foo" };
        var credentialId = (byte[])[1, 2, 3];
        var userManager = SetupUserManager(user);
        var handler = CreateHandler(userManager);

        var result = await handler.MakeUnknownPasskeySignalOptionsAsync(
            CreateAssertionCredentialJson(credentialId),
            CreateHttpContext());

        Assert.NotNull(result);
        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("contoso.com", options.GetProperty("rpId").GetString());
        Assert.Equal(Base64Url.EncodeToString(credentialId), options.GetProperty("credentialId").GetString());
        Mock.Get(userManager).Verify(
            m => m.FindByPasskeyIdAsync(It.Is<byte[]>(id => id.SequenceEqual(credentialId))),
            Times.Once);
    }

    [Fact]
    public async Task MakeUnknownPasskeySignalOptionsReturnsNullWhenCredentialBelongsToAUser()
    {
        var user = new PocoUser { UserName = "Foo" };
        var credentialId = (byte[])[1, 2, 3];
        var userManager = SetupUserManagerMock(user);
        userManager
            .Setup(m => m.FindByPasskeyIdAsync(It.Is<byte[]>(id => id.SequenceEqual(credentialId))))
            .ReturnsAsync(user);
        var handler = CreateHandler(userManager.Object);

        var result = await handler.MakeUnknownPasskeySignalOptionsAsync(
            CreateAssertionCredentialJson(credentialId),
            CreateHttpContext());

        Assert.Null(result);
    }

    [Fact]
    public async Task MakeUnknownPasskeySignalOptionsReturnsNullForMalformedJson()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeUnknownPasskeySignalOptionsAsync("{", CreateHttpContext());

        Assert.Null(result);
    }

    [Fact]
    public async Task MakeUnknownPasskeySignalOptionsReturnsNullWhenStoreDoesNotSupportPasskeys()
    {
        var handler = CreateHandler(MockHelpers.TestUserManager<PocoUser>());

        var result = await handler.MakeUnknownPasskeySignalOptionsAsync(
            CreateAssertionCredentialJson([1, 2, 3]),
            CreateHttpContext());

        Assert.Null(result);
    }

    [Fact]
    public async Task CanMakeUnknownPasskeySignalOptionsFromAttestationCredential()
    {
        var user = new PocoUser { UserName = "Foo" };
        var credentialId = (byte[])[1, 2, 3];
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeUnknownPasskeySignalOptionsAsync(
            CreateAttestationCredentialJson(credentialId),
            CreateHttpContext());

        Assert.NotNull(result);
        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal(Base64Url.EncodeToString(credentialId), options.GetProperty("credentialId").GetString());
    }

    [Fact]
    public async Task MakeUnknownPasskeySignalOptionsUsesConfiguredServerDomain()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user), new() { ServerDomain = "fabrikam.com" });

        var result = await handler.MakeUnknownPasskeySignalOptionsAsync(
            CreateAssertionCredentialJson([1, 2, 3]),
            CreateHttpContext("contoso.com"));

        Assert.NotNull(result);
        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("fabrikam.com", options.GetProperty("rpId").GetString());
    }

    [Fact]
    public async Task MakeUnknownPasskeySignalOptionsUsesUnpaddedBase64UrlCredentialId()
    {
        var user = new PocoUser { UserName = "Foo" };
        var credentialId = (byte[])[251, 255];
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeUnknownPasskeySignalOptionsAsync(
            CreateAssertionCredentialJson(credentialId),
            CreateHttpContext());

        Assert.NotNull(result);
        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        var encodedCredentialId = options.GetProperty("credentialId").GetString();
        Assert.Equal("-_8", encodedCredentialId);
        Assert.NotNull(encodedCredentialId);
        Assert.DoesNotContain('=', encodedCredentialId);
    }

    private static PasskeyHandler<PocoUser> CreateHandler(UserManager<PocoUser> userManager, IdentityPasskeyOptions? options = null)
        => new(userManager, Options.Create(options ?? new IdentityPasskeyOptions()));

    private static UserManager<PocoUser> SetupUserManager(PocoUser user, params UserPasskeyInfo[] passkeys)
        => SetupUserManagerMock(user, passkeys).Object;

    private static Mock<UserManager<PocoUser>> SetupUserManagerMock(PocoUser user, params UserPasskeyInfo[] passkeys)
    {
        var manager = MockHelpers.MockUserManager<PocoUser>();
        manager.Setup(m => m.SupportsUserPasskey).Returns(true);
        manager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id);
        manager.Setup(m => m.GetPasskeysAsync(user)).ReturnsAsync(passkeys);
        manager.Setup(m => m.FindByPasskeyIdAsync(It.IsAny<byte[]>())).ReturnsAsync((PocoUser?)null);
        return manager;
    }

    private static HttpContext CreateHttpContext(string host = "contoso.com", int? port = null)
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = port is { } portValue ? new HostString(host, portValue) : new HostString(host);
        return httpContext;
    }

    private static PasskeyUserEntity CreateUserEntity(PocoUser user, string name = "Foo", string displayName = "Foo")
        => new()
        {
            Id = user.Id,
            Name = name,
            DisplayName = displayName,
        };

    private static UserPasskeyInfo CreatePasskey(byte[] credentialId)
        => new(credentialId, [], default, 0, null, false, false, false, [], []);

    private static string CreateAssertionCredentialJson(byte[] credentialId)
        => CreateCredentialJson(credentialId, new
        {
            clientDataJSON = "",
            authenticatorData = "",
            signature = "",
            userHandle = (string?)null,
        });

    private static string CreateAttestationCredentialJson(byte[] credentialId)
        => CreateCredentialJson(credentialId, new
        {
            clientDataJSON = "",
            attestationObject = "",
            transports = Array.Empty<string>(),
        });

    private static string CreateCredentialJson(byte[] credentialId, object response)
    {
        var encodedCredentialId = Base64Url.EncodeToString(credentialId);
        return JsonSerializer.Serialize(new
        {
            id = encodedCredentialId,
            rawId = encodedCredentialId,
            response,
            type = "public-key",
            clientExtensionResults = new { },
            authenticatorAttachment = "platform",
        });
    }
}
