// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.Service.Claims;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultTokenClaimsManagerTest
    {
        [Fact]
        public async Task DefaultTokenClaimsManager_CallsProviders_InAscendingOrderAsync()
        {
            // Arrange
            var context = new TokenGeneratingContext(new ClaimsPrincipal(), new ClaimsPrincipal(), new OpenIdConnectMessage(), new RequestGrants());
            var resultsList = new List<string>();

            var firstProvider = new Mock<ITokenClaimsProvider>();
            firstProvider.SetupGet(p => p.Order).Returns(100);
            firstProvider.Setup(p => p.OnGeneratingClaims(It.IsAny<TokenGeneratingContext>()))
                .Callback(() => resultsList.Add("first"))
                .Returns(Task.CompletedTask);

            var secondProvider = new Mock<ITokenClaimsProvider>();
            secondProvider.SetupGet(p => p.Order).Returns(101);
            secondProvider.Setup(p => p.OnGeneratingClaims(It.IsAny<TokenGeneratingContext>()))
                .Callback(() => resultsList.Add("second"))
                .Returns(Task.CompletedTask);

            var manager = new DefaultTokenClaimsManager(new[] { secondProvider.Object, firstProvider.Object });

            // Act
            await manager.CreateClaimsAsync(context);

            // Assert
            Assert.Equal(new List<string> { "first", "second" }, resultsList);
        }
    }
}
