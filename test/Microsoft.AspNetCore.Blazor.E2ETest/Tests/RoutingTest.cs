// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using Xunit;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class RoutingTest : BasicTestAppTestBase, IDisposable
    {
        private readonly ServerFixture _server;

        public RoutingTest(BrowserFixture browserFixture, DevHostServerFixture<Program> serverFixture)
            : base(browserFixture, serverFixture)
        {
            _server = serverFixture;
            Navigate(ServerPathBase, noReload: true);
            WaitUntilDotNetRunningInBrowser();
        }

        [Fact]
        public void CanArriveAtDefaultPage()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/");

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanArriveAtPageWithParameters()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/WithParameters/Name/Dan/LastName/Roth");

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("Your full name is Dan Roth.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanArriveAtNonDefaultPage()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/Other");

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToOtherPage()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithBaseRelativeUrl()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/");            

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with base-relative URL")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToPageWithParameters()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("With parameters")).Click();
            Assert.Equal("Your full name is Steve Sanderson.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToDefaultPage()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default")).Click();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithQueryString()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with query")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToDefaultPageWithQueryString()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with query")).Click();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithHash()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with hash")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanFollowLinkToDefaultPageWithHash()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with hash")).Click();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
        }

        [Fact]
        public void CanNavigateProgrammatically()
        {
            SetUrlViaPushState($"{ServerPathBase}/RouterTest/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.TagName("button")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
        }

        public void Dispose()
        {
            // Clear any existing state
            SetUrlViaPushState(ServerPathBase);
            MountTestComponent<TextOnlyComponent>();
        }

        private void SetUrlViaPushState(string relativeUri)
        {
            var jsExecutor = (IJavaScriptExecutor)Browser;
            var absoluteUri = new Uri(_server.RootUri, relativeUri);
            jsExecutor.ExecuteScript($"Blazor.navigateTo('{absoluteUri.ToString()}')");
        }
    }
}
