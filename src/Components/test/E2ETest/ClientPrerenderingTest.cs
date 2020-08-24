// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using OpenQA.Selenium;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests
{
    [Collection("auth")] // Because auth uses cookies, this can't run in parallel with other auth tests
    public class ClientPrerenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<PrerenderedStartup>>
    {
        public ClientPrerenderingTest(
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
        public void PrerenderingWaitsForAsyncDisposableComponents()
        {
            Navigate("/prerendered/prerendered-async-disposal");

            // Prerendered output shows "not connected"
            Browser.Equal("After async disposal", () => Browser.FindElement(By.Id("disposal-message")).Text);
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
