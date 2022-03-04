// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class ControllerFromServicesTest : IClassFixture<MvcTestFixture<ControllersFromServicesWebSite.Startup>>
    {
        public ControllerFromServicesTest(MvcTestFixture<ControllersFromServicesWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ControllersWithConstructorInjectionAreCreatedAndActivated()
        {
            // Arrange
            var expected = "/constructorinjection 14 test-header-value";
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/constructorinjection?value=14");
            request.Headers.TryAddWithoutValidation("Test-Header", "test-header-value");

            // Act
            var response = await Client.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(expected, responseText);
        }

        [Fact]
        public async Task TypesDerivingFromControllerAreRegistered()
        {
            // Arrange
            var expected = "No schedules available for 23";

            // Act
            var response = await Client.GetStringAsync("http://localhost/schedule/23");

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task TypesDerivingFromTypesWithControllerAttributeAreRegistered()
        {
            // Arrange
            var expected = "4";

            // Act
            var response = await Client.GetStringAsync("http://localhost/inventory/");

            // Assert
            Assert.Equal(expected, response);
        }

        [Fact]
        public async Task TypesWithControllerSuffixAreRegistered()
        {
            // Arrange
            var expected = "Updated record employee303";

            // Act
            var response = await Client.PutAsync(
                "http://localhost/employee/update_records?recordId=employee303",
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

            // Act
            var response = await Client.PostAsync(
                "http://localhost/employeerecords/save/211",
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
            // Arrange & Act
            var response = await Client.GetAsync("http://localhost/" + action);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddControllersAsServices_MultipleCalls_DoesNotReplacePreviousProvider()
        {
            // Arrange
            var expected = "1";

            // Act
            var response = await Client.GetStringAsync("http://localhost/another/");

            // Assert
            Assert.Equal(expected, response);
        }
    }
}
