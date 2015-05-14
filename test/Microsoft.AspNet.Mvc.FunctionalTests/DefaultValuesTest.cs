// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class DefaultValuesTest
    {
        private const string SiteName = nameof(BasicWebSite);
        private readonly Action<IApplicationBuilder> _app = new BasicWebSite.Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new BasicWebSite.Startup().ConfigureServices;

        [Fact]
        public async Task Controller_WithDefaultValueAttribut_ReturnsDefault()
        {
            // Arrange
            var expected = "hello";

            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var url = "http://localhost/DefaultValues/EchoValue_DefaultValueAttribute";

            // Act
            var response = await client.GetStringAsync(url);

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task Controller_WithDefaultValueAttribute_ReturnsModelBoundValues()
        {
            // Arrange
            var expected = "cool";

            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var url = "http://localhost/DefaultValues/EchoValue_DefaultValueAttribute?input=cool";

            // Act
            var response = await client.GetStringAsync(url);

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task Controller_WithDefaultParameterValue_ReturnsDefault()
        {
            // Arrange
            var expected = "world";

            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var url = "http://localhost/DefaultValues/EchoValue_DefaultParameterValue";

            // Act
            var response = await client.GetStringAsync(url);

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task Controller_WithDefaultParameterValue_ReturnsModelBoundValues()
        {
            // Arrange
            var expected = "cool";

            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            var url = "http://localhost/DefaultValues/EchoValue_DefaultParameterValue?input=cool";

            // Act
            var response = await client.GetStringAsync(url);

            // Assert
            Assert.Equal(expected, response);
        }
    }
}
