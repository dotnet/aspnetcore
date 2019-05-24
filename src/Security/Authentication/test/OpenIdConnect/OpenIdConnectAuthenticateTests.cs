// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Microsoft.AspNetCore.Authentication.Test.OpenIdConnect
{
    public class OpenIdConnectAuthenticateTests
    {
        [Fact]
        public async Task RegularGetRequestToCallbackPathSkips()
        {
            // Arrange
            var settings = new TestSettings(
                opt =>
                {
                    opt.Authority = TestServerBuilder.DefaultAuthority;
                    opt.CallbackPath = new PathString("/");
                    opt.SkipUnrecognizedRequests = true;
                    opt.ClientId = "Test Id";
                });

            var server = settings.CreateTestServer(handler: async context =>
            {
                await context.Response.WriteAsync("Hi from the callback path");
            });

            // Act
            var transaction = await server.SendAsync("/");

            // Assert
            Assert.Equal("Hi from the callback path", transaction.ResponseText);
        }

        [Fact]
        public async Task RegularPostRequestToCallbackPathSkips()
        {
            // Arrange
            var settings = new TestSettings(
                opt =>
                {
                    opt.Authority = TestServerBuilder.DefaultAuthority;
                    opt.CallbackPath = new PathString("/");
                    opt.SkipUnrecognizedRequests = true;
                    opt.ClientId = "Test Id";
                });

            var server = settings.CreateTestServer(handler: async context =>
            {
                await context.Response.WriteAsync("Hi from the callback path");
            });

            // Act
            var request = new HttpRequestMessage(HttpMethod.Post, "/");
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>());

            var transaction = await server.SendAsync(request, cookieHeader: null);

            // Assert
            Assert.Equal("Hi from the callback path", transaction.ResponseText);
        }
    }
}
