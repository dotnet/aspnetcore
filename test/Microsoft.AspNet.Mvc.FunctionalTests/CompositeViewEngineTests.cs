// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public class CompositeViewEngineTests : IClassFixture<MvcTestFixture<CompositeViewEngineWebSite.Startup>>
    {
        public CompositeViewEngineTests(MvcTestFixture<CompositeViewEngineWebSite.Startup> fixture)
        {
            Client = fixture.Client;
        }

        public HttpClient Client { get; }

        [Fact]
        public async Task CompositeViewEngine_FindsPartialViewsAcrossAllEngines()
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/");

            // Assert
            Assert.Equal("Hello world", body.Trim());
        }

        [Fact]
        public async Task CompositeViewEngine_FindsViewsAcrossAllEngines()
        {
            // Arrange & Act
            var body = await Client.GetStringAsync("http://localhost/Home/TestView");

            // Assert
            Assert.Equal("Content from test view", body.Trim());
        }
    }
}