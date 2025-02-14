// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using TestServer;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;
using OpenQA.Selenium;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class ErrorHandlingTest(BrowserFixture browserFixture, BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture, ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>(browserFixture, serverFixture, output)
{

    [Fact]
    public async Task RendersExceptionFromComponent()
    {
        GoTo("Throws?suppress-autostart=true");

        Browser.Equal("Error", () => Browser.Title);

        Assert.Collection(
            Browser.FindElements(By.CssSelector(".text-danger")),
            item => Assert.Equal("Error.", item.Text),
            item => Assert.Equal("An error occurred while processing your request.", item.Text));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
        Browser.Click(By.Id("call-blazor-start"));
        await Task.Delay(3000);
        Browser.Exists(By.Id("blazor-started"));
        Browser.Equal("False", () => Browser.FindElement(By.Id("is-interactive-server")).Text);
    }

    private void GoTo(string relativePath)
    {
        Navigate($"{ServerPathBase}/{relativePath}");
    }
}
