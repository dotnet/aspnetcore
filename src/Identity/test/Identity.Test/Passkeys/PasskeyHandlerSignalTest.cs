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

    private static PasskeyHandler<PocoUser> CreateHandler(UserManager<PocoUser> userManager, IdentityPasskeyOptions? options = null)
        => new(userManager, Options.Create(options ?? new IdentityPasskeyOptions()));

    private static UserManager<PocoUser> SetupUserManager(PocoUser user, params UserPasskeyInfo[] passkeys)
    {
        var manager = MockHelpers.MockUserManager<PocoUser>();
        manager.Setup(m => m.SupportsUserPasskey).Returns(true);
        manager.Setup(m => m.GetUserIdAsync(user)).ReturnsAsync(user.Id);
        manager.Setup(m => m.GetPasskeysAsync(user)).ReturnsAsync(passkeys);
        return manager.Object;
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
}
