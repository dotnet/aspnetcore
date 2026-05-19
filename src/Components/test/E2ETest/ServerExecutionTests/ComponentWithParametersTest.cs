// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETest.ServerExecutionTests;

public class ComponentWithParametersTest : ServerTestBase<BasicTestAppServerSiteFixture<PrerenderedStartup>>
{
    public ComponentWithParametersTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<PrerenderedStartup> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void PassingParametersToComponentsFromThePageWorks()
    {
        Navigate("/prerendered/componentwithparameters?QueryValue=testQueryValue");

        BeginInteractivity();

        Browser.Exists(By.CssSelector(".interactive"));

        var parameter1 = Browser.Exists(By.CssSelector(".Param1"));
        Assert.Equal(100, parameter1.FindElements(By.CssSelector("li")).Count);
        Assert.Equal("99 99", parameter1.FindElement(By.CssSelector("li:last-child")).Text);

        // The assigned value is of a more derived type than the declared model type. This check
        // verifies we use the actual model type during round tripping.
        var parameter2 = Browser.Exists(By.CssSelector(".Param2"));
        Assert.Equal("Value Derived-Value", parameter2.Text);

        // This check verifies CaptureUnmatchedValues works
        var parameter3 = Browser.FindElements(By.CssSelector(".Param3 li"));
        Assert.Collection(
            parameter3,
            p => Assert.Equal("key1 testQueryValue", p.Text),
            p => Assert.Equal("key2 43", p.Text));
    }

    private void BeginInteractivity()
    {
        Browser.Exists(By.Id("load-boot-script")).Click();
    }
}
