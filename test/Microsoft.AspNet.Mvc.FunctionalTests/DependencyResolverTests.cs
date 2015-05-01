// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#if DNX451
using System;
using System.Threading.Tasks;
using AutofacWebSite;
using Microsoft.AspNet.Builder;
using Microsoft.Framework.DependencyInjection;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class DependencyResolverTests
    {
        private const string SiteName = nameof(AutofacWebSite);
        private readonly Action<IApplicationBuilder> _app = new Startup().Configure;
        private readonly Func<IServiceCollection, IServiceProvider> _configureServices = new Startup().ConfigureServices;

        [Theory]
        [InlineData("http://localhost/di", "<p>Builder Output: Hello from builder.</p>")]
        [InlineData("http://localhost/basic", "<p>Hello From Basic View</p>")]
        public async Task AutofacDIContainerCanUseMvc(string url, string expectedResponseBody)
        {
            // Arrange & Act & Assert (does not throw)
            // This essentially calls into the Startup.Configuration method
            var server = TestHelper.CreateServer(_app, SiteName, _configureServices);

            // Make a request to start resolving DI pieces
            var response = await server.CreateClient().GetAsync(url);

            var actualResponseBody = await response.Content.ReadAsStringAsync();
            Assert.Equal(expectedResponseBody, actualResponseBody);
        }
    }
}
#endif
