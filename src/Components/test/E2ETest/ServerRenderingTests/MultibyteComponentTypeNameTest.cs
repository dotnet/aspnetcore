// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class MultibyteComponentTypeNameTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public MultibyteComponentTypeNameTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Theory]
    [InlineData("server")]
    [InlineData("webassembly")]
    public void CanRenderInteractiveComponentsWithMultibyteName(string renderMode)
    {
        Navigate($"{ServerPathBase}/multibyte-character-component/{renderMode}");

        Browser.Equal("True", () => Browser.FindElement(By.ClassName("is-interactive")).Text);
        Browser.Equal("0", () => Browser.FindElement(By.ClassName("count")).Text);

        Browser.FindElement(By.ClassName("increment")).Click();

        Browser.Equal("1", () => Browser.FindElement(By.ClassName("count")).Text);
    }
}
