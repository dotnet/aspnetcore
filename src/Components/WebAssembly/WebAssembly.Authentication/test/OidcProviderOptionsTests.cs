// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;

namespace Microsoft.AspNetCore.Components.WebAssembly.Authentication;

public class OidcProviderOptionsTests
{

    [Fact]
    public void TokenStorage_Default()
    {
        var options = new OidcProviderOptions();

        Assert.Equal(RemoteAuthenticationTokenStorage.SessionStorage, options.TokenStorage);
    }
    [Theory]
    [InlineData(RemoteAuthenticationTokenStorage.SessionStorage)]
    [InlineData(RemoteAuthenticationTokenStorage.LocalStorage)]
    public void TokenStorage_CanBeSet(RemoteAuthenticationTokenStorage storage)
    {
        var options = new OidcProviderOptions
        {
            TokenStorage = storage
        };

        Assert.Equal(storage, options.TokenStorage);
    }

    [Theory]
    [InlineData(RemoteAuthenticationTokenStorage.SessionStorage)]
    [InlineData(RemoteAuthenticationTokenStorage.LocalStorage)]
    public void TokenStorage_RoundTripsThroughJson(RemoteAuthenticationTokenStorage storage)
    {
        var original = new OidcProviderOptions
        {
            TokenStorage = storage
        };

        var deserialized = JsonSerializer.Deserialize<OidcProviderOptions>(JsonSerializer.Serialize(original));

        Assert.NotNull(deserialized);
        Assert.Equal(storage, deserialized.TokenStorage);
    }

    [Theory]
    [InlineData("{\"tokenStorage\":\"SessionStorage\"}", RemoteAuthenticationTokenStorage.SessionStorage)]
    [InlineData("{\"tokenStorage\":\"LocalStorage\"}", RemoteAuthenticationTokenStorage.LocalStorage)]
    public void TokenStorage_DeserializesStringValue(string json, RemoteAuthenticationTokenStorage expected)
    {
        var options = JsonSerializer.Deserialize<OidcProviderOptions>(json);

        Assert.NotNull(options);
        Assert.Equal(expected, options.TokenStorage);
    }

    [Theory]
    [InlineData("{\"tokenStorage\":\"sessionstorage\"}", RemoteAuthenticationTokenStorage.SessionStorage)]
    [InlineData("{\"tokenStorage\":\"localstorage\"}", RemoteAuthenticationTokenStorage.LocalStorage)]
    [InlineData("{\"tokenStorage\":\"SESSIONSTORAGE\"}", RemoteAuthenticationTokenStorage.SessionStorage)]
    public void TokenStorage_DeserializesCaseInsensitively(string json, RemoteAuthenticationTokenStorage expected)
    {
        var options = JsonSerializer.Deserialize<OidcProviderOptions>(json);

        Assert.NotNull(options);
        Assert.Equal(expected, options.TokenStorage);
    }

    [Fact]
    public void TokenStorage_UnknownValue_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<OidcProviderOptions>("{\"tokenStorage\":\"Cookie\"}"));
    }

    [Fact]
    public void TokenStorage_NullValue_Throws()
    {
        Assert.Throws<JsonException>(() =>
            JsonSerializer.Deserialize<OidcProviderOptions>("{\"tokenStorage\":null}"));
    }

    [Fact]
    public void TokenStorage_MissingKey_DefaultsToSessionStorage()
    {
        var options = JsonSerializer.Deserialize<OidcProviderOptions>("{}");

        Assert.NotNull(options);
        Assert.Equal(RemoteAuthenticationTokenStorage.SessionStorage, options.TokenStorage);
    }
}
