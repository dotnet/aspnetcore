// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace Microsoft.AspNetCore.Mvc.FunctionalTests
{
    // Each of these tests makes two requests, because we want each test to verify that the data is
    // PER-REQUEST and does not linger around to impact the next request.
    public abstract class RequestServicesTestBase<TStartup> : IClassFixture<MvcTestFixture<TStartup>> where TStartup : class
    {
        protected RequestServicesTestBase(MvcTestFixture<TStartup> fixture)
        {
            var factory = fixture.Factories.FirstOrDefault() ?? fixture.WithWebHostBuilder(ConfigureWebHostBuilder);
            Client = factory.CreateDefaultClient();
        }

        private static void ConfigureWebHostBuilder(IWebHostBuilder builder) =>
            builder.UseStartup<TStartup>();

        public HttpClient Client { get; }

        [Fact]
        public abstract Task HasEndpointMatch();

        [Theory]
        [InlineData("http://localhost/RequestScopedService/FromFilter")]
        [InlineData("http://localhost/RequestScopedService/FromView")]
        [InlineData("http://localhost/RequestScopedService/FromViewComponent")]
        [InlineData("http://localhost/RequestScopedService/FromActionArgument")]
        public async Task RequestServices(string url)
        {
            for (var i = 0; i < 2; i++)
            {
                // Arrange
                var requestId = Guid.NewGuid().ToString();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation(HeaderNames.RequestId, requestId);

                // Act
                var response = await Client.SendAsync(request);

                // Assert
                response.EnsureSuccessStatusCode();
                var body = (await response.Content.ReadAsStringAsync()).Trim();
                Assert.Equal(requestId, body);
            }
        }

        [Fact]
        public async Task RequestServices_TagHelper()
        {
            // Arrange
            var url = "http://localhost/RequestScopedService/FromTagHelper";

            // Act & Assert
            for (var i = 0; i < 2; i++)
            {
                var requestId = Guid.NewGuid().ToString();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation(HeaderNames.RequestId, requestId);

                var response = await Client.SendAsync(request);

                var body = (await response.Content.ReadAsStringAsync()).Trim();

                var expected = "<request-scoped>" + requestId + "</request-scoped>";
                Assert.Equal(expected, body);
            }
        }

        [Fact]
        public async Task RequestServices_Constraint()
        {
            // Arrange
            var url = "http://localhost/RequestScopedService/FromConstraint";

            // Act & Assert
            var requestId1 = "b40f6ec1-8a6b-41c1-b3fe-928f581ebaf5";
            var request1 = new HttpRequestMessage(HttpMethod.Get, url);
            request1.Headers.TryAddWithoutValidation(HeaderNames.RequestId, requestId1);

            var response1 = await Client.SendAsync(request1);

            var body1 = (await response1.Content.ReadAsStringAsync()).Trim();
            Assert.Equal(requestId1, body1);

            var requestId2 = Guid.NewGuid().ToString();
            var request2 = new HttpRequestMessage(HttpMethod.Get, url);
            request2.Headers.TryAddWithoutValidation(HeaderNames.RequestId, requestId2);

            var response2 = await Client.SendAsync(request2);
            Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
        }
    }
}