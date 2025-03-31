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

public class StatusCodePagesTest(BrowserFixture browserFixture, BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture, ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>(browserFixture, serverFixture, output)
{

    [Fact]
    public async Task StatusCodePagesWithReexecution()
    {
        Navigate($"{ServerPathBase}/set-not-found");

        Browser.Equal("Re-executed page", () => Browser.Title);
        var infoText = Browser.FindElement(By.Id("test-info")).Text;
        Assert.Contains("Welcome On Page Re-executed After Not Found Event", infoText);
    }
}
