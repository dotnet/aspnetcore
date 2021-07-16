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
    public class HeadModificationPrerenderingTest : ServerTestBase<BasicTestAppServerSiteFixture<DeferredComponentContentStartup>>
    {
        public HeadModificationPrerenderingTest(
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
            Browser.Equal("Title 1", () => Browser.Title);
            Browser.Exists(By.Id("meta-description"));

            BeginInteractivity();

            // Check that page medatada has not changed
            Browser.Equal("Title 1", () => Browser.Title);
            Browser.Exists(By.Id("meta-description"));

            var titleText1 = Browser.FindElement(By.Id("title-text-1"));
            titleText1.Clear();
            titleText1.SendKeys("Updated title 1\n");

            var descriptionText1 = Browser.FindElement(By.Id("description-text-1"));
            descriptionText1.Clear();
            descriptionText1.SendKeys("Updated description 1\n");

            // Check that head metadata can be changed after prerendering.
            Browser.Equal("Updated title 1", () => Browser.Title);
            Browser.Equal("Updated description 1", () => Browser.FindElement(By.Id("meta-description")).GetAttribute("content"));
        }

        private void BeginInteractivity()
        {
            Browser.Exists(By.Id("load-boot-script")).Click();
        }
    }
}
