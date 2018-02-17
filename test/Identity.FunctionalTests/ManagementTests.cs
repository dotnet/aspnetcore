// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class ManagementTests
    {
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
    }
}
