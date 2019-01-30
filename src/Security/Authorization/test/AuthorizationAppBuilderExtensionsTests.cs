// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization.Test.TestObjects;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Endpoints;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.AspNetCore.Authorization.Test
{
    public class AuthorizationAppBuilderExtensionsTests
    {
        [Fact]
        public async Task UseAuthorization_RegistersMiddleware()
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

        private IServiceProvider CreateServices(IAuthenticationService authenticationService)
        {
            var services = new ServiceCollection();

            services.AddAuthorization(options => { });
            services.AddAuthorizationPolicyEvaluator();
            services.AddLogging();
            services.AddSingleton(authenticationService);

            var serviceProvder = services.BuildServiceProvider();

            return serviceProvder;
        }
    }
}
