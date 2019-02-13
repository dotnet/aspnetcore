// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    public class FiltersTest : IClassFixture<MvcTestFixture<BasicWebSite.Startup>>
    {
        public FiltersTest(MvcTestFixture<BasicWebSite.Startup> fixture)
        {
            Client = fixture.CreateDefaultClient();
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task CanAuthorize_UsersByRole()
        {
            // Arrange & Act
            var response = await Client.GetAsync("AuthorizeUser/AdminRole");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello World!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task CanAuthorize_UsersByPolicyRequirements()
        {
            // Arrange & Act
            var response = await Client.GetAsync("AuthorizeUser/ApiManagers");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello World!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task ImpossiblePolicyFailsAuthorize()
        {
            // Arrange & Act
            var response = await Client.GetAsync("AuthorizeUser/Impossible");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Theory]
        [InlineData("en-US", "en-US")]
        [InlineData("fr", "fr")]
        [InlineData("ab-cd", "en-US")]
        public async Task MiddlewareFilter_LocalizationMiddlewareRegistration_UsesRouteDataToFindCulture(
            string culture,
            string expected)
        {
            // Arrange & Act
            var response = await Client.GetAsync($"{culture}/Filters/MiddlewareFilterTest");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(
                $"CurrentCulture:{expected},CurrentUICulture:{expected}",
                await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task AlwaysRunResultFilters_CanRunWhenResourceFiltersShortCircuit()
        {
            // Arrange
            var url = "Filters/AlwaysRunResultFiltersCanRunWhenResourceFilterShortCircuit";
            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent("Test", Encoding.UTF8, "application/json"),
            };

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            Assert.Equal(422, (int)response.StatusCode);
            Assert.Equal("Can't process this!", await response.Content.ReadAsStringAsync());
        }

        [Fact]
        public async Task FiltersCanBeDeclaredGlobally()
        {
            // Arrange
            var url = "Filters/TraceResult";

            // Act
            var response = await Client.GetStringAsync(url);

            // Assert
            Assert.Equal("This value was set by TraceResourceFilter", response);
        }

        [Fact]
        public async Task ServiceFiltersWork()
        {
            // Arrange
            var url = "Filters/ServiceFilterTest";

            // Act
            var response = await Client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Service filter content", await response.Content.ReadAsStringAsync());
            Assert.Equal(new[] { "True" }, response.Headers.GetValues("X-ServiceActionFilter"));
        }
    }
}
