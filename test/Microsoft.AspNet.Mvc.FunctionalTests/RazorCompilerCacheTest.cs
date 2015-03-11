// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using RazorCompilerCacheWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class RazorCompilerCacheTest
    {
        private const string SiteName = nameof(RazorCompilerCacheWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task CompilerCache_IsNotInitializedUntilFirstViewRequest()
        {
            // Arrange
            var server = TestHelper.CreateServer(_app, SiteName);
            var client = server.CreateClient();
            client.BaseAddress = new Uri("http://localhost");

            // Act - 1
            // Visit a sampling of controller actions that do not produce ViewResult
            var result1 = await client.GetAsync("/file");
            var result2 = await client.GetAsync("/statuscode");
            var result3 = await client.GetStringAsync("/cache-status");

            // Assert - 1
            Assert.Equal(HttpStatusCode.OK, result1.StatusCode);
            Assert.Equal(HttpStatusCode.OK, result2.StatusCode);
            // Ensure the cache was not initialized.
            Assert.Equal(bool.FalseString, result3);

            // Act - 2
            var result4 = await client.GetStringAsync("/view");
            var result5 = await client.GetStringAsync("/cache-status");

            // Assert - 2
            Assert.Equal("Hello from view!", result4);
            Assert.Equal(bool.TrueString, result5);
        }
    }
}