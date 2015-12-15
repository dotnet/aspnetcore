// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class ActionConstraintSampleTest : IClassFixture<MvcTestFixture<ActionConstraintSample.Web.Startup>>
    {
        public ActionConstraintSampleTest(MvcTestFixture<ActionConstraintSample.Web.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ControllerWithActionConstraint_SelectsSpecificController()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Items/US/GetItems");
            request.Headers.Add("User", "Blah");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from US", body);
        }

        [Fact]
        public async Task ControllerWithActionConstraint_NoMatchesFound_SelectsDefaultController()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/Items/CA/GetItems");
            request.Headers.Add("User", "Blah");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("Hello from everywhere", body);
        }
    }
}
