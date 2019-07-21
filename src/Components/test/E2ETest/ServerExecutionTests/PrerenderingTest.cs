// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    public class PrerenderingTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public PrerenderingTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            _serverFixture.Environment = AspNetEnvironment.Development;
            _serverFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
        }

        [Fact]
        public void CanTransitionFromPrerenderedToInteractiveMode()
        {
            Navigate("/prerendered/prerendered-transition");

            // Prerendered output shows "not connected"
            Browser.Equal("not connected", () => Browser.FindElement(By.Id("connected-state")).Text);

            // Once connected, output changes
            BeginInteractivity();
            Browser.Equal("connected", () => Browser.FindElement(By.Id("connected-state")).Text);

            // ... and now the counter works
            Browser.FindElement(By.Id("increment-count")).Click();
            Browser.Equal("1", () => Browser.FindElement(By.Id("count")).Text);
        }

        [Fact]
        public void CanUseJSInteropFromOnAfterRenderAsync()
        {
            Navigate("/prerendered/prerendered-interop");

            // Prerendered output can't use JSInterop
            Browser.Equal("No value yet", () => Browser.FindElement(By.Id("val-get-by-interop")).Text);
            Browser.Equal(string.Empty, () => Browser.FindElement(By.Id("val-set-by-interop")).GetAttribute("value"));

            BeginInteractivity();

            // Once connected, we can
            Browser.Equal("Hello from interop call", () => Browser.FindElement(By.Id("val-get-by-interop")).Text);
            Browser.Equal("Hello from interop call", () => Browser.FindElement(By.Id("val-set-by-interop")).GetAttribute("value"));
        }

        [Fact]
        public void CanReadUrlHashOnlyOnceConnected()
        {
            var urlWithoutHash = "prerendered/show-uri?my=query&another=value";
            var url = $"{urlWithoutHash}#some/hash?tokens";

            // The server doesn't receive the hash part of the URL, so you can't
            // read it during prerendering
            Navigate(url);
            Browser.Equal(
                _serverFixture.RootUri + urlWithoutHash,
                () => Browser.FindElement(By.TagName("strong")).Text);

            // Once connected, you do have access to the full URL
            BeginInteractivity();
            Browser.Equal(
                _serverFixture.RootUri + url,
                () => Browser.FindElement(By.TagName("strong")).Text);
        }

        [Fact]
        public async Task CanRedirectDuringPrerendering()
        {
            var targetUri = new Uri(_serverFixture.RootUri, "prerendered/prerendered-redirection?destination=prerendered-transition");
            var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            var response = await httpClient.GetAsync(targetUri);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("prerendered-transition", response.Headers.Location.ToString());
        }

        private void BeginInteractivity()
        {
            Browser.FindElement(By.Id("load-boot-script")).Click();
        }
    }
}
