// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using Microsoft.AspNetCore.InternalTesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.FormHandlingTests;

public class FormWithNoBackForwardCacheTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public FormWithNoBackForwardCacheTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
    {
        return InitializeAsync(BrowserFixture.StreamingBackForwardCacheContext);
    }

    private void SuppressEnhancedNavigation(bool shouldSuppress)
        => EnhancedNavigationTestUtil.SuppressEnhancedNavigation(this, shouldSuppress);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CanUseFormWithMethodGet(bool suppressEnhancedNavigation)
    {
        SuppressEnhancedNavigation(suppressEnhancedNavigation);
        Navigate($"{ServerPathBase}/forms/method-get");
        Browser.Equal("Form with method=get", () => Browser.FindElement(By.TagName("h2")).Text);

        // Validate initial state
        var stringInput = Browser.FindElement(By.Id("mystring"));
        var boolInput = Browser.FindElement(By.Id("mybool"));
        Browser.Equal("Initial value", () => stringInput.GetDomProperty("value"));
        Browser.Equal("False", () => boolInput.GetDomProperty("checked"));

        // Edit and submit the form; check it worked
        stringInput.Clear();
        stringInput.SendKeys("Edited value");
        boolInput.Click();
        Browser.FindElement(By.Id("submit-get-form")).Click();
        AssertUiState("Edited value", true);
        Browser.Contains($"MyString=Edited+value", () => Browser.Url);
        Browser.Contains($"MyBool=True", () => Browser.Url);

        // Check 'back' correctly gets us to the previous state
        Browser.Navigate().Back();
        AssertUiState("Initial value", false);
        Browser.False(() => Browser.Url.Contains("MyString"));
        Browser.False(() => Browser.Url.Contains("MyBool"));

        // Check 'forward' correctly recreates the edited state
        Browser.Navigate().Forward();
        AssertUiState("Edited value", true);
        Browser.Contains($"MyString=Edited+value", () => Browser.Url);
        Browser.Contains($"MyBool=True", () => Browser.Url);

        void AssertUiState(string expectedStringValue, bool expectedBoolValue)
        {
            Browser.Equal(expectedStringValue, () => Browser.FindElement(By.Id("mystring-value")).Text);
            Browser.Equal(expectedBoolValue.ToString(), () => Browser.FindElement(By.Id("mybool-value")).Text);

            // If we're not suppressing, we'll keep referencing the same elements to show they were preserved
            if (suppressEnhancedNavigation)
            {
                stringInput = Browser.FindElement(By.Id("mystring"));
                boolInput = Browser.FindElement(By.Id("mybool"));
            }

            Browser.Equal(expectedStringValue, () => stringInput.GetDomProperty("value"));
            Browser.Equal(expectedBoolValue.ToString(), () => boolInput.GetDomProperty("checked"));
        }
    }
}

