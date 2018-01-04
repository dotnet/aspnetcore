// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.Identity.Service
{
    public class DefaultAuthorizationResponseFactoryTest
    {
        [Fact]
        public async Task CreateAuthorizationResponse_CallsParameterProvidersInOrder()
        {
            // Arrange
            var context = new TokenGeneratingContext(new ClaimsPrincipal(), new ClaimsPrincipal(), new OpenIdConnectMessage(), new RequestGrants());
            var resultsList = new List<string>();

            var firstProvider = new Mock<IAuthorizationResponseParameterProvider>();
            firstProvider.SetupGet(p => p.Order).Returns(100);
            firstProvider.Setup(p => p.AddParameters(It.IsAny<TokenGeneratingContext>(), It.IsAny<AuthorizationResponse>()))
                .Callback(() => resultsList.Add("first"))
                .Returns(Task.CompletedTask);

            var secondProvider = new Mock<IAuthorizationResponseParameterProvider>();
            secondProvider.SetupGet(p => p.Order).Returns(101);
            secondProvider.Setup(p => p.AddParameters(It.IsAny<TokenGeneratingContext>(), It.IsAny<AuthorizationResponse>()))
                .Callback(() => resultsList.Add("second"))
                .Returns(Task.CompletedTask);

            var responseFactory = new DefaultAuthorizationResponseFactory(new[] { secondProvider.Object, firstProvider.Object });

            // Act
            var response = await responseFactory.CreateAuthorizationResponseAsync(context);

            // Assert
            Assert.Equal(new List<string> { "first", "second" }, resultsList);
        }
    }
}
