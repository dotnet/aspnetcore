// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Diagnostics.FunctionalTests
{
    public class MiddlewareAnalysisSampleTest : IClassFixture<TestFixture<MiddlewareAnaysisSample.Startup>>
    {
        public MiddlewareAnalysisSampleTest(TestFixture<MiddlewareAnaysisSample.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task MiddlewareAnalysisPage_ShowsAnalysis()
        {
            // Arrange
            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/");

            // Act
            var response = await Client.SendAsync(request);

            // Assert
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
