// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class DependencyResolverTests : IClassFixture<MvcTestFixture<AutofacWebSite.Startup>>
    {
        public DependencyResolverTests(MvcTestFixture<AutofacWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Theory]
        [InlineData("http://localhost/di", "<p>Builder Output: Hello from builder.</p>")]
        [InlineData("http://localhost/basic", "<p>Hello From Basic View</p>")]
        public async Task AutofacDIContainerCanUseMvc(string url, string expectedResponseBody)
        {
            // Arrange & Act & Assert (does not throw)
            // Make a request to start resolving DI pieces
            var responseText = await Client.GetStringAsync(url);

            Assert.Equal(expectedResponseBody, responseText);
        }
    }
}
