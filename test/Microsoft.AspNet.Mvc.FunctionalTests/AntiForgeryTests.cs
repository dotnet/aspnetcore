// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class AntiForgeryTests
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("AntiForgeryWebSite");
        private readonly Action<IApplicationBuilder> _app = new AntiForgeryWebSite.Startup().Configure;

        [Fact]
        public async Task MultipleAFTokensWithinTheSamePage_AreAllowed()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // Act
            var response = await client.GetAsync("http://localhost/Account/Login");

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var header = Assert.Single(response.Headers.GetValues("X-Frame-Options"));
            Assert.Equal("SAMEORIGIN", header);

            var setCookieHeader = response.Headers.GetValues("Set-Cookie").ToArray();
            Assert.Equal(2, setCookieHeader.Length);
            Assert.True(setCookieHeader[0].StartsWith("__RequestVerificationToken"));
            Assert.True(setCookieHeader[1].StartsWith("__RequestVerificationToken"));
        }
    }
}