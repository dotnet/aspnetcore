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

    [Fact]
    public void SupplyParameterCanReadFromSession()
    {
        Navigate($"{ServerPathBase}/supply-parameter-from-session");
        Browser.Exists(By.Id("input-email")).SendKeys("email");
        Browser.Exists(By.Id("set-email")).Click();
        Browser.Equal("email", () => Browser.Exists(By.Id("text-email")).Text);
    }
}
