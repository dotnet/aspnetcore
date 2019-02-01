// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Linq;
using Microsoft.Extensions.Options;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Configuration
{
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
}
