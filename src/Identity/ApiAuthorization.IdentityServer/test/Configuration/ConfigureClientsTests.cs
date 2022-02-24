// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Duende.IdentityServer;
using Duende.IdentityServer.Models;
using Microsoft.Extensions.Configuration;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;

public class ConfigureClientsTests
{
    [Fact]
    public void GetClients_DoesNothingIfThereAreNoConfiguredClients()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {

        }).Build();

        var resources = Array.Empty<ApiResource>();
        var clientLoader = new ConfigureClients(config, new TestLogger<ConfigureClients>());

        // Act
        var clients = clientLoader.GetClients();

        // Assert
        Assert.Empty(clients);
    }

    [Fact]
    public void GetClients_ReadsIdentityServerSPAFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["MyClient:Profile"] = "IdentityServerSPA"
        }).Build();

        var resources = Array.Empty<ApiResource>();
        var expectedScopes = new[]
        {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile
            };

        var clientLoader = new ConfigureClients(config, new TestLogger<ConfigureClients>());

        // Act
        var clients = clientLoader.GetClients();

        // Assert
        var client = Assert.Single(clients);
        Assert.Equal("MyClient", client.ClientId);
        Assert.Equal("MyClient", client.ClientName);
        Assert.True(client.AllowAccessTokensViaBrowser);
        Assert.Equal(new[] { "/authentication/login-callback" }, client.RedirectUris.ToArray());
        Assert.Equal(new[] { "/authentication/logout-callback" }, client.PostLogoutRedirectUris.ToArray());
        Assert.Empty(client.AllowedCorsOrigins);
        Assert.False(client.RequireConsent);
        Assert.Empty(client.ClientSecrets);
        Assert.Equal(GrantTypes.Code.ToArray(), client.AllowedGrantTypes.ToArray());
    }

    [Fact]
    public void GetClients_ReadsNativeAppFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["MyClient:Profile"] = "NativeApp"
        }).Build();

        var resources = Array.Empty<ApiResource>();
        var clientLoader = new ConfigureClients(config, new TestLogger<ConfigureClients>());
        var expectedScopes = new[]
        {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile,
                IdentityServerConstants.StandardScopes.OfflineAccess
            };

        // Act
        var clients = clientLoader.GetClients();

        // Assert
        var client = Assert.Single(clients);
        Assert.Equal("MyClient", client.ClientId);
        Assert.Equal("MyClient", client.ClientName);
        Assert.False(client.AllowAccessTokensViaBrowser);
        Assert.Equal(new[] { "urn:ietf:wg:oauth:2.0:oob" }, client.RedirectUris.ToArray());
        Assert.Equal(new[] { "urn:ietf:wg:oauth:2.0:oob" }, client.PostLogoutRedirectUris.ToArray());
        Assert.Empty(client.AllowedCorsOrigins);
        Assert.False(client.RequireConsent);
        Assert.Empty(client.ClientSecrets);
        Assert.Equal(GrantTypes.Code.ToArray(), client.AllowedGrantTypes.ToArray());
        Assert.True(client.RequirePkce);
        Assert.False(client.AllowPlainTextPkce);
    }

    [Fact]
    public void GetClients_ReadsSPAFromConfiguration()
    {
        // Arrange
        var expectedRedirectUrl = "https://www.example.com/authenticate";
        var expectedLogoutUrl = "https://www.example.com/logout";
        var expectedAllowedOrigins = "https://www.example.com";

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["MyClient:Profile"] = "SPA",
            ["MyClient:RedirectUri"] = expectedRedirectUrl,
            ["MyClient:LogoutUri"] = expectedLogoutUrl,
        }).Build();

        var resources = Array.Empty<ApiResource>();
        var expectedScopes = new[]
        {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile
            };

        var clientLoader = new ConfigureClients(config, new TestLogger<ConfigureClients>());

        // Act
        var clients = clientLoader.GetClients();

        // Assert
        var client = Assert.Single(clients);
        Assert.Equal("MyClient", client.ClientId);
        Assert.Equal("MyClient", client.ClientName);
        Assert.True(client.AllowAccessTokensViaBrowser);
        Assert.Equal(new[] { expectedRedirectUrl }, client.RedirectUris.ToArray());
        Assert.Equal(new[] { expectedLogoutUrl }, client.PostLogoutRedirectUris.ToArray());
        Assert.Equal(new[] { expectedAllowedOrigins }, client.AllowedCorsOrigins);
        Assert.False(client.RequireConsent);
        Assert.Empty(client.ClientSecrets);
        Assert.Equal(GrantTypes.Code.ToArray(), client.AllowedGrantTypes.ToArray());
    }

    [Fact]
    public void Configure_AddsClientsToExistingClientsList()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
        {
            ["MyClient:Profile"] = "IdentityServerSPA"
        }).Build();

        var resources = Array.Empty<ApiResource>();
        var expectedScopes = new[]
        {
                IdentityServerConstants.StandardScopes.OpenId,
                IdentityServerConstants.StandardScopes.Profile
            };

        var clientLoader = new ConfigureClients(config, new TestLogger<ConfigureClients>());

        var options = new ApiAuthorizationOptions();

        // Act
        clientLoader.Configure(options);

        // Assert
        var client = Assert.Single(options.Clients);
        Assert.Equal("MyClient", client.ClientId);
        Assert.Equal("MyClient", client.ClientName);
        Assert.True(client.AllowAccessTokensViaBrowser);
        Assert.Equal(new[] { "/authentication/login-callback" }, client.RedirectUris.ToArray());
        Assert.Equal(new[] { "/authentication/logout-callback" }, client.PostLogoutRedirectUris.ToArray());
        Assert.Empty(client.AllowedCorsOrigins);
        Assert.False(client.RequireConsent);
        Assert.Empty(client.ClientSecrets);
        Assert.Equal(GrantTypes.Code.ToArray(), client.AllowedGrantTypes.ToArray());
    }
}
