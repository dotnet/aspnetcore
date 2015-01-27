// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ControllersFromServicesWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ControllerFromServicesTest
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices(
            nameof(ControllersFromServicesWebSite));
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ControllersWithConstructorInjectionAreCreatedAndActivated()
        {
            // Arrange
            var expected = "/constructorinjection 14 test-header-value";
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Test-Header", "test-header-value");

            // Act
            var response = await client.GetStringAsync("http://localhost/constructorinjection?value=14");

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task TypesDerivingFromControllerAreRegistered()
        {
            // Arrange
            var expected = "No schedules available for 23";
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/schedule/23");

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task TypesWithControllerSuffixAreRegistered()
        {
            // Arrange
            var expected = "Updated record employee303";
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.PutAsync("http://localhost/employee/update_records?recordId=employee303", 
                                                 new StringContent(string.Empty));

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task TypesWithControllerSuffixAreConventionalRouted()
        {
            // Arrange
            var expected = "Saved record employee #211";
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.PostAsync("http://localhost/employeerecords/save/211",
                                                  new StringContent(string.Empty));

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("generic")]
        [InlineData("nested")]
        [InlineData("not-in-services")]
        public async Task AddControllersFromServices_UsesControllerDiscoveryContentions(string action)
        {
            // Arrange
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/not-discovered/" + action);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
