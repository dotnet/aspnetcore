// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.ClientValidation;

// E2E coverage for the JS client-side validation pipeline against a Blazor SSR page
// with raw <form>/<input data-val=*> markup. The .NET-side rendering of data-val-*
// attributes via DataAnnotationsValidator/InputBase lives in a separate PR; these
// tests exercise the JS library's behaviour against markup that matches what the
// .NET side will emit once both PRs land.
public class ClientValidationBasicTest : ClientValidationTestBase
{
    public ClientValidationBasicTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        base.InitializeAsyncCore();
        NavigateToClientValidationPage("basic-validation");
        // Reset client-side state captured across test runs.
        ((IJavaScriptExecutor)Browser).ExecuteScript("localStorage.removeItem('lastValidation');");
    }

    [Fact]
    public void SubmittingEmptyFormShowsRequiredErrors()
    {
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("The Name field is required.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Name']")).Text);
        Browser.Equal("The Email field is required.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Email']")).Text);
        Browser.Equal("Password is required.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Password']")).Text);
    }

    [Fact]
    public void FillingRequiredFieldsAndSubmittingValidates()
    {
        Browser.Exists(By.Id("name")).SendKeys("Alice");
        Browser.Exists(By.Id("email")).SendKeys("alice@example.com");
        Browser.Exists(By.Id("password")).SendKeys("password123");
        Browser.Exists(By.Id("agree")).Click();
        new SelectElement(Browser.Exists(By.Id("category"))).SelectByValue("A");

        Browser.Exists(By.Id("submit")).Click();

        // localStorage value persists across the post-submit page reload that
        // happens when JS allows the submit through.
        Browser.Equal("valid:true;errors:0",
            () => (string)((IJavaScriptExecutor)Browser).ExecuteScript(
                "return localStorage.getItem('lastValidation');"));
    }

    [Fact]
    public void InvalidFieldGetsAriaInvalidAndAriaDescribedBy()
    {
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("true",
            () => Browser.Exists(By.Id("name")).GetAttribute("aria-invalid"));
        Browser.True(
            () => !string.IsNullOrEmpty(Browser.Exists(By.Id("name")).GetAttribute("aria-describedby")));
    }

    [Fact]
    public void ValidFieldHasNoAriaInvalid()
    {
        var name = Browser.Exists(By.Id("name"));
        name.SendKeys("Alice");
        name.SendKeys(Keys.Tab); // commit change

        Browser.True(() => Browser.Exists(By.Id("name")).GetAttribute("aria-invalid") is null);
    }

    [Fact]
    public void SummaryUpdatesAfterInvalidSubmit()
    {
        Browser.Exists(By.Id("submit")).Click();

        Browser.True(() =>
            Browser.FindElements(By.CssSelector("[data-valmsg-summary='true'] li")).Count >= 4);
    }

    [Fact]
    public void SummaryClearsAfterValidSubmit()
    {
        Browser.Exists(By.Id("submit")).Click();
        Browser.True(() =>
            Browser.FindElements(By.CssSelector("[data-valmsg-summary='true'] li")).Count > 0);

        Browser.Exists(By.Id("name")).SendKeys("Alice");
        Browser.Exists(By.Id("email")).SendKeys("alice@example.com");
        Browser.Exists(By.Id("password")).SendKeys("password123");
        Browser.Exists(By.Id("agree")).Click();
        new SelectElement(Browser.Exists(By.Id("category"))).SelectByValue("A");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("valid:true;errors:0",
            () => (string)((IJavaScriptExecutor)Browser).ExecuteScript(
                "return localStorage.getItem('lastValidation');"));
    }

    [Fact]
    public void ResetClearsValidationErrorsAndCssClasses()
    {
        Browser.Exists(By.Id("submit")).Click();
        Browser.Exists(By.CssSelector(".input-validation-error"));

        Browser.Exists(By.Id("reset")).Click();

        Browser.DoesNotExist(By.CssSelector(".input-validation-error"));
        Browser.DoesNotExist(By.CssSelector(".input-validation-valid"));
    }

    [Fact]
    public void NovalidateAttributeAddedToTrackedForm()
    {
        // Verified by the InitializeAsyncCore precondition; reasserted here for
        // explicit documentation of the expected behaviour.
        Browser.True(() => Browser.Exists(By.Id("test-form")).GetAttribute("novalidate") != null);
    }

    [Fact]
    public void ValidationCompleteEventDispatchedOnInvalidSubmit()
    {
        Browser.Exists(By.Id("submit")).Click();

        // Form is invalid so JS prevents default; page does not reload, event-log
        // div is observable directly. (localStorage is also written, but use the
        // div here as a separate signal.)
        Browser.Contains("valid:false", () => Browser.Exists(By.Id("event-log")).Text);
    }

    [Fact]
    public void CheckboxRequiredValidation_RejectsUnchecked()
    {
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("You must agree to the terms.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Agree']")).Text);
    }

    [Fact]
    public void SelectRequiredValidation_RejectsUnselected()
    {
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Please select a category.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Category']")).Text);
    }

    [Fact]
    public void MaxlengthShowsErrorWhenExceeded()
    {
        Browser.Exists(By.Id("bio")).SendKeys(new string('x', 101));
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Bio must be at most 100 characters.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Bio']")).Text);
    }

    [Fact]
    public void LengthMinMaxShowsErrorWhenTooShort()
    {
        Browser.Exists(By.Id("password")).SendKeys("short");
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Password must be between 8 and 50 characters.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Password']")).Text);
    }
}
