// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    [Collection("auth")] // Because auth uses cookies, this can't run in parallel with other auth tests
    public class PrerenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<PrerenderedStartup>>
    {
        public PrerenderingTest(
            BrowserFixture browserFixture,
            BasicTestAppServerSiteFixture<PrerenderedStartup> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
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
        public void IsCompatibleWithLazyLoadWebAssembly()
        {
            Navigate("/prerendered/WithLazyAssembly");

            var button = Browser.FindElement(By.Id("use-package-button"));

            button.Click();

            AssertLogDoesNotContainCriticalMessages("Could not load file or assembly 'Newtonsoft.Json");
        }

        [Fact]
        public void CanInfluenceHeadDuringPrerender()
        {
            Navigate("/prerendered/prerendered-head");

            var metaWithBindings = Browser.FindElement(By.Id("meta-with-bindings"));
            var metaNoBindings = Browser.FindElement(By.Id("meta-no-bindings"));

            // Validate updated head during prerender
            Browser.Equal("Initial title", () => Browser.Title);
            Browser.Equal("Initial meta content", () => metaWithBindings.GetAttribute("content"));
            Browser.Equal("Immutable meta content", () => metaNoBindings.GetAttribute("content"));

            BeginInteractivity();

            // Wait for elements to be recreated with internal ids to permit mutation
            WaitForNewElement(ref metaWithBindings, "meta-with-bindings");
            WaitForNewElement(ref metaNoBindings, "meta-no-bindings");

            // Validate updated head after prerender
            Browser.Equal("Initial title", () => Browser.Title);
            Browser.Equal("Initial meta content", () => metaWithBindings.GetAttribute("content"));
            Browser.Equal("Immutable meta content", () => metaNoBindings.GetAttribute("content"));

            // Change parameter of meta component
            var inputMetaBinding = Browser.FindElement(By.Id("input-meta-binding"));
            inputMetaBinding.Clear();
            inputMetaBinding.SendKeys("Updated meta content\n");

            // Wait for meta tag to be recreated with new attributes
            WaitForNewElement(ref metaWithBindings, "meta-with-bindings");

            // Validate new meta content attribute
            Browser.Equal("Updated meta content", () => metaWithBindings.GetAttribute("content"));

            void WaitForNewElement(ref IWebElement existingElement, string id)
            {
                var newElement = existingElement;

                Browser.NotEqual(existingElement, () => newElement = Browser.FindElement(By.Id(id)) ?? newElement);

                existingElement = newElement;
            }
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

        [Theory]
        [InlineData("base/relative", "prerendered/base/relative")]
        [InlineData("/root/relative", "/root/relative")]
        [InlineData("http://absolute/url", "http://absolute/url")]
        public async Task CanRedirectDuringPrerendering(string destinationParam, string expectedRedirectionLocation)
        {
            var requestUri = new Uri(
                _serverFixture.RootUri,
                "prerendered/prerendered-redirection?destination=" + destinationParam);

            var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false });
            var response = await httpClient.GetAsync(requestUri);

            var expectedUri = new Uri(_serverFixture.RootUri, expectedRedirectionLocation);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal(expectedUri, response.Headers.Location);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData(null, "Bert")]
        [InlineData("Bert", null)]
        [InlineData("Bert", "Treb")]
        public void CanAccessAuthenticationStateDuringStaticPrerendering(string initialUsername, string interactiveUsername)
        {
            // See that the authentication state is usable during the initial prerendering
            SignInAs(initialUsername, null);
            Navigate("/prerendered/prerendered-transition");
            Browser.Equal($"Hello, {initialUsername ?? "anonymous"}!", () => Browser.FindElement(By.TagName("h1")).Text);

            // See that during connection, we update to whatever the latest authentication state now is
            SignInAs(interactiveUsername, null, useSeparateTab: true);
            BeginInteractivity();
            Browser.Equal($"Hello, {interactiveUsername ?? "anonymous"}!", () => Browser.FindElement(By.TagName("h1")).Text);
        }

        private void BeginInteractivity()
        {
            Browser.FindElement(By.Id("load-boot-script")).Click();
        }

        private void AssertLogDoesNotContainCriticalMessages(params string[] messages)
        {
            var log = Browser.Manage().Logs.GetLog(LogType.Browser);
            foreach (var message in messages)
            {
                Assert.DoesNotContain(log, entry =>
                {
                    return entry.Level == LogLevel.Severe
                    && entry.Message.Contains(message);
                });
            }
        }

        private void SignInAs(string userName, string roles, bool useSeparateTab = false) =>
            Browser.SignInAs(new Uri(_serverFixture.RootUri, "/prerendered/"), userName, roles, useSeparateTab);
    }
}
