// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
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
            : base(browserFixture, serverFixture.WithServerExecution(), output)
        {            
        }

        [Fact]
        [QuarantinedTest("https://github.com/dotnet/aspnetcore/issues/29739")]
        public void DownloadFileFromAnchor()
        {
            // Arrange
            MountAndNavigateToRouterTest();
            GetAndClearRequestedPaths();
            var initialUrl = Browser.Url;

            // Act
            var anchor = Browser.Exists(By.CssSelector("a[download]"));
            anchor.Click();

            // URL should still be same as before click
            Assert.Equal(initialUrl, Browser.Url);

            // File should be requested
            var requestedPaths = GetAndClearRequestedPaths();
            Assert.NotEmpty(requestedPaths.Where(path => path.EndsWith("blazor_logo_1000x.png", StringComparison.InvariantCultureIgnoreCase)));
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
