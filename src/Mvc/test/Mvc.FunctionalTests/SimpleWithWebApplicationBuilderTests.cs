// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class SimpleWithWebApplicationBuilderTests : IClassFixture<MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.FakeStartup>>
    {
        private readonly MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.FakeStartup> _fixture;

        public SimpleWithWebApplicationBuilderTests(MvcTestFixture<SimpleWebSiteWithWebApplicationBuilder.FakeStartup> fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task HelloWorld()
        {
            // Arrange
            var expected = "Hello World";
            using var client = _fixture.CreateDefaultClient();

            // Act
            var content = await client.GetStringAsync("http://localhost/");

            // Assert
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task JsonResult_Works()
        {
            // Arrange
            var expected = "{\"name\":\"John\",\"age\":42}";
            using var client = _fixture.CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/json");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task OkObjectResult_Works()
        {
            // Arrange
            var expected = "{\"name\":\"John\",\"age\":42}";
            using var client = _fixture.CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/ok-object");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.OK);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task AcceptedObjectResult_Works()
        {
            // Arrange
            var expected = "{\"name\":\"John\",\"age\":42}";
            using var client = _fixture.CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/accepted-object");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.Accepted);
            Assert.Equal("/ok-object", response.Headers.Location.ToString());
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task ActionReturningMoreThanOneResult_NotFound()
        {
            // Arrange
            using var client = _fixture.CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/many-results?id=-1");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ActionReturningMoreThanOneResult_Found()
        {
            // Arrange
            using var client = _fixture.CreateDefaultClient();

            // Act
            var response = await client.GetAsync("/many-results?id=7");

            // Assert
            await response.AssertStatusCodeAsync(HttpStatusCode.MovedPermanently);
            Assert.Equal("/json", response.Headers.Location.ToString());
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/33889")]
        public async Task DefaultEnvironment_Is_Development()
        {
            // Arrange
            var expected = "Development";
            using var client = _fixture.CreateDefaultClient();

            // Act
            var content = await client.GetStringAsync("http://localhost/environment");

            // Assert
            Assert.Equal(expected, content);
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/33876")]
        public async Task Configuration_Can_Be_Overridden()
        {
            // Arrange
            var fixture = _fixture.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration(builder =>
                {
                    var config = new[]
                    {
                        KeyValuePair.Create("Greeting", "Bonjour tout le monde"),
                    };

                    builder.AddInMemoryCollection(config);
                });
            });

            var expected = "Bonjour tout le monde";
            using var client = fixture.CreateDefaultClient();

            // Act
            var content = await client.GetStringAsync("http://localhost/greeting");

            // Assert
            Assert.Equal(expected, content);
        }

        [Fact]
        public async Task Environment_Can_Be_Overridden()
        {
            // Arrange
            var fixture = _fixture.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Staging);
            });

            var expected = "Staging";
            using var client = fixture.CreateDefaultClient();

            // Act
            var content = await client.GetStringAsync("http://localhost/environment");

            // Assert
            Assert.Equal(expected, content);
        }
    }
}
