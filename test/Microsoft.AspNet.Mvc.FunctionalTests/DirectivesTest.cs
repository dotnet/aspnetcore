// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class DirectivesTest
    {
        private const string SiteName = nameof(RazorWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Fact]
        public async Task ViewsInheritsUsingsAndInjectDirectivesFromViewStarts()
        {
            var expected = @"Hello Person1";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Directives/ViewInheritsInjectAndUsingsFromViewImports");

            // Assert
            Assert.Equal(expected, body.Trim());
        }

        [Fact]
        public async Task ViewInheritsBasePageFromViewStarts()
        {
            var expected = @"WriteLiteral says:layout:Write says:Write says:Hello Person2";
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/Directives/ViewInheritsBasePageFromViewImports");

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}