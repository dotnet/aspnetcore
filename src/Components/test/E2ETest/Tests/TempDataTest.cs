// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.E2ETesting;
using Xunit.Abstractions;
using OpenQA.Selenium;
using TestServer;

namespace Microsoft.AspNetCore.Components.E2ETest.Tests;

public class TempDataTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    public TempDataTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    [Fact]
    public void TempDataCanPersistThroughNavigation()
    {
        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Fact]
    public void TempDataCanPersistThroughDifferentPages()
    {
        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button-diff-page")).Click();
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Fact]
    public void TempDataPeekDoesntDelete()
    {
        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.Equal("Peeked value", () => Browser.FindElement(By.Id("peeked-value")).Text);
    }

    [Fact]
    public void TempDataKeepAllElements()
    {
        Navigate($"{ServerPathBase}/tempdata?ValueToKeep=all");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Kept value", () => Browser.FindElement(By.Id("kept-value")).Text);
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("Kept value", () => Browser.FindElement(By.Id("kept-value")).Text);
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Fact]
    public void TempDataKeepOneElement()
    {
        Navigate($"{ServerPathBase}/tempdata?ValueToKeep=KeptValue");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Kept value", () => Browser.FindElement(By.Id("kept-value")).Text);
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.Equal("Kept value", () => Browser.FindElement(By.Id("kept-value")).Text);
    }
}
