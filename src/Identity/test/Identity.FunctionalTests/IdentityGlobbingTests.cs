// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using System.Threading.Tasks;
using Identity.DefaultUI.Globbing;
using Identity.DefaultUI.Globbing.Data;
using Xunit;

namespace Microsoft.AspNetCore.Identity.FunctionalTests
{
    public class IdentityGlobbingTests : IClassFixture<ServerFactory<Startup, ApplicationDbContext>>
    {
        public IdentityGlobbingTests(ServerFactory<Startup, ApplicationDbContext> serverFactory)
        {
            ServerFactory = serverFactory;
        }

        public ServerFactory<Startup, ApplicationDbContext> ServerFactory { get; }

        [Fact]
        public async Task GlobbingPatternDoesNotFail()
        {
            // Arrange
            var client = ServerFactory
                .CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("/js/dist/dashboardHome.js", content);
            Assert.DoesNotContain("/js/dist/FAKE*.js", content);
        }

        [Fact]
        public async Task GlobbingPatternFindsFolder()
        {
            // Arrange
            var client = ServerFactory
                .CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("/_content/Identity.DefaultUI.RazorClassLib/razorlib/file.js", content);
            Assert.DoesNotContain("RazorClassLib/**/file.js", content);

            var file = await client.GetAsync("/_content/Identity.DefaultUI.RazorClassLib/razorlib/file.js");
            Assert.Equal(HttpStatusCode.OK, file.StatusCode);
        }
    }
}
