// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class DirectivesTest
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices("RazorWebSite");
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task ViewsInheritsUsingsAndInjectDirectivesFromViewStarts()
        {
            var expected = @"Hello Person1";
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Directives/ViewInheritsInjectAndUsingsFromViewStarts");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewInheritsBasePageFromViewStarts()
        {
            var expected = @"WriteLiteral says:layout:Write says:Write says:Hello Person2";
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Directives/ViewInheritsBasePageFromViewStarts");

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}