// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Components.Server.Tests.Circuits
{
    public class FixedAuthenticationStateProviderTest
    {
        [Fact]
        public async Task CannotProvideAuthenticationStateBeforeInitialization()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                new FixedAuthenticationStateProvider()
                    .GetAuthenticationStateAsync());
        }

        [Fact]
        public async Task SuppliesAuthenticationStateWithFixedUser()
        {
            // Arrange
            var user = new ClaimsPrincipal();
            var provider = new FixedAuthenticationStateProvider();
            provider.Initialize(user);

            // Act
            var authenticationState = await provider.GetAuthenticationStateAsync();

            // Assert
            Assert.NotNull(authenticationState);
            Assert.Same(user, authenticationState.User);
        }
    }
}
