// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.Tests;

public class SupplyParameterFromSessionAttributeTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    public SupplyParameterFromSessionAttributeTest(BrowserFixture browserFixture, BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture, ITestOutputHelper output) : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        _serverFixture.AdditionalArguments.Add("--UseSession=true");
        base.InitializeAsyncCore();
    }

    [Fact]
    public void SupplyParameterCanReadFromSession()
    {
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Exists(By.Id("input-email")).SendKeys("email");
        Browser.Exists(By.Id("set-email")).Click();
        Browser.Equal("email", () => Browser.Exists(By.Id("text-email")).Text);
    }

    [Fact]
    public void SupplyParameterNullWhenNoKeyInSession()
    {
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Exists(By.Id("input-email")).SendKeys("email");
        Browser.Exists(By.Id("set-email")).Click();
        Browser.Equal("email", () => Browser.Exists(By.Id("text-email")).Text);
        Browser.Equal("", () => Browser.Exists(By.Id("text-null")).Text);
    }

    [Fact]
    public void SupplyParameterWorksWhenDifferentName()
    {
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Exists(By.Id("input-another-email")).SendKeys("email");
        Browser.Exists(By.Id("set-another-email")).Click();
        Browser.Equal("email", () => Browser.Exists(By.Id("text-another-email")).Text);
        Browser.Equal("email", () => Browser.Exists(By.Id("text-email")).Text);
        Browser.Equal("", () => Browser.Exists(By.Id("text-null")).Text);
    }

    [Fact]
    public void SupplyParameterWorksWithRedirect()
    {
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Exists(By.Id("input-email-redirect")).SendKeys("email");
        Browser.Exists(By.Id("set-email-redirect")).Click();
        Browser.Equal("email", () => Browser.Exists(By.Id("text-email")).Text);
    }

    [Fact]
    public void SupplyParameterWorksWithComplexTypes()
    {
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Exists(By.Id("input-testclass-email")).SendKeys("email");
        Browser.Exists(By.Id("input-testclass-age")).SendKeys("30");
        Browser.Exists(By.Id("set-testclass-object")).Click();
        Browser.Equal("email", () => Browser.Exists(By.Id("text-testclass-email")).Text);
        Browser.Equal("30", () => Browser.Exists(By.Id("text-testclass-age")).Text);
    }

    [Fact]
    public void SupplyParameterPersistThroughNavigationAndBack()
    {
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Exists(By.Id("input-number")).SendKeys("12345");
        Browser.Exists(By.Id("set-number")).Click();
        Browser.Exists(By.Id("redirect")).Click();
        Browser.Equal("12345", () => Browser.Exists(By.Id("text-number")).Text);
    }
}
