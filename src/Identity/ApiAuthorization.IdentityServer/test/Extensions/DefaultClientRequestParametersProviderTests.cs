// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using IdentityServer4.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Microsoft.AspNetCore.ApiAuthorization.IdentityServer.Extensions
{
    public class DefaultClientRequestParametersProviderTests
    {
        [Fact]
        public void GetClientParameters_ReturnsParametersForExistingClients()
        {
            // Arrange
            var absoluteUrlFactory = new Mock<IAbsoluteUrlFactory>();
            absoluteUrlFactory.Setup(auf => auf.GetAbsoluteUrl(It.IsAny<HttpContext>(), It.IsAny<string>()))
                .Returns<HttpContext, string>((_, s) => Uri.IsWellFormedUriString(s, UriKind.Absolute) ? s : new Uri(new Uri("http://localhost/"), s).ToString());

            var options = Options.Create(new ApiAuthorizationOptions());
            options.Value.Clients.AddIdentityServerSPA("SPA", cb =>
                 cb.WithScopes("a/b", "c/d")
                    .WithRedirectUri("authentication/login-callback")
                    .WithLogoutRedirectUri("authentication/logout-callback"));

            var context = new DefaultHttpContext();
            context.Request.Scheme = "http";
            context.Request.Host = new HostString("localhost");
            context.RequestServices = new ServiceCollection()
                .AddSingleton(new IdentityServerOptions())
                .BuildServiceProvider();

            var clientRequestParametersProvider =
                new DefaultClientRequestParametersProvider(
                    absoluteUrlFactory.Object,
                    options);

            var expectedParameters = new Dictionary<string, string>
            {
                ["authority"] = "http://localhost",
                ["client_id"] = "SPA",
                ["redirect_uri"] = "http://localhost/authentication/login-callback",
                ["post_logout_redirect_uri"] = "http://localhost/authentication/logout-callback",
                ["response_type"] = "code",
                ["scope"] = "a/b c/d"
            };

            // Act
            var result = clientRequestParametersProvider.GetClientParameters(context, "SPA");

            // Assert
            Assert.Equal(expectedParameters, result);
        }
    }
}
