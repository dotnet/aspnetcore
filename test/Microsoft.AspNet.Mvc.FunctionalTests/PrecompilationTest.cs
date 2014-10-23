// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.TestHost;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class PrecompilationTest
    {
        private readonly IServiceProvider _services = TestHelper.CreateServices("PrecompilationWebSite");
        private readonly Action<IApplicationBuilder> _app = new PrecompilationWebSite.Startup().Configure;

        [Fact]
        public async Task PrecompiledView_RendersCorrectly()
        {
            // Arrange
            var server = TestServer.Create(_services, _app);
            var client = server.CreateClient();

            // We will render a view that writes the fully qualified name of the Assembly containing the type of
            // the view. If the view is precompiled, this assembly will be PrecompilationWebsite.
            var expectedContent = typeof(PrecompilationWebSite.Startup).GetTypeInfo().Assembly.GetName().ToString();

            // Act
            var response = await client.GetAsync("http://localhost/Home/Index");
            var responseContent = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(expectedContent, responseContent);
        }
    }
}