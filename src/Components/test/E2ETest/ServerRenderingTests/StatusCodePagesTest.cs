// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Components.TestServer.RazorComponents;
using Components.TestServer.RazorComponents.Pages.StreamingRendering;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class StatusCodePagesTest(BrowserFixture browserFixture, BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture, ITestOutputHelper output)
    : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>(browserFixture, serverFixture, output)
{

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public void StatusCodePagesWithReExecution(bool setNotFound, bool streaming)
    {
        string streamingPath = streaming ? "streaming-" : "";
        Navigate($"{ServerPathBase}/reexecution/{streamingPath}set-not-found?shouldSet={setNotFound}");

        string expectedTitle = setNotFound ? "Re-executed page" : "Original page";
        Browser.Equal(expectedTitle, () => Browser.Title);
        var infoText = Browser.FindElement(By.Id("test-info")).Text;
        // streaming when response started does not re-execute
        string expectedInfoText = streaming ? "Default Not Found Page" : setNotFound ? "Welcome On Page Re-executed After Not Found Event" : "Any content";
        Assert.Contains(expectedInfoText, infoText);
    }

}
