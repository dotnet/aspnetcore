// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class AntiForgeryTests
    {
        private readonly IServiceProvider _services;
        private readonly Action<IBuilder> _app = new AntiForgeryWebSite.Startup().Configure;

        public AntiForgeryTests()
        {
            _services = TestHelper.CreateServices("AntiForgeryWebSite");
        }

        [Fact]
        public async Task MultipleAFTokensWithinTheSamePage_AreAllowed()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.Handler;

            // Act
            var response = await client.GetAsync("http://localhost/Account/Login");

            //Assert
            Assert.Equal(200, response.StatusCode);
            Assert.Equal("SAMEORIGIN", response.Headers["X-Frame-Options"]);

            var setCookieHeader = response.Headers.GetCommaSeparatedValues("Set-Cookie");
            Assert.Equal(2, setCookieHeader.Count);
            Assert.Equal(true, setCookieHeader[0].StartsWith("__RequestVerificationToken"));
            Assert.Equal(true, setCookieHeader[1].StartsWith("__RequestVerificationToken"));
        }
    }
}