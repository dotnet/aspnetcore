// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Identity.DefaultUI.WebSite;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class ManagementTests : IClassFixture<ServerFactory>
    {
        public ManagementTests(ServerFactory serverFactory)
        {
            ServerFactory = serverFactory;
        }

        public ServerFactory ServerFactory { get; }

        [Fact]
        public async Task CanEnableTwoFactorAuthentication()
        {
            // Arrange
            var client = ServerFactory.CreateDefaultClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var index = await UserStories.RegisterNewUserAsync(client, userName, password);

            // Act & Assert
            await UserStories.EnableTwoFactorAuthentication(index);
        }

        [Fact]
        public async Task CanConfirmEmail()
        {
            // Arrange
            var emails = new ContosoEmailSender();
            var server = ServerFactory.CreateServer(builder =>
                builder.ConfigureServices(s => s.SetupTestEmailSender(emails)));
            var client = ServerFactory.CreateDefaultClient(server);

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var index = await UserStories.RegisterNewUserAsync(client, userName, password);
            var manageIndex = await UserStories.SendEmailConfirmationLinkAsync(index);

            // Act & Assert
            Assert.Equal(2, emails.SentEmails.Count);
            var email = emails.SentEmails[1];
            await UserStories.ConfirmEmailAsync(email, client);
        }

        [Fact]
        public async Task CanDownloadPersonalData()
        {
            // Arrange
            var client = ServerFactory.CreateDefaultClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var index = await UserStories.RegisterNewUserAsync(client, userName, password);

            // Act & Assert
            var jsonData = await UserStories.DownloadPersonalData(index, userName);
            Assert.Contains($"\"Id\":\"", jsonData);
            Assert.Contains($"\"UserName\":\"{userName}\"", jsonData);
            Assert.Contains($"\"Email\":\"{userName}\"", jsonData);
            Assert.Contains($"\"EmailConfirmed\":\"False\"", jsonData);
            Assert.Contains($"\"PhoneNumber\":\"null\"", jsonData);
            Assert.Contains($"\"PhoneNumberConfirmed\":\"False\"", jsonData);
            Assert.Contains($"\"TwoFactorEnabled\":\"False\"", jsonData);
        }

        [Fact]
        public async Task CanDeleteUser()
        {
            // Arrange
            var client = ServerFactory.CreateDefaultClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            var index = await UserStories.RegisterNewUserAsync(client, userName, password);

            // Act & Assert
            await UserStories.DeleteUser(index, password);
        }
    }
}
