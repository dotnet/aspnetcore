// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

public class LocalizationDisabledTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public LocalizationDisabledTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        _serverFixture.AdditionalArguments.Add("EnableCultureTesting=true");
        _serverFixture.AdditionalArguments.Add("EnforceServerCultureOnClient=false");
        base.InitializeAsyncCore();
    }

    [Fact]
    public void DoesNotPersistCultureFromServerWhenDisabled()
    {
        Navigate($"{ServerPathBase}/Culture/SetCulture?culture=fr-FR&redirectUri={Uri.EscapeDataString($"{ServerPathBase}/persist-culture-state?suppress-autostart")}");
        Browser.Exists(By.ClassName("return-from-culture-setter")).Click();

        Browser.Equal("Prerender", () => Browser.FindElement(By.Id("prerender")).Text);
        Browser.Equal("fr-FR", () => Browser.FindElement(By.Id("culture-set")).Text);
        Browser.Equal("fr-FR", () => Browser.FindElement(By.Id("culture-ui-set")).Text);

        Browser.Click(By.Id("call-blazor-start"));

        Browser.Equal("Interactive", () => Browser.FindElement(By.Id("interactive")).Text);
        Browser.Equal("en-US", () => Browser.FindElement(By.Id("culture-set")).Text);
        Browser.Equal("en-US", () => Browser.FindElement(By.Id("culture-ui-set")).Text);
    }
}
