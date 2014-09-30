// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using ActivatorWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ActivatorTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("ActivatorWebSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ControllerThatCannotBeActivated_ThrowsWhenAttemptedToBeInvoked()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedMessage = "TODO: No service for type 'ActivatorWebSite.CannotBeActivatedController+FakeType' " +
                                   "has been registered.";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(() => client.GetAsync("http://localhost/CannotBeActivated/Index"));
            Assert.Equal(expectedMessage, ex.Message);
        }

        [Fact]
        public async Task PropertiesForPocoControllersAreInitialized()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expected = "4|some-text";

            // Act
            var response = await client.GetAsync("http://localhost/Plain?foo=some-text");

            // Assert
            var headerValue = Assert.Single(response.Headers.GetValues("X-Fake-Header"));
            Assert.Equal("Fake-Value", headerValue);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, body);
        }

        [Fact]
        public async Task PropertiesForTypesDerivingFromControllerAreInitialized()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expected = "Hello world";

            // Act
            var body = await client.GetStringAsync("http://localhost/Regular");

            // Assert
            Assert.Equal(expected, body);
        }

        [Fact]
        public async Task ViewActivator_ActivatesDefaultInjectedProperties()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expected = @"<label for=""Hello"">Hello</label> world! /View/ConsumeServicesFromBaseType";

            // Act
            var body = await client.GetStringAsync("http://localhost/View/ConsumeDefaultProperties");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewActivator_ActivatesAndContextualizesInjectedServices()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expected = "4 test-value";

            // Act
            var body = await client.GetStringAsync("http://localhost/View/ConsumeInjectedService?test=test-value");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewActivator_ActivatesServicesFromBaseType()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expected = @"/content/scripts/test.js";

            // Act
            var body = await client.GetStringAsync("http://localhost/View/ConsumeServicesFromBaseType");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewComponentActivator_ActivatesProperties()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expected = @"Random Number:4";

            // Act
            var body = await client.GetStringAsync("http://localhost/View/ConsumeViewComponent");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewComponentActivator_ActivatesPropertiesAndContextualizesThem()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expected = "test-value";

            // Act
            var body = await client.GetStringAsync("http://localhost/View/ConsumeValueComponent?test=test-value");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewComponentActivator_ActivatesPropertiesAndContextualizesThem_WhenMultiplePropertiesArePresent()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expected = "Random Number:4 test-value";

            // Act
            var body = await client.GetStringAsync("http://localhost/View/ConsumeViewAndValueComponent?test=test-value");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewComponentThatCannotBeActivated_ThrowsWhenAttemptedToBeInvoked()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            var expectedMessage = "TODO: No service for type 'ActivatorWebSite.CannotBeActivatedComponent+FakeType' " +
                                   "has been registered.";

            // Act & Assert
            var ex = await Assert.ThrowsAsync<Exception>(
                () => client.GetAsync("http://localhost/View/ConsumeCannotBeActivatedComponent"));
            Assert.Equal(expectedMessage, ex.Message);
        }
    }
}