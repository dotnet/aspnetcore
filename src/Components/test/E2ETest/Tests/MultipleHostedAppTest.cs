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
    public class MultipleHostedAppTest: ServerTestBase<AspNetSiteServerFixture>
    {
        public MultipleHostedAppTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            serverFixture.AdditionalArguments.AddRange(new[] { "--MapAllApps", "true" });
            serverFixture.BuildWebHostMethod = HostedInAspNet.Server.Program.BuildWebHost;
            serverFixture.Environment = AspNetEnvironment.Development;
        }

        protected override void InitializeAsyncCore()
        {
            Navigate("/", noReload: true);
            WaitUntilLoaded();
        }

        [Fact]
        public void CanLoadBlazorAppFromSubPath()
        {
            Navigate("/app/");
            WaitUntilLoaded();
            Assert.Equal("App loaded on custom path", Browser.Title);
            Assert.Equal(0, Browser.GetBrowserLogs(LogLevel.Severe).Count);
        }

        [Fact]
        public void HasTitle()
        {
            Assert.Equal("Sample Blazor app", Browser.Title);
        }

        [Fact]
        public void ServesStaticAssetsFromClientAppWebRoot()
        {
            var javascriptExecutor = (IJavaScriptExecutor)Browser;
            var bootstrapTooltipType = javascriptExecutor
                .ExecuteScript("return window.customJsWasLoaded;");
            Assert.True((bool)bootstrapTooltipType);
        }

        private void WaitUntilLoaded()
        {
            var app = Browser.Exists(By.TagName("app"));
            Browser.NotEqual("Loading...", () => app.Text);
        }
    }
}
