// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Test.TestObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authorization.Test
{
    public class AuthorizationAppBuilderExtensionsTests
    {
        [Fact]
        public async Task UseAuthorization_HasRequiredSevices_RegistersMiddleware()
        {
            // Arrange
            var authenticationService = new TestAuthenticationService();
            var services = CreateServices(authenticationService);

            var app = new ApplicationBuilder(services);

            app.UseAuthorization();

            var appFunc = app.Build();

            var endpoint = new Endpoint(
                null,
                new EndpointMetadataCollection(new AuthorizeAttribute()),
                "Test endpoint");

            var httpContext = new DefaultHttpContext();
            httpContext.RequestServices = services;
            httpContext.SetEndpoint(endpoint);

            // Act
            await appFunc(httpContext);

            // Assert
            Assert.True(authenticationService.ChallengeCalled);
        }

        [Fact]
        public void UseAuthorization_MissingRequiredSevices_FriendlyErrorMessage()
        {
            // Arrange
            var authenticationService = new TestAuthenticationService();

            var app = new ApplicationBuilder(new ServiceCollection().BuildServiceProvider());

            // Act
            var ex = Assert.Throws<InvalidOperationException>(() =>
            {
                app.UseAuthorization();
            });

            // Assert
            Assert.Equal(
                "Unable to find the required services. Please add all the required services by calling " +
                "'IServiceCollection.AddAuthorization' inside the call to 'ConfigureServices(...)' " +
                "in the application startup code.",
                ex.Message);
        }

        private IServiceProvider CreateServices(IAuthenticationService authenticationService)
        {
            var services = new ServiceCollection();

            services.AddAuthorization();
            services.AddLogging();
            services.AddSingleton(authenticationService);

            var serviceProvder = services.BuildServiceProvider();

            return serviceProvder;
        }
    }
}
