// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Blazor.E2ETest.Infrastructure;
using Microsoft.Blazor.E2ETest.Infrastructure.ServerFixtures;
using OpenQA.Selenium;
using Xunit;

namespace Microsoft.Blazor.E2ETest.Tests
{
    public class HelloWorldTest : ServerTestBase<StaticSiteServerFixture>
    {
        public HelloWorldTest(BrowserFixture browserFixture, StaticSiteServerFixture serverFixture)
            : base(browserFixture, serverFixture)
        {
            serverFixture.SampleSiteName = "HelloWorld";
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
