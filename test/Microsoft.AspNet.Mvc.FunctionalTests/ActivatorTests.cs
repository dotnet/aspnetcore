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
        private readonly Action<IBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ControllerThatCannotBeActivated_ThrowsWhenAttemptedToBeInvoked()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
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
            var client = server.Handler;
            var expected = "4|some-text";

            // Act
            var result = await client.GetAsync("http://localhost/Plain?foo=some-text");

            // Assert
            Assert.Equal("Fake-Value", result.HttpContext.Response.Headers["X-Fake-Header"]);
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expected, body);
        }

        [Fact]
        public async Task PropertiesForTypesDerivingFromControllerAreInitialized()
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expected = "Hello world";

            // Act
            var result = await client.GetAsync("http://localhost/Regular");

            // Assert
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expected, body);
        }

        [Fact]
        public async Task ViewActivator_ActivatesDefaultInjectedProperties()
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expected = "<label for=\"Hello\">Hello</label> world!";

            // Act
            var result = await client.GetAsync("http://localhost/View/ConsumeDefaultProperties");

            // Assert
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewActivator_ActivatesAndContextualizesInjectedServices()
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expected = "4 test-value";

            // Act
            var result = await client.GetAsync("http://localhost/View/ConsumeInjectedService?test=test-value");

            // Assert
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewActivator_ActivatesServicesFromBaseType()
        {
            var server = TestServer.Create(_provider, _app);
            var client = server.Handler;
            var expected = 
@"/content/scripts/test.js
/View/ConsumeDefaultProperties";

            // Act
            var result = await client.GetAsync("http://localhost/View/ConsumeServicesFromBaseType");

            // Assert
            var body = await result.HttpContext.Response.ReadBodyAsStringAsync();
            Assert.Equal(expected, body.Trim());
        }
    }
}