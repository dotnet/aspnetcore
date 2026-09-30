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
    public async Task CanMakeAllAcceptedCredentialsSignalOptions()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManager(user, CreatePasskey([1, 2, 3]), CreatePasskey([4, 5, 6]));
        var handler = CreateHandler(userManager);
        var httpContext = CreateHttpContext("contoso.com", port: 5001);

        var result = await handler.MakeAllAcceptedCredentialsSignalOptionsAsync(user, httpContext);

        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("contoso.com", options.GetProperty("rpId").GetString());
        Assert.Equal(Base64Url.EncodeToString(Encoding.UTF8.GetBytes(user.Id)), options.GetProperty("userId").GetString());
        Assert.Collection(options.GetProperty("allAcceptedCredentialIds").EnumerateArray(),
            id => Assert.Equal(Base64Url.EncodeToString([1, 2, 3]), id.GetString()),
            id => Assert.Equal(Base64Url.EncodeToString([4, 5, 6]), id.GetString()));
    }

    [Fact]
    public async Task MakeAllAcceptedCredentialsSignalOptionsOnlyIncludesSpecifiedMembers()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeAllAcceptedCredentialsSignalOptionsAsync(user, CreateHttpContext());

        AssertMemberNames(result.SignalOptionsJson, "rpId", "userId", "allAcceptedCredentialIds");
    }

    [Fact]
    public void SupportsPasskeySignalOptionsIsTrue()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        Assert.True(handler.SupportsPasskeySignalOptions);
    }

    [Fact]
    public void SupportsPasskeySignalOptionsIsFalseWhenStoreDoesNotSupportPasskeys()
    {
        var userManager = MockHelpers.MockUserManager<PocoUser>();
        userManager.Setup(m => m.SupportsUserPasskey).Returns(false);
        var handler = CreateHandler(userManager.Object);

        Assert.False(handler.SupportsPasskeySignalOptions);
    }

    [Fact]
    public async Task MakeAllAcceptedCredentialsSignalOptionsUsesConfiguredServerDomain()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManager(user);
        var handler = CreateHandler(userManager, new() { ServerDomain = "fabrikam.com" });
        var httpContext = CreateHttpContext("contoso.com");

        var result = await handler.MakeAllAcceptedCredentialsSignalOptionsAsync(user, httpContext);

        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("fabrikam.com", options.GetProperty("rpId").GetString());
    }

    [Fact]
    public async Task MakeAllAcceptedCredentialsSignalOptionsWithoutPasskeysReturnsEmptyCredentialList()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeAllAcceptedCredentialsSignalOptionsAsync(user, CreateHttpContext());

        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Empty(options.GetProperty("allAcceptedCredentialIds").EnumerateArray());
    }

    [Fact]
    public async Task CanMakeCurrentUserDetailsSignalOptions()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManager(user, CreatePasskey([1, 2, 3]));
        var handler = CreateHandler(userManager);
        var httpContext = CreateHttpContext("contoso.com", port: 5001);

        var result = await handler.MakeCurrentUserDetailsSignalOptionsAsync(user, CreateUserEntity(user, "Foo", "Foo Bar"), httpContext);

        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("contoso.com", options.GetProperty("rpId").GetString());
        Assert.Equal(Base64Url.EncodeToString(Encoding.UTF8.GetBytes(user.Id)), options.GetProperty("userId").GetString());
        Assert.Equal("Foo", options.GetProperty("name").GetString());
        Assert.Equal("Foo Bar", options.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task MakeCurrentUserDetailsSignalOptionsOnlyIncludesSpecifiedMembers()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user, CreatePasskey([1, 2, 3])));

        var result = await handler.MakeCurrentUserDetailsSignalOptionsAsync(user, CreateUserEntity(user), CreateHttpContext());

        AssertMemberNames(result.SignalOptionsJson, "rpId", "userId", "name", "displayName");
    }

    [Fact]
    public async Task MakeCurrentUserDetailsSignalOptionsDoesNotRetrievePasskeys()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManagerMock(user);
        var handler = CreateHandler(userManager.Object);

        await handler.MakeCurrentUserDetailsSignalOptionsAsync(user, CreateUserEntity(user), CreateHttpContext());

        userManager.Verify(m => m.GetPasskeysAsync(It.IsAny<PocoUser>()), Times.Never);
    }

    [Fact]
    public async Task MakeCurrentUserDetailsSignalOptionsUsesConfiguredServerDomain()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManager(user);
        var handler = CreateHandler(userManager, new() { ServerDomain = "fabrikam.com" });
        var httpContext = CreateHttpContext("contoso.com");

        var result = await handler.MakeCurrentUserDetailsSignalOptionsAsync(user, CreateUserEntity(user), httpContext);

        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("fabrikam.com", options.GetProperty("rpId").GetString());
    }

    [Fact]
    public async Task MakeCurrentUserDetailsSignalOptionsThrowsWhenUserEntityIdDoesNotMatchUser()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));
        var userEntity = new PasskeyUserEntity
        {
            Id = "some-other-id",
            Name = "Foo",
            DisplayName = "Foo",
        };

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => handler.MakeCurrentUserDetailsSignalOptionsAsync(user, userEntity, CreateHttpContext()));

        Assert.Equal("userEntity", ex.ParamName);
        Assert.StartsWith($"The user entity ID 'some-other-id' does not match the ID '{user.Id}' of the specified user.", ex.Message);
    }

    [Fact]
    public async Task CanMakeUnknownCredentialSignalOptions()
    {
        var user = new PocoUser { UserName = "Foo" };
        var credentialId = (byte[])[1, 2, 3];
        var userManager = SetupUserManager(user);
        var handler = CreateHandler(userManager);

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync(
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
    public async Task MakeUnknownCredentialSignalOptionsOnlyIncludesSpecifiedMembers()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync(
            CreateAssertionCredentialJson([1, 2, 3]),
            CreateHttpContext());

        Assert.NotNull(result);
        AssertMemberNames(result.SignalOptionsJson, "rpId", "credentialId");
    }

    [Fact]
    public async Task MakeUnknownCredentialSignalOptionsReturnsNullWhenCredentialBelongsToAUser()
    {
        var user = new PocoUser { UserName = "Foo" };
        var credentialId = (byte[])[1, 2, 3];
        var userManager = SetupUserManagerMock(user);
        userManager
            .Setup(m => m.FindByPasskeyIdAsync(It.Is<byte[]>(id => id.SequenceEqual(credentialId))))
            .ReturnsAsync(user);
        var handler = CreateHandler(userManager.Object);

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync(
            CreateAssertionCredentialJson(credentialId),
            CreateHttpContext());

        Assert.Null(result);
    }

    [Fact]
    public async Task MakeUnknownCredentialSignalOptionsReturnsNullForMalformedJson()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync("{", CreateHttpContext());

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task MakeUnknownCredentialSignalOptionsThrowsForNullOrEmptyCredentialJson(string? credentialJson)
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user));

        var exception = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => handler.MakeUnknownCredentialSignalOptionsAsync(credentialJson!, CreateHttpContext()));

        Assert.Equal("credentialJson", exception.ParamName);
    }

    [Fact]
    public async Task MakeUnknownCredentialSignalOptionsThrowsForNullHttpContextBeforeStoreLookup()
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManagerMock(user);
        var handler = CreateHandler(userManager.Object);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(
            () => handler.MakeUnknownCredentialSignalOptionsAsync(CreateAssertionCredentialJson([1, 2, 3]), null!));

        Assert.Equal("httpContext", exception.ParamName);
        userManager.Verify(m => m.FindByPasskeyIdAsync(It.IsAny<byte[]>()), Times.Never);
    }

    [Theory]
    [InlineData(1023, true)]
    [InlineData(1024, false)]
    public async Task MakeUnknownCredentialSignalOptionsEnforcesCredentialIdLengthLimit(int credentialIdLength, bool returnsOptions)
    {
        var user = new PocoUser { UserName = "Foo" };
        var userManager = SetupUserManagerMock(user);
        var handler = CreateHandler(userManager.Object);

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync(
            CreateAssertionCredentialJson(new byte[credentialIdLength]),
            CreateHttpContext());

        Assert.Equal(returnsOptions, result is not null);
        userManager.Verify(
            m => m.FindByPasskeyIdAsync(It.IsAny<byte[]>()),
            returnsOptions ? Times.Once : Times.Never);
    }

    [Fact]
    public async Task MakeUnknownCredentialSignalOptionsReturnsNullWhenStoreDoesNotSupportPasskeys()
    {
        var handler = CreateHandler(MockHelpers.TestUserManager<PocoUser>());

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync(
            CreateAssertionCredentialJson([1, 2, 3]),
            CreateHttpContext());

        Assert.Null(result);
    }

    [Fact]
    public async Task CanMakeUnknownCredentialSignalOptionsFromAttestationCredential()
    {
        var user = new PocoUser { UserName = "Foo" };
        var credentialId = (byte[])[1, 2, 3];
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync(
            CreateAttestationCredentialJson(credentialId),
            CreateHttpContext());

        Assert.NotNull(result);
        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal(Base64Url.EncodeToString(credentialId), options.GetProperty("credentialId").GetString());
    }

    [Fact]
    public async Task MakeUnknownCredentialSignalOptionsUsesConfiguredServerDomain()
    {
        var user = new PocoUser { UserName = "Foo" };
        var handler = CreateHandler(SetupUserManager(user), new() { ServerDomain = "fabrikam.com" });

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync(
            CreateAssertionCredentialJson([1, 2, 3]),
            CreateHttpContext("contoso.com"));

        Assert.NotNull(result);
        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        Assert.Equal("fabrikam.com", options.GetProperty("rpId").GetString());
    }

    [Fact]
    public async Task MakeUnknownCredentialSignalOptionsUsesUnpaddedBase64UrlCredentialId()
    {
        var user = new PocoUser { UserName = "Foo" };
        var credentialId = (byte[])[251, 255];
        var handler = CreateHandler(SetupUserManager(user));

        var result = await handler.MakeUnknownCredentialSignalOptionsAsync(
            CreateAssertionCredentialJson(credentialId),
            CreateHttpContext());

        Assert.NotNull(result);
        var options = JsonSerializer.Deserialize<JsonElement>(result.SignalOptionsJson);
        var encodedCredentialId = options.GetProperty("credentialId").GetString();
        Assert.Equal("-_8", encodedCredentialId);
        Assert.NotNull(encodedCredentialId);
        Assert.DoesNotContain('=', encodedCredentialId);
    }

    private static void AssertMemberNames(string optionsJson, params string[] expectedNames)
    {
        var options = JsonSerializer.Deserialize<JsonElement>(optionsJson);
        var actualNames = options.EnumerateObject().Select(p => p.Name);
        Assert.Equal(expectedNames, actualNames);
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
