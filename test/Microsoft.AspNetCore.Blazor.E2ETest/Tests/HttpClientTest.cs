// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class HttpClientTest : BasicTestAppTestBase, IClassFixture<AspNetSiteServerFixture>
    {
        readonly ServerFixture _apiServerFixture;

        public HttpClientTest(
            BrowserFixture browserFixture,
            DevHostServerFixture<BasicTestApp.Program> devHostServerFixture,
            AspNetSiteServerFixture apiServerFixture)
            : base(browserFixture, devHostServerFixture)
        {
            apiServerFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
            _apiServerFixture = apiServerFixture;

            //Navigate(ServerPathBase, noReload: true);
        }

        [Fact]
        public async Task SanityCheck_ApiServerIsRunning()
        {
            // Just so we can be sure that the other tests are even relevant
            // Note that the HttpClient we're instantiating here is *not* the
            // one under test. This is not related to Blazor in any way.
            var httpClient = new HttpClient { BaseAddress = _apiServerFixture.RootUri };
            var responseText = await httpClient.GetStringAsync("/api/greeting/sayhello");
            Assert.Equal("Hello", responseText);
        }
    }
}
