// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ControllersFromServicesWebSite;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ControllerFromServicesTest
    {
        private const string SiteName = nameof(ControllersFromServicesWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ControllersWithConstructorInjectionAreCreatedAndActivated()
        {
            // Arrange
            var expected = "/constructorinjection 14 test-header-value";
            var server = TestHelper.CreateServer(_app, SiteName);
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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/schedule/23");

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task TypesDerivingFromControllerPrefixedTypesAreRegistered()
        {
            // Arrange
            var expected = "4";
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetStringAsync("http://localhost/inventory/");

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task TypesWithControllerSuffixAreRegistered()
        {
            // Arrange
            var expected = "Updated record employee303";
            var server = TestHelper.CreateServer(_app, SiteName);
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
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.PostAsync("http://localhost/employeerecords/save/211",
                                                  new StringContent(string.Empty));

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal(expected, await response.Content.ReadAsStringAsync());
        }

        [Theory]
        [InlineData("not-discovered/generic")]
        [InlineData("not-discovered/nested")]
        [InlineData("not-discovered/not-in-services")]
        [InlineData("ClientUIStub/GetClientContent/5")]
        public async Task AddControllersFromServices_UsesControllerDiscoveryContentions(string action)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/" + action);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
