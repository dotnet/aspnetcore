// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HostedInAspNet.Server;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    public class DownloadAnchorTest
        : ServerTestBase<AspNetSiteServerFixture>
    {
      

        public DownloadAnchorTest(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            serverFixture.BuildWebHostMethod = Program.BuildWebHost;
        }

        public override Task InitializeAsync()
        {
            return base.InitializeAsync(Guid.NewGuid().ToString());
        }

        [Fact]
        public void DownloadFileFromAnchor()
        {
            // Arrange
            Navigate("/");
            WaitUntilLoaded();
            GetAndClearRequestedPaths();

            // Act
            var anchor = Browser.Exists(By.CssSelector("a[download]"));
            anchor.Click();

            // URL should still be "/"
            Assert.Equal($"{_serverFixture.RootUri}", Browser.Url);

            // File should be requested
            var requestedPaths = GetAndClearRequestedPaths();
            Assert.NotEmpty(requestedPaths.Where(path => path.EndsWith("Download.txt", StringComparison.InvariantCultureIgnoreCase)));
        }


        private IReadOnlyCollection<string> GetAndClearRequestedPaths()
        {
            var requestLog = _serverFixture.Host.Services.GetRequiredService<BootResourceRequestLog>();
            var result = requestLog.RequestPaths.ToList();
            requestLog.Clear();
            return result;
        }

        private void WaitUntilLoaded()
        {
            var element = Browser.Exists(By.TagName("h1"));
            Browser.Equal("Hello, world!", () => element.Text);
        }
    }
}
