// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using RazorWebSite;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    // Test to verify compilation options from the application are used to compile
    // precompiled and dynamically compiled views.
    public class CompilationOptionsTests
    {
        private readonly IServiceProvider _provider = TestHelper.CreateServices(nameof(RazorWebSite));
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;

        [Fact]
        public async Task CompilationOptions_AreUsedByViewsAndPartials()
        {
            // Arrange
#if ASPNET50
            var expected =
@"This method is running from ASPNET50

This method is only defined in ASPNET50";
#elif ASPNETCORE50
            var expected = 
@"This method is running from ASPNETCORE50

This method is only defined in ASPNETCORE50";
#endif
            var server = TestServer.Create(_provider, _app);
            var client = server.CreateClient();

            // Act
            var body = await client.GetStringAsync("http://localhost/ViewsConsumingCompilationOptions/");

            // Assert
            Assert.Equal(expected, body.Trim());
        }
    }
}