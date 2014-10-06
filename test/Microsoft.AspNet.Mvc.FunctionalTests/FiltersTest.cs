// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Newtonsoft.Json;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class FiltersTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("FiltersWebSite");
        private readonly Action<IApplicationBuilder> _app = new FiltersWebSite.Startup().Configure;

        [Fact]
        public async Task ListAllFilters()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Products/GetPrice/5");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<decimal>(body);

            Assert.Equal(19.95m, result);

            var filters = response.Headers.GetValues("filters");
            Assert.Equal(
                new string[]
                {
                    // This one uses order to set itself 'first' even though it appears on the controller
                    "FiltersWebSite.PassThroughActionFilter",

                    // Configured as global with default order
                    "FiltersWebSite.GlobalExceptionFilter",

                    // Configured on the controller with default order
                    "FiltersWebSite.PassThroughResultFilter",

                    // Configured on the action with default order
                    "FiltersWebSite.PassThroughActionFilter",

                    // The controller itself
                    "FiltersWebSite.ProductsController",
                },
                filters);
        }

        [Fact]
        public async Task AnonymousUsersAreBlocked()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Anonymous/GetHelloWorld");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CanAuthorizeParticularUsers()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/AuthorizeUser/ReturnHelloWorldOnlyForAuthorizedUser");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello World!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ExceptionFilterHandlesAnException()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Exception/GetError?error=RandomError");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("GlobalExceptionFilter.OnException", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ServiceFilterUsesRegisteredServicesAsFilter()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/RandomNumber/GetRandomNumber");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("4", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ServiceFilterThrowsIfServiceIsNotRegistered()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var url = "http://localhost/RandomNumber/GetAuthorizedRandomNumber";

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => client.GetAsync(url));
        }

        [Fact]
        public async Task TypeFilterInitializesArguments()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var url = "http://localhost/RandomNumber/GetModifiedRandomNumber?randomNumber=10";

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("22", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TypeFilterThrowsIfServicesAreNotRegistered()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();
            var url = "http://localhost/RandomNumber/GetHalfOfModifiedRandomNumber?randomNumber=3";

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => client.GetAsync(url));
        }

        [Fact]
        public async Task ActionFilterOverridesActionExecuted()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/XmlSerializer/GetDummyClass?sampleInput=10");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>10</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ResultFilterOverridesOnResultExecuting()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DummyClass/GetDummyClass");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("<DummyClass xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" " +
                "xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><SampleInt>120</SampleInt></DummyClass>",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ResultFilterOverridesOnResultExecuted()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/DummyClass/GetEmptyActionResult");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var result = response.Headers.GetValues("OnResultExecuted");
            Assert.Equal(new string[] { "ResultExecutedSuccessfully" }, result);
        }
    }
}