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
    [InlineData("{\"tokenStorage\":0}", RemoteAuthenticationTokenStorage.SessionStorage)]
    [InlineData("{\"tokenStorage\":1}", RemoteAuthenticationTokenStorage.LocalStorage)]
    public void TokenStorage_DeserializesNumericValue(string json, RemoteAuthenticationTokenStorage expected)
    {
        var options = JsonSerializer.Deserialize<OidcProviderOptions>(json);

        Assert.NotNull(options);
        Assert.Equal(expected, options.TokenStorage);
    }
}
