// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests
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

        private void BeginInteractivity()
        {
            Browser.FindElement(By.Id("load-boot-script")).Click();
        }
    }
}
