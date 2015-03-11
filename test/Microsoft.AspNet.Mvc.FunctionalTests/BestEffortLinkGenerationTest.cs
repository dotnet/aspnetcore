// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.


using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{

    public class BestEffortLinkGenerationTest
    {
        private const string SiteName = nameof(BestEffortLinkGenerationWebSite);
        private readonly Action<IApplicationBuilder> _app = new BestEffortLinkGenerationWebSite.Startup().Configure;

        private const string ExpectedOutput = @"<html>
<body>
<a href=""/Home/About"">About Us</a>
</body>
</html>";

        [Fact]
        public async Task GenerateLink_NonExistentAction()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();

            var url = "http://localhost/Home/Index";
            // Act
            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ExpectedOutput, content);
        }
    }
}