// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Identity.DefaultUI.WebSite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public abstract class LoginTests<TStartup, TContext> : IClassFixture<ServerFactory<TStartup, TContext>>
        where TStartup : class
        where TContext : DbContext
    {
        protected LoginTests(ServerFactory<TStartup, TContext> serverFactory)
        {
            ServerFactory = serverFactory;
        }

        public ServerFactory<TStartup, TContext> ServerFactory { get; }

        [Fact]
        public async Task CanLogInWithAPreviouslyRegisteredUser()
        {
            // Arrange
            var client = ServerFactory.CreateClient();
            var newClient = ServerFactory.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            // Act & Assert
            await UserStories.RegisterNewUserAsync(client, userName, password);

            // Use a new client to simulate a new browser session.
            await UserStories.LoginExistingUserAsync(newClient, userName, password);
        }

        [Fact]
        public async Task CanLogInWithAPreviouslyRegisteredUser_WithGlobalAuthorizeFilter()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) =>
                services.SetupGlobalAuthorizeFilter();

            var server = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            // Act & Assert
            await UserStories.RegisterNewUserAsync(client, userName, password);

            // Use a new client to simulate a new browser session.
            await UserStories.LoginExistingUserAsync(newClient, userName, password);
        }

        [Fact]
        public async Task CanLogInWithTwoFactorAuthentication()
        {
            // Arrange
            var client = ServerFactory.CreateClient();
            var newClient = ServerFactory.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);
            var showRecoveryCodes = await UserStories.EnableTwoFactorAuthentication(loggedIn);

            var twoFactorKey = showRecoveryCodes.Context.AuthenticatorKey;

            // Act & Assert
            // Use a new client to simulate a new browser session.
            await UserStories.LoginExistingUser2FaAsync(newClient, userName, password, twoFactorKey);
        }

        [Fact]
        public async Task CanLogInWithTwoFactorAuthentication_WithGlobalAuthorizeFilter()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) =>
                services.SetupGlobalAuthorizeFilter();

            var server = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);
            var showRecoveryCodes = await UserStories.EnableTwoFactorAuthentication(loggedIn);

            var twoFactorKey = showRecoveryCodes.Context.AuthenticatorKey;

            // Act & Assert
            // Use a new client to simulate a new browser session.
            await UserStories.LoginExistingUser2FaAsync(newClient, userName, password, twoFactorKey);
        }

        [Fact]
        public async Task CanLogInWithRecoveryCode()
        {
            // Arrange
            var client = ServerFactory.CreateClient();
            var newClient = ServerFactory.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);
            var showRecoveryCodes = await UserStories.EnableTwoFactorAuthentication(loggedIn);

            var recoveryCode = showRecoveryCodes.Context.RecoveryCodes.First();

            // Act & Assert
            // Use a new client to simulate a new browser session.
            await UserStories.LoginExistingUserRecoveryCodeAsync(newClient, userName, password, recoveryCode);
        }

        [Fact]
        public async Task CanLogInWithRecoveryCode_WithGlobalAuthorizeFilter()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) =>
                services.SetupGlobalAuthorizeFilter();

            var server = ServerFactory
                .WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));
            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);
            var showRecoveryCodes = await UserStories.EnableTwoFactorAuthentication(loggedIn);

            var recoveryCode = showRecoveryCodes.Context.RecoveryCodes.First();

            // Act & Assert
            // Use a new client to simulate a new browser session.
            await UserStories.LoginExistingUserRecoveryCodeAsync(newClient, userName, password, recoveryCode);
        }

        [Fact]
        public async Task CannotLogInWithoutRequiredEmailConfirmation()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services) => services
                    .SetupTestEmailSender(emailSender)
                    .SetupEmailRequired();

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);

            // Act & Assert
            // Use a new client to simulate a new browser session.
            await Assert.ThrowsAnyAsync<XunitException>(() => UserStories.LoginExistingUserAsync(newClient, userName, password));
        }

        [Fact]
        public async Task CannotLogInWithoutRequiredEmailConfirmation_WithGlobalAuthorizeFilter()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services) => services
                    .SetupTestEmailSender(emailSender)
                    .SetupEmailRequired()
                    .SetupGlobalAuthorizeFilter();

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);

            // Act & Assert
            // Use a new client to simulate a new browser session.
            await Assert.ThrowsAnyAsync<XunitException>(() => UserStories.LoginExistingUserAsync(newClient, userName, password));
        }

        [Fact]
        public async Task CanLogInAfterConfirmingEmail()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services) => services
                .SetupTestEmailSender(emailSender)
                .SetupEmailRequired();

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);

            // Act & Assert
            // Use a new client to simulate a new browser session.
            var email = Assert.Single(emailSender.SentEmails);
            await UserStories.ConfirmEmailAsync(email, newClient);

            await UserStories.LoginExistingUserAsync(newClient, userName, password);
        }

        [Fact]
        public async Task CanResendConfirmingEmail()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services) => services
                .SetupTestEmailSender(emailSender)
                .SetupEmailRequired();

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);

            // Act & Assert
            // Use a new client to simulate a new browser session.
            await UserStories.ResendConfirmEmailAsync(server.CreateClient(), userName);
            Assert.Equal(2, emailSender.SentEmails.Count);
            var email = emailSender.SentEmails.Last();
            await UserStories.ConfirmEmailAsync(email, newClient);
        }

        [Fact]
        public async Task CanLogInAfterConfirmingEmail_WithGlobalAuthorizeFilter()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services) => services
                .SetupTestEmailSender(emailSender)
                .SetupEmailRequired()
                .SetupGlobalAuthorizeFilter();

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var loggedIn = await UserStories.RegisterNewUserAsync(client, userName, password);

            // Act & Assert
            // Use a new client to simulate a new browser session.
            var email = Assert.Single(emailSender.SentEmails);
            await UserStories.ConfirmEmailAsync(email, newClient);

            await UserStories.LoginExistingUserAsync(newClient, userName, password);
        }

        [Fact]
        public async Task CanLoginWithASocialLoginProvider()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) =>
                services.SetupTestThirdPartyLogin();

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var guid = Guid.NewGuid();
            var userName = $"{guid}";
            var email = $"{guid}@example.com";

            // Act & Assert
            await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
            await UserStories.LoginWithSocialLoginAsync(newClient, userName);
        }

        [Fact]
        public async Task CanLoginWithASocialLoginProvider_WithGlobalAuthorizeFilter()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) => services
                .SetupTestThirdPartyLogin()
                .SetupGlobalAuthorizeFilter();

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var guid = Guid.NewGuid();
            var userName = $"{guid}";
            var email = $"{guid}@example.com";

            // Act & Assert
            await UserStories.RegisterNewUserWithSocialLoginAsync(client, userName, email);
            await UserStories.LoginWithSocialLoginAsync(newClient, userName);
        }

        [Fact]
        public async Task CanLogInAfterResettingThePassword()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services) => services
                .SetupTestEmailSender(emailSender);

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var resetPasswordClient = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            var newPassword = $"!New.Password1$";

            await UserStories.RegisterNewUserAsync(client, userName, password);
            var registrationEmail = Assert.Single(emailSender.SentEmails);
            await UserStories.ConfirmEmailAsync(registrationEmail, client);

            // Act & Assert
            await UserStories.ForgotPasswordAsync(resetPasswordClient, userName);
            Assert.Equal(2, emailSender.SentEmails.Count);
            var email = emailSender.SentEmails[1];
            await UserStories.ResetPasswordAsync(resetPasswordClient, email, userName, newPassword);
            await UserStories.LoginExistingUserAsync(newClient, userName, newPassword);
        }

        [Fact]
        public async Task CanResetPassword_WithGlobalAuthorizeFilter()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services) =>
                services.SetupGlobalAuthorizeFilter().SetupTestEmailSender(emailSender);

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var resetPasswordClient = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            var newPassword = $"!New.Password1$";

            await UserStories.RegisterNewUserAsync(client, userName, password);
            var registrationEmail = Assert.Single(emailSender.SentEmails);
            await UserStories.ConfirmEmailAsync(registrationEmail, client);

            // Act & Assert
            await UserStories.ForgotPasswordAsync(resetPasswordClient, userName);
            Assert.Equal(2, emailSender.SentEmails.Count);
            var email = emailSender.SentEmails[1];
            await UserStories.ResetPasswordAsync(resetPasswordClient, email, userName, newPassword);
            await UserStories.LoginExistingUserAsync(newClient, userName, newPassword);
        }

        [Fact]
        public async Task UserNotLockedOut_AfterMaxFailedAccessAttempts_WithGlobalAuthorizeFilter()
        {
            // Arrange
            var emailSender = new ContosoEmailSender();
            void ConfigureTestServices(IServiceCollection services) =>
                services.SetupGlobalAuthorizeFilter().SetupMaxFailedAccessAttempts().SetupTestEmailSender(emailSender);

            var server = ServerFactory.WithWebHostBuilder(whb => whb.ConfigureServices(ConfigureTestServices));

            var client = server.CreateClient();
            var newClient = server.CreateClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";
            var wrongPassword = $"!Wrong.Password1$";

            await UserStories.RegisterNewUserAsync(client, userName, password);
            var registrationEmail = Assert.Single(emailSender.SentEmails);
            await UserStories.ConfirmEmailAsync(registrationEmail, client);

            // Act & Assert
            await UserStories.LoginFailsWithWrongPasswordAsync(newClient, userName, wrongPassword);
        }
    }
}
