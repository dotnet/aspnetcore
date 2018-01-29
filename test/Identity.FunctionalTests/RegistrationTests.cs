// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class RegistrationTests
    {
        [Fact]
        public async Task CanRegisterAUser()
        {
            // Arrange
            var client = ServerFactory.CreateDefaultClient();

            var userName = $"{Guid.NewGuid()}@example.com";
            var password = $"!Test.Password1$";

            // Act & Assert
            await UserStories.RegisterNewUserAsync(client, userName, password);
        }
    }
}
