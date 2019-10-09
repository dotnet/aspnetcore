// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.TestHost
{
    public class RequestBuilderTests
    {
        [Fact]
        public void AddRequestHeader()
        {
            var builder = new WebHostBuilder().Configure(app => { });
            var server = new TestServer(builder);
            server.CreateRequest("/")
                .AddHeader("Host", "MyHost:90")
                .And(request =>
                {
                    Assert.Equal("MyHost:90", request.Headers.Host.ToString());
                });
        }

        [Fact]
        public void AddContentHeaders()
        {
            var builder = new WebHostBuilder().Configure(app => { });
            var server = new TestServer(builder);
            server.CreateRequest("/")
                .AddHeader("Content-Type", "Test/Value")
                .And(request =>
                {
                    Assert.NotNull(request.Content);
                    Assert.Equal("Test/Value", request.Content.Headers.ContentType.ToString());
                });
        }

        [Fact]
        public void TestServer_PropertyShouldHoldTestServerInstance()
        {
            // Arrange
            var builder = new WebHostBuilder().Configure(app => { });
            var server = new TestServer(builder);

            // Act
            var requestBuilder = server.CreateRequest("/");

            // Assert
            Assert.Equal(server, requestBuilder.TestServer);
        }
    }
}
