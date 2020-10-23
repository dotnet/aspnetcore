// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class DownloadAnchorTest
        : ServerTestBase<ToggleExecutionModeServerFixture<Program>>
    {
      

        public DownloadAnchorTest(
            BrowserFixture browserFixture,
            ToggleExecutionModeServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : this(browserFixture, serverFixture, output, ExecutionMode.Client)
        {
            
        }

        protected DownloadAnchorTest(
           BrowserFixture browserFixture,
           ToggleExecutionModeServerFixture<Program> serverFixture,
           ITestOutputHelper output,
           ExecutionMode executionMode)
           : base(browserFixture, serverFixture, output)
        {
            // Normally, the E2E tests use the Blazor dev server if they are testing
            // client-side execution. But for the download tests, we always have to run
            // in "hosted on ASP.NET Core" mode, in order to intercept file requests.
            switch (executionMode)
            {
                case ExecutionMode.Client:
                    serverFixture.UseAspNetHost(TestServer.Program.BuildWebHost<TestServer.HostedInAspNetStartup>);
                    break;
                default:
                    break;
            }
        }

        [Fact]
        public void DownloadFileFromAnchor()
        {
            // Arrange
            MountAndNavigateToRouterTest();
            var initialUrl = Browser.Url;

            // Act
            var anchor = Browser.Exists(By.CssSelector("a[download]"));
            anchor.Click();

            // URL should still be same as before click
            Assert.Equal(initialUrl, Browser.Url);

            // File should be requested
            var requestedPaths = GetAndClearRequestedPaths();
            Assert.NotEmpty(requestedPaths.Where(path => path.EndsWith("Download.txt", StringComparison.InvariantCultureIgnoreCase)));
        }

        protected IWebElement MountAndNavigateToRouterTest()
        {
            Navigate(ServerPathBase, noReload: true);
            var appElement = Browser.MountTestComponent<TestRouter>();
            return appElement;
        }


        private IReadOnlyCollection<string> GetAndClearRequestedPaths()
        {
            var requestLog = _serverFixture.Host.Services.GetRequiredService<TestServer.ResourceRequestLog>();
            var result = requestLog.RequestPaths.ToList();
            requestLog.Clear();
            return result;
        }


    }
}
