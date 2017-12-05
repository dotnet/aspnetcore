// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Blazor.E2ETest.Infrastructure;
using OpenQA.Selenium;
using Xunit;

namespace Blazor.E2ETest.Tests
{
    public class HelloWorldTest : StaticSiteTestBase
    {
        public HelloWorldTest(BrowserFixture browserFixture, StaticServerFixture serverFixture)
            : base(browserFixture, serverFixture, "HelloWorld")
        {
        }

        [Fact]
        public void HasTitle()
        {
            Navigate("/");
            Assert.Equal("Hello", Browser.Title);
        }

        [Fact]
        public void DisplaysMessage()
        {
            Navigate("/");
            Assert.Equal("Hello, world!", Browser.FindElement(By.TagName("h1")).Text);
        }
    }
}
