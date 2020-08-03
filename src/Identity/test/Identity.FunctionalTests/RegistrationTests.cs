// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Identity.DefaultUI.WebSite;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public abstract class RegistrationTests<TStartup, TContext> : IClassFixture<ServerFactory<TStartup, TContext>>
        where TStartup : class
        where TContext : DbContext
    {
        protected RegistrationTests(ServerFactory<TStartup, TContext> serverFactory)
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
        public async Task CanRegisterAUserWithRequiredConfirmation()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) { services.Configure<IdentityOptions>(o => o.SignIn.RequireConfirmedAccount = true); };

            var server = ServerFactory
                    .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));
            var client = server.CreateClient();
            var client2 = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            // Act & Assert
            var register = await UserStories.RegisterNewUserAsyncWithConfirmation(client, userName, password);

            // Since we aren't confirmed yet, login should fail until we confirm
            await UserStories.LoginFailsWithWrongPasswordAsync(client, userName, password);
            await register.ClickConfirmLinkAsync();
            await UserStories.LoginExistingUserAsync(client, userName, password);
        }

        private class FakeEmailSender : IEmailSender
        {
            public Task SendEmailAsync(string email, string subject, string htmlMessage)
                => Task.CompletedTask;
        }

        [Fact]
        public async Task RegisterWithRealConfirmationDoesNotShowLink()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) {
                services.Configure<IdentityOptions>(o => o.SignIn.RequireConfirmedAccount = true);
                services.AddSingleton<IEmailSender, FakeEmailSender>();
            };

            var server = ServerFactory
                    .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));
            var client = server.CreateClient();
            var client2 = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            // Act & Assert
            var register = await UserStories.RegisterNewUserAsyncWithConfirmation(client, userName, password, hasRealEmailSender: true);

            // Since we aren't confirmed yet, login should fail until we confirm
            await UserStories.LoginFailsWithWrongPasswordAsync(client, userName, password);
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
        public async Task CanRegisterWithASocialLoginProviderFromLogin()
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
        public async Task CanRegisterWithASocialLoginProviderFromLoginWithConfirmation()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services)
            {
                services.Configure<IdentityOptions>(o => o.SignIn.RequireConfirmedAccount = true)
                        .SetupTestThirdPartyLogin();
            }

            var client = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices))
                .CreateClient();

            var guid = Guid.NewGuid();
            var userName = $"{guid}";
            var email = $"{guid}@example.com";

            // Act & Assert
            await UserStories.RegisterNewUserWithSocialLoginWithConfirmationAsync(client, userName, email);
        }

        [Fact]
        public async Task CanRegisterWithASocialLoginProviderFromLoginWithConfirmationAndRealEmailSender()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services)
            {
                services.SetupTestEmailSender(emailSender);
                services
                        .Configure<IdentityOptions>(o => o.SignIn.RequireConfirmedAccount = true)
                        .SetupTestThirdPartyLogin();
            }

            var client = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices))
                .CreateClient();

            var guid = Guid.NewGuid();
            var userName = $"{guid}";
            var email = $"{guid}@example.com";

            // Act & Assert
            await UserStories.RegisterNewUserWithSocialLoginWithConfirmationAsync(client, userName, email, hasRealEmailSender: true);
            Assert.Single(emailSender.SentEmails);
        }

        [Fact]
        public async Task CanRegisterWithASocialLoginProviderFromRegister()
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
            await UserStories.RegisterNewUserWithSocialLoginAsyncViaRegisterPage(client, userName, email);
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

        [Fact]
        public async Task RegisterWithASocialLoginProviderSetsAuthenticationMethodClaim()
        {
            // Arrange
            string authenticationMethod = null;

            void ConfigureTestServices(IServiceCollection services) =>
                services
                    .SetupTestThirdPartyLogin()
                    .SetupGetUserClaimsPrincipal(user =>
                        authenticationMethod = user.FindFirstValue(ClaimTypes.AuthenticationMethod), IdentityConstants.ApplicationScheme);

            var client = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices))
                .CreateClient();

            var guid = Guid.NewGuid();
            var userName = $"{guid}";
            var email = $"{guid}@example.com";

            // Act & Assert
            await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);

            Assert.Equal("Contoso", authenticationMethod);
        }
    }
}
