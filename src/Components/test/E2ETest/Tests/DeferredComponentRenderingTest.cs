// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests
{
    public class DeferredComponentRenderingTest : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
        public DeferredComponentRenderingTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        protected override void InitializeAsyncCore()
        {
            Navigate(ServerPathBase, noReload: _serverFixture.ExecutionMode == ExecutionMode.Client);
        }

        [Fact]
        public void HeadContentsAreAppended()
        {
            Browser.MountTestComponent<HeadModification>();

            // Wait until the head has been dynamically modified
            Browser.Exists(By.Id("meta-description"));

            // Ensure that the static head contents are untouched.
            Browser.Exists(By.TagName("base"));
        }

        [Fact]
        public void MostRecentHeadContentTakesPriority()
        {
            Browser.MountTestComponent<HeadModification>();

            // Check that page medatada was rendered correctly
            Browser.Equal("Modified title!", () => Browser.Title);
            Browser.Equal("Modified description!", GetPageDescription);

            var selectElement = new SelectElement(Browser.FindElement(By.Id("select-dynamic-head-content")));
            selectElement.SelectByIndex(1);

            var inputTitle = Browser.FindElement(By.Id("input-title"));
            inputTitle.Clear();
            inputTitle.SendKeys("This title will apply later.\n");

            var inputDescription = Browser.FindElement(By.Id("input-description"));
            inputDescription.Clear();
            inputDescription.SendKeys("This description will apply later.\n");

            // Check that page medatada is overridden by the most recently attached head content
            Browser.Equal("Overridden title 1", () => Browser.Title);
            Browser.DoesNotExist(By.Id("meta-description"));

            selectElement.SelectByIndex(0);

            // Check that disposing the most recent head content falls back on the next most recent content
            Browser.Equal("This title will apply later.", () => Browser.Title);
            Browser.Equal("This description will apply later.", GetPageDescription);

            string GetPageDescription()
                => Browser.FindElement(By.Id("meta-description"))?.GetAttribute("content");
        }
    }
}
