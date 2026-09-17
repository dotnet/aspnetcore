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

public class TempDataCookieTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>>>
{
    private const string TempDataCookieName = ".AspNetCore.Components.TempData";

    public TempDataCookieTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsNoInteractivityStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync() => InitializeAsync(BrowserFixture.StreamingContext);

    protected override void InitializeAsyncCore()
    {
        base.InitializeAsyncCore();
        Browser.Manage().Cookies.DeleteCookieNamed(TempDataCookieName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TempDataCanPersistThroughNavigation(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TempDataCanPersistThroughDifferentPages(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button-diff-page")).Click();
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TempDataPeekDoesntDelete(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.Equal("Peeked value", () => Browser.FindElement(By.Id("peeked-value")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TempDataKeepAllElements(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata?ValueToKeep=all");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Kept value", () => Browser.FindElement(By.Id("kept-value")).Text);
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("Kept value", () => Browser.FindElement(By.Id("kept-value")).Text);
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TempDataKeepOneElement(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata?ValueToKeep=KeptValue");

        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-button")).Click();
        Browser.Equal("Kept value", () => Browser.FindElement(By.Id("kept-value")).Text);
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.Equal("Kept value", () => Browser.FindElement(By.Id("kept-value")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanRemoveTheElementWithRemove(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanCheckIfTempDataContainsKey(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TempDataPersistWithoutAccessing(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
        Browser.FindElement(By.Id("set-values-not-read")).Click();
        Browser.FindElement(By.Id("redirect-button")).Click();
        Browser.Equal("Message", () => Browser.FindElement(By.Id("message")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TempDataPreservesTypedArrays(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata");

        Browser.Equal("Wrong type: null", () => Browser.FindElement(By.Id("string-array")).Text);
        Browser.Equal("Wrong type: null", () => Browser.FindElement(By.Id("int-array")).Text);

        Browser.FindElement(By.Id("set-values-button")).Click();

        Browser.Equal("a,b,c", () => Browser.FindElement(By.Id("string-array")).Text);
        Browser.Equal("1,2,3", () => Browser.FindElement(By.Id("int-array")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SupplyParameterFromTempDataReadsAndSavesValues(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("", () => Browser.FindElement(By.Id("supply-parameter-from-tempdata")).Text);
        Browser.FindElement(By.Id("set-supply-from-tempdata")).Click();
        Browser.Equal("Supplied from TempData", () => Browser.FindElement(By.Id("supply-parameter-from-tempdata")).Text);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SupplyParameterFromTempDataReadsAndSavesValuesFromEditForm(bool disableThrowNavigationException)
    {
        TestFeatureSwitches.SetDisableThrowNavigationException(disableThrowNavigationException);

        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("", () => Browser.FindElement(By.Id("supply-parameter-from-tempdata")).Text);
        Browser.Equal("False", () => Browser.FindElement(By.Id("navigation-manager-returned")).Text);

        Browser.FindElement(By.Id("set-supply-from-tempdata-edit-form")).Click();

        Browser.Equal("Supplied from TempData", () => Browser.FindElement(By.Id("supply-parameter-from-tempdata")).Text);

        // The handler writes this flag only on the line after NavigateTo. It is reached when navigation is
        // implemented by invoking the endpoint callback, and skipped when NavigateTo throws NavigationException,
        // so it confirms which control-flow mode actually produced the redirect.
        var expectedNavigationManagerReturned = disableThrowNavigationException ? "True" : "False";
        Browser.Equal(expectedNavigationManagerReturned, () => Browser.FindElement(By.Id("navigation-manager-returned")).Text);
    }

    [Fact]
    public void StreamingSSR_CookieTempData_DoesNotPersistValuesWrittenAfterFirstFlush()
    {
        Navigate($"{ServerPathBase}/streaming-cookie-tempdata-persistence");
        Browser.Exists(By.Id("streaming-complete"));

        Navigate($"{ServerPathBase}/tempdata");
        Browser.Equal("No message", () => Browser.FindElement(By.Id("message")).Text);
    }
}
