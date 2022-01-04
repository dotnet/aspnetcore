// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration;

public class ConfigureClientScopesTests
{
    [Fact]
    public void PostConfigure_AddResourcesScopesToClients()
    {
        // Arrange
        var configureClientScopes = new ConfigureClientScopes(new TestLogger<ConfigureClientScopes>());
        var options = new ApiAuthorizationOptions();
        options.Clients.AddRange(
            ClientBuilder
                .IdentityServerSPA("TestSPA")
                .FromConfiguration()
                .Build(),
            ClientBuilder
                .NativeApp("NativeApp")
                .FromConfiguration()
                .Build());

        options.ApiResources.AddRange(
            ApiResourceBuilder.ApiResource("ResourceApi")
                .FromConfiguration()
                .AllowAllClients()
                .Build());

        // Act
        configureClientScopes.PostConfigure(Options.DefaultName, options);

        // Assert
        foreach (var client in options.Clients)
        {
            Assert.Contains("ResourceApi", client.AllowedScopes);
        }
    }

    [Fact]
    public void PostConfigure_AddIdentityResourcesScopesToClients()
    {
        // Arrange
        var configureClientScopes = new ConfigureClientScopes(new TestLogger<ConfigureClientScopes>());
        var options = new ApiAuthorizationOptions();
        options.Clients.AddRange(
            ClientBuilder
                .IdentityServerSPA("TestSPA")
                .FromConfiguration()
                .Build(),
            ClientBuilder
                .NativeApp("NativeApp")
                .FromConfiguration()
                .Build());

        options.ApiResources.AddRange(
            ApiResourceBuilder.ApiResource("ResourceAPI")
                .FromConfiguration()
                .AllowAllClients()
                .Build());

        // Act
        configureClientScopes.PostConfigure(Options.DefaultName, options);

        // Assert
        var spaClient = Assert.Single(options.Clients, c => c.ClientId == "TestSPA");
        Assert.Equal(new[] { "openid", "profile", "ResourceAPI" }.OrderBy(id => id).ToArray(), spaClient.AllowedScopes.OrderBy(id => id).ToArray());

        var nativeApp = Assert.Single(options.Clients, c => c.ClientId == "NativeApp");
        Assert.Equal(new[] { "offline_access", "openid", "profile", "ResourceAPI" }.OrderBy(id => id).ToArray(), nativeApp.AllowedScopes.OrderBy(id => id).ToArray());
    }
}
