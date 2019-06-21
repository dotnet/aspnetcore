// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerExecutionTests
{
    public class ConnectionQualityTests : ServerTestBase<AspNetSiteServerFixture>
    {
        public ConnectionQualityTests(
            BrowserFixture browserFixture,
            AspNetSiteServerFixture serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            serverFixture.Environment = AspNetEnvironment.Development;
            serverFixture.BuildWebHostMethod = TestServer.Program.BuildWebHost;
        }

        [Fact]
        public void RefreshingThePageMultipleTimesCreatesNewCircuitsWithoutIssues()
        {
            Navigate("/connectionqualityhost");

            var previousIdentifier = CaptureScopeIdentifier();
            for (int i = 0; i < 50; i++)
            {
                Browser.Equal(i + 3, () => Browser.Manage().Cookies.AllCookies.Where(c => c.Name.StartsWith("Circuit.")).Count());
                var newIdentifier = CaptureScopeIdentifier();
                Assert.NotEqual(previousIdentifier, newIdentifier);
                previousIdentifier = newIdentifier;
            }
        }

        [Fact]
        public void CannotConnectWithoutACookie()
        {
            Navigate("/connectionqualityhost");

            Browser.Equal("Disconnected", () => Browser.FindElement(By.Id("connection-status")).Text);
            var prerenderedScope = Browser.FindElement(By.Id("connection-identifier")).Text;
            Browser.Manage().Cookies.DeleteAllCookies();

            Browser.FindElement(By.Id("load-boot-script")).Click();
            Browser.Equal("Disconnected", () => Browser.FindElement(By.Id("connection-status")).Text);

            Browser.True(() => ((IJavaScriptExecutor)Browser)
                .ExecuteScript("return window['__aspnetcore__testing__blazor__failed__'];") == null ? false : true);
        }

        [Fact]
        public void CannotConnectWithInvalidCookie()
        {
            Navigate("/connectionqualityhost");

            Browser.Equal("Disconnected", () => Browser.FindElement(By.Id("connection-status")).Text);
            var prerenderedScope = Browser.FindElement(By.Id("connection-identifier")).Text;
            var circuitIdCookie = Browser.Manage().Cookies.AllCookies.Single(c => c.Name.StartsWith("Circuit.") && !c.Name.EndsWith("KeepAlive"));
            Browser.Manage().Cookies.DeleteCookieNamed(circuitIdCookie.Name);
            Browser.Manage().Cookies.AddCookie(new Cookie(circuitIdCookie.Name, circuitIdCookie.Value[0..^5]));

            Browser.FindElement(By.Id("load-boot-script")).Click();
            Browser.Equal("Disconnected", () => Browser.FindElement(By.Id("connection-status")).Text);

            Browser.True(() => ((IJavaScriptExecutor)Browser)
                .ExecuteScript("return window['__aspnetcore__testing__blazor__failed__'];") == null ? false : true);
        }

        [Fact]
        public void CannotConnectWithSwappedCookie()
        {
            Navigate("/connectionqualityhost");

            Browser.Equal("Disconnected", () => Browser.FindElement(By.Id("connection-status")).Text);
            var prerenderedScope = Browser.FindElement(By.Id("connection-identifier")).Text;
            var oldCookie = Browser.Manage().Cookies.AllCookies.Single(c => c.Name.StartsWith("Circuit.") && !c.Name.EndsWith("KeepAlive"));
            BeginInteractivity();
            Browser.FindElement(By.Id("refresh-page")).Click();

            var newCookie = Browser.Manage().Cookies.AllCookies.Single(c => c.Name != oldCookie.Name && c.Name.StartsWith("Circuit.") && !c.Name.EndsWith("KeepAlive"));
            Browser.Manage().Cookies.DeleteCookieNamed(oldCookie.Name);
            Browser.Manage().Cookies.DeleteCookieNamed(newCookie.Name);
            Browser.Manage().Cookies.AddCookie(new Cookie(newCookie.Name, oldCookie.Value));

            Browser.FindElement(By.Id("load-boot-script")).Click();
            Browser.Equal("Disconnected", () => Browser.FindElement(By.Id("connection-status")).Text);

            Browser.True(() => ((IJavaScriptExecutor)Browser)
                .ExecuteScript("return window['__aspnetcore__testing__blazor__failed__'];") == null ? false : true);
        }

        private string CaptureScopeIdentifier()
        {
            Browser.Equal("Disconnected", () => Browser.FindElement(By.Id("connection-status")).Text);
            var prerenderedScope = Browser.FindElement(By.Id("connection-identifier")).Text;
            var keepAliveCookie = Browser.HasCookie((c) => c.Name.EndsWith("KeepAlive"));
            BeginInteractivity();
            Browser.DoesNotHaveCookie(keepAliveCookie.Name);

            Browser.Equal("Connected", () => Browser.FindElement(By.Id("connection-status")).Text);
            Browser.Equal(prerenderedScope, () => Browser.FindElement(By.Id("connection-identifier")).Text);
            Browser.FindElement(By.Id("refresh-page")).Click();

            return prerenderedScope;
        }

        private void BeginInteractivity()
        {
            Browser.FindElement(By.Id("load-boot-script")).Click();
            Browser.True(() => ((IJavaScriptExecutor)Browser)
                .ExecuteScript("return window['__aspnetcore__testing__blazor__started__'];") == null ? false : true);
        }
    }
}
