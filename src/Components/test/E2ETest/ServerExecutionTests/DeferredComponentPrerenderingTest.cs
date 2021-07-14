// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests
{
    [Collection("auth")] // Because auth uses cookies, this can't run in parallel with other auth tests
    public class DeferredComponentPrerenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<DeferredComponentContentStartup>>
    {
        public DeferredComponentPrerenderingTest(
            BrowserFixture browserFixture,
            BasicTestAppServerSiteFixture<DeferredComponentContentStartup> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        [Fact]
        public void CanModifyHeadDuringAndAfterPrerendering()
        {
            Navigate("/deferred-component-content");

            // Check that page medatada was rendered correctly
            Browser.Equal("Modified title!", () => Browser.Title);
            Browser.Exists(By.Id("meta-description"));

            BeginInteractivity();

            // Check that page medatada has not changed
            Browser.Equal("Modified title!", () => Browser.Title);
            Browser.Exists(By.Id("meta-description"));

            var inputTitle = Browser.FindElement(By.Id("input-title"));
            inputTitle.Clear();
            inputTitle.SendKeys("New title.\n");

            var inputDescription = Browser.FindElement(By.Id("input-description"));
            inputDescription.Clear();
            inputDescription.SendKeys("New description.\n");

            // Check that head metadata can be changed after prerendering.
            Browser.Equal("New title.", () => Browser.Title);
            Browser.Equal("New description.", () => Browser.FindElement(By.Id("meta-description")).GetAttribute("content"));
        }

        private void BeginInteractivity()
        {
            Browser.Exists(By.Id("load-boot-script")).Click();
        }
    }
}
