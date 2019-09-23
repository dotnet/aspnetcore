// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests
{
    /// <summary>
    /// Tests for various MapFallbackToClientSideBlazor overloads. We're just verifying that things render correctly.
    /// That means that routing and file serving is working for the startup pattern under test.
    /// </summary>
    public class ClientSideHostingTest :
        ServerTestBase<BasicTestAppServerSiteFixture<TestServer.StartupWithMapFallbackToClientSideBlazor>>
    {
        public ClientSideHostingTest(
            BrowserFixture browserFixture,
            BasicTestAppServerSiteFixture<TestServer.StartupWithMapFallbackToClientSideBlazor> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
        }

        [Fact]
        public void MapFallbackToClientSideBlazor_FilePath()
        {
            Navigate("/filepath");
            WaitUntilLoaded();
            Assert.NotNull(Browser.FindElement(By.Id("test-selector")));
        }

        [Fact]
        public void MapFallbackToClientSideBlazor_Pattern_FilePath()
        {
            Navigate("/pattern_filepath/test");
            WaitUntilLoaded();
            Assert.NotNull(Browser.FindElement(By.Id("test-selector")));
        }

        [Fact]
        public void MapFallbackToClientSideBlazor_AssemblyPath_FilePath()
        {
            Navigate("/assemblypath_filepath");
            WaitUntilLoaded();
            Assert.NotNull(Browser.FindElement(By.Id("test-selector")));
        }

        [Fact]
        public void MapFallbackToClientSideBlazor_AssemblyPath_Pattern_FilePath()
        {
            Navigate("/assemblypath_pattern_filepath/test");
            WaitUntilLoaded();
            Assert.NotNull(Browser.FindElement(By.Id("test-selector")));
        }


        private void WaitUntilLoaded()
        {
            new WebDriverWait(Browser, TimeSpan.FromSeconds(30)).Until(
                driver => driver.FindElement(By.TagName("app")).Text != "Loading...");
        }
    }
}
