// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class HostedInAlternativeBasePathTest : ServerTestBase<AspNetSiteServerFixture>
    {
        public HostedInAlternativeBasePathTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            serverFixture.AdditionalArguments.AddRange(new[] { "--UseAlternativeBasePath", "true" });
            serverFixture.BuildWebHostMethod = HostedInAspNet.Server.Program.BuildWebHost;
            serverFixture.Environment = AspNetEnvironment.Development;
        }

        protected override void InitializeAsyncCore()
        {
            Navigate("/app/", noReload: true);
            WaitUntilLoaded();
        }

        [Fact]
        public void CanLoadBlazorAppFromSubPath()
        {
            Assert.Equal("App loaded on custom path", Browser.Title);
            Assert.Equal(0, Browser.GetBrowserLogs(LogLevel.Severe).Count);
        }

        private void WaitUntilLoaded()
        {
            var app = Browser.Exists(By.TagName("app"));
            Browser.NotEqual("Loading...", () => app.Text);
        }
    }
}
