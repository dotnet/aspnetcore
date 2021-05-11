// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BasicTestApp.HttpClientTest;
using Microsoft.AspNetCore.BrowserTesting;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Testing;
using PlaywrightSharp;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class BinaryHttpClientTest : ComponentBrowserTestBase,
        IClassFixture<BasicTestAppServerSiteFixture<CorsStartup>>,
        IClassFixture<BlazorWasmTestAppFixture<BasicTestApp.Program>>
    {
        private readonly BlazorWasmTestAppFixture<BasicTestApp.Program> _devHostServerFixture;
        readonly ServerFixture _apiServerFixture;

        public BinaryHttpClientTest(
            BlazorWasmTestAppFixture<BasicTestApp.Program> devHostServerFixture,
            BasicTestAppServerSiteFixture<CorsStartup> apiServerFixture,
            ITestOutputHelper output)
            : base(output)
        {
            _devHostServerFixture = devHostServerFixture;
            _devHostServerFixture.PathBase = "/subdir";
            _apiServerFixture = apiServerFixture;
        }

        [Fact]
        public async Task CanSendAndReceiveBytes()
        {
            if (BrowserManager.IsAvailable(BrowserKind.Chromium))
            {
                await using var browser = await BrowserManager.GetBrowserInstance(BrowserKind.Chromium, BrowserContextInfo);
                var page = await browser.NewPageAsync();
                var url = _devHostServerFixture.RootUri + "subdir";
                var response = await page.GoToAsync(url);

                Assert.True(response.Ok, "Got: " + response.StatusText + "from: "+url);
                Output.WriteLine("Loaded page");

                await MountTestComponentAsync<BinaryHttpRequestsComponent>(page);

                var targetUri = new Uri(_apiServerFixture.RootUri, "/subdir/api/data");
                await page.TypeAsync("#request-uri", targetUri.AbsoluteUri);
                await page.ClickAsync("#send-request");

                var status = await page.GetTextContentAsync("#response-status");
                var statusText = await page.GetTextContentAsync("#response-status-text");
                var testOutcome = await page.GetTextContentAsync("#test-outcome");

                Assert.Equal("OK", status);
                Assert.Equal("OK", statusText);
                Assert.Equal("", testOutcome);

                await page.CloseAsync();
            }
        }
    }
}
