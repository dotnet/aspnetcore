// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using BasicTestApp;
using BasicTestApp.RouterTest;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Blazor.E2ETest.Tests
{
    public class RoutingTest : BasicTestAppTestBase, IDisposable
    {
        private readonly ServerFixture _server;

        public RoutingTest(
            BrowserFixture browserFixture, 
            DevHostServerFixture<Program> serverFixture,
            ITestOutputHelper output)
            : base(browserFixture, serverFixture, output)
        {
            _server = serverFixture;
            Navigate(ServerPathBase, noReload: true);
            WaitUntilDotNetRunningInBrowser();
        }

        [Fact]
        public void CanArriveAtDefaultPage()
        {
            SetUrlViaPushState($"{ServerPathBase}/");

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
        }

        [Fact]
        public void CanArriveAtDefaultPageWithoutTrailingSlash()
        {
            // This is a bit of a degenerate case because ideally devs would configure their
            // servers to enforce a canonical URL (with trailing slash) for the homepage.
            // But in case they don't want to, we need to handle it the same as if the URL does
            // have a trailing slash.
            SetUrlViaPushState($"{ServerPathBase}");

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
        }

        [Fact]
        public void CanArriveAtPageWithParameters()
        {
            SetUrlViaPushState($"{ServerPathBase}/WithParameters/Name/Ghi/LastName/O'Jkl");

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("Your full name is Ghi O'Jkl.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks();
        }

        [Fact]
        public void CanArriveAtNonDefaultPage()
        {
            SetUrlViaPushState($"{ServerPathBase}/Other");

            var app = MountTestComponent<TestRouter>();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
        }

        [Fact]
        public void CanFollowLinkToOtherPage()
        {
            SetUrlViaPushState($"{ServerPathBase}/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithBaseRelativeUrl()
        {
            SetUrlViaPushState($"{ServerPathBase}/");            

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with base-relative URL (matches all)")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
        }

        [Fact]
        public void CanFollowLinkToEmptyStringHrefAsBaseRelativeUrl()
        {
            SetUrlViaPushState($"{ServerPathBase}/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with base-relative URL (matches all)")).Click();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
        }

        [Fact]
        public void CanFollowLinkToPageWithParameters()
        {
            SetUrlViaPushState($"{ServerPathBase}/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("With parameters")).Click();
            Assert.Equal("Your full name is Abc McDef.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("With parameters");
        }

        [Fact]
        public void CanFollowLinkToDefaultPage()
        {
            SetUrlViaPushState($"{ServerPathBase}/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default (matches all)")).Click();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default (matches all)", "Default with base-relative URL (matches all)");
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithQueryString()
        {
            SetUrlViaPushState($"{ServerPathBase}/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with query")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with query");
        }

        [Fact]
        public void CanFollowLinkToDefaultPageWithQueryString()
        {
            SetUrlViaPushState($"{ServerPathBase}/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with query")).Click();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default with query");
        }

        [Fact]
        public void CanFollowLinkToOtherPageWithHash()
        {
            SetUrlViaPushState($"{ServerPathBase}/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Other with hash")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with hash");
        }

        [Fact]
        public void CanFollowLinkToDefaultPageWithHash()
        {
            SetUrlViaPushState($"{ServerPathBase}/Other");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.LinkText("Default with hash")).Click();
            Assert.Equal("This is the default page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Default with hash");
        }

        [Fact]
        public void CanNavigateProgrammatically()
        {
            SetUrlViaPushState($"{ServerPathBase}/");

            var app = MountTestComponent<TestRouter>();
            app.FindElement(By.TagName("button")).Click();
            Assert.Equal("This is another page.", app.FindElement(By.Id("test-info")).Text);
            AssertHighlightedLinks("Other", "Other with base-relative URL (matches all)");
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
            jsExecutor.ExecuteScript($"Blazor.navigateTo('{absoluteUri.ToString().Replace("'", "\\'")}')");
        }

        private void AssertHighlightedLinks(params string[] linkTexts)
        {
            var actual = Browser.FindElements(By.CssSelector("a.active"));
            var actualTexts = actual.Select(x => x.Text);
            Assert.Equal(linkTexts, actualTexts);
        }
    }
}
