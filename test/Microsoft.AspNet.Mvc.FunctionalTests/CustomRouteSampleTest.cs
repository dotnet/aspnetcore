// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class CustomRouteSampleTest : IClassFixture<MvcTestFixture<CustomRouteSample.Web.Startup>>
    {
        public CustomRouteSampleTest(MvcTestFixture<CustomRouteSample.Web.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("Javier", "Hola from Spain.")]
        [InlineData("Doug", "Hello from Canada.")]
        [InlineData("Ryan", "Hello from the USA.")]
        public async Task RouteToLocale_ConventionalRoute_BasedOnUser(string user, string expected)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Products/Index");
            request.Headers.Add("User", user);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }

        [Theory]
        [InlineData("Javier", "Hello from es-ES.")]
        [InlineData("Doug", "Hello from en-CA.")]
        [InlineData("Ryan", "Hello from en-US.")]
        public async Task RouteWithAttributeRoute_IncludesLocale_BasedOnUser(string user, string expected)
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Orders/5");
            request.Headers.Add("User", user);

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal(expected, content);
        }
    }
}