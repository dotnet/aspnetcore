// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // Each of these tests makes two requests, because we want each test to verify that the data is
    // PER-REQUEST and does not linger around to impact the next request.
    public class RequestServicesTest
    {
        private const string SiteName = nameof(RequestServicesWebSite);
        private readonly Action<IApplicationBuilder> _app = new RequestServicesWebSite.Startup().Configure;

        [Theory]
        [InlineData("http://localhost/RequestScoped/FromController")]
        [InlineData("http://localhost/Other/FromFilter")]
        [InlineData("http://localhost/Other/FromView")]
        [InlineData("http://localhost/Other/FromViewComponent")]
        [InlineData("http://localhost/Other/FromModelProperty")]
        [InlineData("http://localhost/Other/FromActionArgument")]
        public async Task RequestServices(string url)
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            // Act & Assert
            for (var i = 0; i < 2; i++)
            {
                var requestId = Guid.NewGuid().ToString();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("RequestId", requestId);

                var response = await client.SendAsync(request);

                var body = (await response.Content.ReadAsStringAsync()).Trim();
                Assert.Equal(requestId, body);
            }
        }

        [Fact]
        public async Task RequestServices_TagHelper()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url = "http://localhost/Other/FromTagHelper";

            // Act & Assert
            for (var i = 0; i < 2; i++)
            {
                var requestId = Guid.NewGuid().ToString();
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.TryAddWithoutValidation("RequestId", requestId);

                var response = await client.SendAsync(request);

                var body = (await response.Content.ReadAsStringAsync()).Trim();

                var expected = "<request-scoped>" + requestId + "</request-scoped>";
                Assert.Equal(expected, body);
            }
        }

        [Fact]
        public async Task RequestServices_ActionConstraint()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url = "http://localhost/Other/FromActionConstraint";

            // Act & Assert
            var requestId1 = "b40f6ec1-8a6b-41c1-b3fe-928f581ebaf5";
            var request1 = new HttpRequestMessage(HttpMethod.Get, url);
            request1.Headers.TryAddWithoutValidation("RequestId", requestId1);

            var response1 = await client.SendAsync(request1);

            var body1 = (await response1.Content.ReadAsStringAsync()).Trim();
            Assert.Equal(requestId1, body1);

            var requestId2 = Guid.NewGuid().ToString();
            var request2 = new HttpRequestMessage(HttpMethod.Get, url);
            request2.Headers.TryAddWithoutValidation("RequestId", requestId2);

            var response2 = await client.SendAsync(request2);
            Assert.Equal(HttpStatusCode.NotFound, response2.StatusCode);
        }
    }
}