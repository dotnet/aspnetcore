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

public class TempDataSessionStorageTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    private const string SessionCookieName = ".AspNetCore.Session";
    private const string TempDataCookieName = ".AspNetCore.Components.TempData";

    public TempDataSessionStorageTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        _serverFixture.AdditionalArguments.Add("--UseSessionStorageTempDataProvider=true");
        Browser.Manage().Cookies.DeleteCookieNamed(SessionCookieName);
        base.InitializeAsyncCore();
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    public override async Task DisposeAsync()
    {
        var tempDataCookie = Browser.Manage().Cookies.GetCookieNamed(TempDataCookieName);
        var sessionCookie = Browser.Manage().Cookies.GetCookieNamed(".AspNetCore.Session");
        Assert.Null(tempDataCookie);
        Assert.NotNull(sessionCookie);

        await base.DisposeAsync();
    }

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

    [Fact]
    public void CanRemoveTheElementWithRemove()
    {
        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("No peeked value", () => Browser.FindElement(By.Id("peeked-value")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Peeked value", () => Browser.FindElement(By.Id("peeked-value")).Text);
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.Equal("Peeked value", () => Browser.FindElement(By.Id("peeked-value")).Text);
        Browser.FindElement(By.Id("delete-button")).Click();
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.Equal("No peeked value", () => Browser.FindElement(By.Id("peeked-value")).Text);
    }

    [Fact]
    public void CanCheckIfTempDataContainsKey()
    {
        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("False", () => Browser.FindElement(By.Id("contains-peeked-value")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("contains-message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("contains-peeked-value")).Text);
        Browser.Equal("True", () => Browser.FindElement(By.Id("contains-message")).Text);
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("True", () => Browser.FindElement(By.Id("contains-peeked-value")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("contains-message")).Text);
    }

    [Fact]
    public void TempDataPersistWithoutAccessing()
    {
        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-not-read")).Click();
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Fact]
    public void TempDataPreservesTypedArrays()
    {
        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("Wrong type: null", () => Browser.FindElement(By.Id("string-array")).Text);
        Browser.Equal("Wrong type: null", () => Browser.FindElement(By.Id("int-array")).Text);

        Browser.FindElement(By.Id("set-values-button")).Click();

        Browser.Equal("a,b,c", () => Browser.FindElement(By.Id("string-array")).Text);
        Browser.Equal("1,2,3", () => Browser.FindElement(By.Id("int-array")).Text);
    }

    [Fact]
    public void SupplyParameterFromTempDataReadsAndSavesValues()
    {
        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("", () => Browser.FindElement(By.Id("supply-parameter-from-tempdata")).Text);
        Browser.FindElement(By.Id("set-supply=from-tempdata")).Click();
        Browser.Equal("Supplied from TempData", () => Browser.FindElement(By.Id("supply-parameter-from-tempdata")).Text);

    }
}
