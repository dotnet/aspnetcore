// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{

    public class BestEffortLinkGenerationTest : IClassFixture<MvcTestFixture<BestEffortLinkGenerationWebSite.Startup>>
    {
        private const string ExpectedOutput = @"<html>
<body>
<a href=""/Home/About"">About Us</a>
</body>
</html>";

        public BestEffortLinkGenerationTest(MvcTestFixture<BestEffortLinkGenerationWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task GenerateLink_NonExistentAction()
        {
            // Arrange
            var url = "http://localhost/Home/Index";

            // Act
            var response = await Client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ExpectedOutput, content, ignoreLineEndingDifferences: true);
        }
    }
}