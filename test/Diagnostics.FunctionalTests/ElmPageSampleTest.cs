// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests
{
    public class ElmPageSampleTest : IClassFixture<TestFixture<ElmPageSample.Startup>>
    {
        public ElmPageSampleTest(TestFixture<ElmPageSample.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task ElmPagePage_ShowsError()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/throws");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("Elm Logs", body);
        }
    }
}
