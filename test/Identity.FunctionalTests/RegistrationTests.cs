// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public abstract class RegistrationTests<TStartup, TContext> : IClassFixture<ServerFactory<TStartup, TContext>>
        where TStartup : class
        where TContext : DbContext
    {
        public RegistrationTests(ServerFactory<TStartup, TContext> serverFactory)
        {
            ServerFactory = serverFactory;
        }

        public ServerFactory<TStartup, TContext> ServerFactory { get; }

        [Fact]
        public async Task CanRegisterAUser()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) { return; };

            var client = ServerFactory
                    .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices))
                    .CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            // Act & Assert
            await UserStories.RegisterNewUserAsync(client, userName, password);
        }

        [Fact]
        public async Task CanRegisterAUser_WithGlobalAuthorizeFilter()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) =>
                services.SetupGlobalAuthorizeFilter();

            var client = ServerFactory
                    .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices))
                    .CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            // Act & Assert
            await UserStories.RegisterNewUserAsync(client, userName, password);
        }

        [Fact]
        public async Task CanRegisterWithASocialLoginProvider()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) =>
                services
                    .SetupTestThirdPartyLogin();

            var client = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices))
                .CreateClient();

            var guid = Guid.NewGuid();
            var userName = $"{guid}";
            var email = $"{guid}@example.com";

            // Act & Assert
            await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        }

        [Fact]
        public async Task CanRegisterWithASocialLoginProvider_WithGlobalAuthorizeFilter()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) =>
                services
                    .SetupTestThirdPartyLogin()
                    .SetupGlobalAuthorizeFilter();

            var client = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices))
                .CreateClient();

            var guid = Guid.NewGuid();
            var userName = $"{guid}";
            var email = $"{guid}@example.com";

            // Act & Assert
            await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
        }
    }
}
