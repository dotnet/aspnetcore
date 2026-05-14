// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.ClientValidation;

// E2E coverage for advanced JS validation scenarios: trigger overrides, skipped
// elements, multi-form independence, dynamic content, and untracked forms.
public class ClientValidationScenariosTest : ClientValidationTestBase
{
    public ClientValidationScenariosTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void DataValeventSubmit_NoValidationOnChangeOrInput()
    {
        NavigateToClientValidationPage("timing");

        var field = Browser.Exists(By.Id("submit-only"));
        field.Click();
        field.SendKeys(Keys.Tab); // blur

        // Should NOT have triggered validation (per data-valevent="submit")
        Browser.Equal("",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='SubmitOnly']")).Text);

        // Submit triggers it
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("SubmitOnly is required.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='SubmitOnly']")).Text);
    }

    [Fact]
    public void DataValeventInput_ValidatesEagerlyOnPristineForm()
    {
        NavigateToClientValidationPage("timing");

        var field = Browser.Exists(By.Id("input-eager"));
        // Type a character to trigger 'input' event, then clear it via backspace
        field.SendKeys("x");
        field.SendKeys(Keys.Backspace);

        // After clearing (back to empty), should be invalid
        Browser.Equal("InputEager is required.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='InputEager']")).Text);
    }

    [Fact]
    public void HiddenDisabledAndDisplayNoneFieldsAreSkipped()
    {
        NavigateToClientValidationPage("formnovalidate");

        // Fill only Name; the form has 3 other "required" fields that are hidden,
        // disabled, or display:none and should all be skipped during validation.
        Browser.Exists(By.Id("name")).SendKeys("Alice");
        Browser.Exists(By.Id("btn-submit")).Click();

        Browser.Contains("valid:true", () => Browser.Exists(By.Id("event-log")).Text);
    }

    [Fact]
    public void FormnovalidateButtonBypassesValidation()
    {
        NavigateToClientValidationPage("formnovalidate");

        // Click the formnovalidate button without filling anything; validation
        // is bypassed entirely (no validationcomplete event dispatched, no errors).
        Browser.Exists(By.Id("btn-draft")).Click();

        // No error message text in the Name field's slot.
        Browser.Equal("",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Name']")).Text);
    }

    [Fact]
    public void UntrackedFormHasNoNovalidateAttribute()
    {
        // The 'no-validation' page intentionally has no [data-val=true] elements,
        // so the JS library leaves the form alone and never adds [novalidate].
        // Skip the submit interceptor too, so the page's "untracked forms post
        // normally" behaviour is not masked from any future assertions.
        NavigateToClientValidationPage("no-validation", expectTrackedForm: false, interceptSubmit: false);

        Browser.True(() => Browser.Exists(By.Id("plain-form")).GetAttribute("novalidate") is null);
    }

    [Fact]
    public void SubmittingOneFormDoesNotValidateOtherForm()
    {
        NavigateToClientValidationPage("multiple-forms");
        Browser.Exists(By.CssSelector("#form-a[novalidate]"));
        Browser.Exists(By.CssSelector("#form-b[novalidate]"));

        Browser.Exists(By.Id("submit-a")).Click();

        Browser.Equal("Name A is required.",
            () => Browser.Exists(By.CssSelector("#form-a [data-valmsg-for='Name']")).Text);
        Browser.Equal("",
            () => Browser.Exists(By.CssSelector("#form-b [data-valmsg-for='Name']")).Text);
    }

    [Fact]
    public void EachFormHasIndependentSummary()
    {
        NavigateToClientValidationPage("multiple-forms");

        Browser.Exists(By.Id("submit-a")).Click();

        Browser.True(() =>
            Browser.FindElements(By.CssSelector("#form-a [data-valmsg-summary='true'] li")).Count > 0);
        Browser.Equal(0,
            () => Browser.FindElements(By.CssSelector("#form-b [data-valmsg-summary='true'] li")).Count);
    }

    [Fact]
    public void DynamicallyAddedFieldsValidatedAfterScanRules()
    {
        NavigateToClientValidationPage("dynamic-content");

        Browser.Exists(By.Id("add-field")).Click();
        Browser.Exists(By.Id("dyn"));

        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Dyn is required.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Dyn']")).Text);
    }

    [Fact]
    public void RemovedFieldsCleanedUpOnReScan()
    {
        NavigateToClientValidationPage("dynamic-content");

        Browser.Exists(By.Id("add-field")).Click();
        Browser.Exists(By.Id("dyn"));
        Browser.Exists(By.Id("remove-field")).Click();
        Browser.DoesNotExist(By.Id("dyn"));

        // Submitting now should fail only on Name, not on Dyn (which no longer exists).
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Name is required.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Name']")).Text);
    }

    [Fact]
    public void FirstInvalidFieldFocusedOnSubmit()
    {
        NavigateToClientValidationPage("basic-validation");

        Browser.Exists(By.Id("submit")).Click();

        // First invalid field is Name; engine focuses it after validateForm.
        Browser.Equal("name",
            () => Browser.SwitchTo().ActiveElement().GetAttribute("id"));
    }
}
