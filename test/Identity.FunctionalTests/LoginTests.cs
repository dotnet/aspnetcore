// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Identity.DefaultUI.WebSite;
using Xunit;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class LoginTests : IClassFixture<ServerFactory>
    {
        public LoginTests(ServerFactory serverFactory)
        {
            ServerFactory = serverFactory;
        }

        public ServerFactory ServerFactory { get; }

        [Fact]
        public async Task CanLogInWithAPreviouslyRegisteredUser()
        {
            // Arrange
            var server = ServerFactory.CreateDefaultServer();
            var client = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
            var server = ServerFactory.CreateDefaultServer();
            var client = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
            var server = ServerFactory.CreateServer(builder =>
               builder.ConfigureServices(services => services.SetupGlobalAuthorizeFilter()));
            var client = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
            var server = ServerFactory.CreateDefaultServer();
            var client = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
            var server = ServerFactory.CreateServer(builder =>
                builder.ConfigureServices(services => services.SetupGlobalAuthorizeFilter()));
            var client = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
            var server = ServerFactory.CreateServer(builder =>
            {
                builder.ConfigureServices(services => services
                    .SetupTestEmailSender(emailSender)
                    .SetupEmailRequired());
            });

            var client = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
            var server = ServerFactory.CreateServer(builder =>
            {
                builder.ConfigureServices(services => services
                    .SetupTestEmailSender(emailSender)
                    .SetupEmailRequired());
            });

            var client = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
            var server = ServerFactory.CreateServer(builder =>
                builder.ConfigureServices(services => services.SetupTestThirdPartyLogin()));
            var client = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
            var server = ServerFactory.CreateServer(b => b.ConfigureServices(s =>
                s.SetupTestEmailSender(emailSender)));
            var client = ServerFactory.CreateDefaultClient(server);
            var resetPasswordClient = ServerFactory.CreateDefaultClient(server);
            var newClient = ServerFactory.CreateDefaultClient(server);

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
    }
}
