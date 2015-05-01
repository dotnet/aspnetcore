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
    // Test to verify compilation options from the application are used to compile
    // precompiled and dynamically compiled views.
    public class CompilationOptionsTests
    {
        private const string SiteName = nameof(RazorWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Action<IServiceCollection> _configureServices = new Startup().ConfigureServices;

        [Fact]
        public async Task CompilationOptions_AreUsedByViewsAndPartials()
        {
            // Arrange
#if DNX451
            var expected =
@"This method is running from DNX451

This method is only defined in DNX451";
#elif DNXCORE50
            var expected = 
@"This method is running from DNXCORE50

This method is only defined in DNXCORE50";
#endif
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewsConsumingCompilationOptions/");

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}
