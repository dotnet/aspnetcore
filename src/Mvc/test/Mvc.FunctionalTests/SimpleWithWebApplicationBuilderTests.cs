// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class SimpleWithWebApplicationBuilderTests : IClassFixture<MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.FakeStartup>>
    {
        public SimpleWithWebApplicationBuilderTests(MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.FakeStartup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task HelloWorld()
        {
            // Arrange
            var expected = "Hello World";

            // Act
            var content = await Client.GetStringAsync("http://localhost/");

            // Assert
            Assert.Equal(expected, content);
        }
    }
}
