// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text.Json;
using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.ClientValidation;

public class ClientValidationTest : ClientValidationTestBase
{
    public ClientValidationTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void BasicForm_InvalidSubmit_DisplaysErrors_ValidSubmit_Clears()
    {
        NavigateToClientValidationPage("basic-validation");

        // The message placeholders are present in the DOM but hidden (display:none), so no empty
        // message box shows before any validation runs.
        var messageFields = new[] { "Form.Name", "Form.Email", "Form.Password", "Form.ConfirmPassword" };
        Browser.Equal(messageFields.Length, () => Browser.FindElements(By.CssSelector("[data-valmsg-for]")).Count);
        foreach (var field in messageFields)
        {
            AssertMessagePlaceholderHidden(field);
        }

        // The validation summary starts hidden (no server errors) so it takes up no layout space.
        AssertSummaryHidden();

        // Empty submit: the three [Required] fields report errors. [Compare]/[EmailAddress]/
        // [StringLength] do not fire on empty values.
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Name is required.", () => FieldMessage("Form.Name"));
        Browser.Equal("Email is required.", () => FieldMessage("Form.Email"));
        Browser.Equal("Password is required.", () => FieldMessage("Form.Password"));

        // The summary is revealed and lists the same errors as <li> items.
        Browser.Equal("block", () => Browser.Exists(By.CssSelector("ul[data-valmsg-summary]")).GetCssValue("display"));
        Browser.Equal(3, () => Browser.FindElements(By.CssSelector("ul[data-valmsg-summary] > li.validation-message")).Count);
        var summaryMessages = Browser.FindElements(By.CssSelector("ul[data-valmsg-summary] > li.validation-message"))
            .Select(li => li.Text).ToList();
        Assert.Contains("Name is required.", summaryMessages);
        Assert.Contains("Email is required.", summaryMessages);
        Assert.Contains("Password is required.", summaryMessages);

        // While errors are shown the summary carries the error-state class, not the valid one.
        var summaryErrorClasses = Browser.Exists(By.CssSelector("ul[data-valmsg-summary]")).GetAttribute("class");
        Assert.Contains("validation-summary-errors", summaryErrorClasses);
        Assert.DoesNotContain("validation-summary-valid", summaryErrorClasses);

        // Fire one non-required rule to test integration end-to-end.
        Browser.Exists(By.Id("name")).SendKeys("Alice");
        Browser.Exists(By.Id("email")).SendKeys("not-an-email");
        Browser.Exists(By.Id("password")).SendKeys("longenoughpassword");
        Browser.Exists(By.Id("confirmpassword")).SendKeys("longenoughpassword");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Email is not valid.", () => FieldMessage("Form.Email"));
        Browser.Equal("", () => FieldMessage("Form.Name"));
        Browser.Equal("", () => FieldMessage("Form.Password"));
        Browser.Equal("", () => FieldMessage("Form.ConfirmPassword"));

        // Fix the email and submit: all errors clear and the form reports valid.
        ReplaceText(By.Id("email"), "alice@example.com");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("valid:true;errors:0", () => Browser.Exists(By.Id("event-log")).Text);
        Browser.Equal("", () => FieldMessage("Form.Email"));

        var nameClasses = Browser.Exists(By.Id("name")).GetAttribute("class");
        Assert.Contains("modified", nameClasses);
        Assert.DoesNotContain("invalid", nameClasses);
        Assert.Contains("valid", nameClasses);

        // After clearing, every message placeholder is hidden again (display:none), not just emptied.
        foreach (var field in messageFields)
        {
            AssertMessagePlaceholderHidden(field);
        }

        // The summary is emptied and hidden again once the form is valid.
        AssertSummaryHidden();
        Browser.Equal(0, () => Browser.FindElements(By.CssSelector("ul[data-valmsg-summary] > li")).Count);

        // The summary toggles back to the valid-state class.
        var summaryValidClasses = Browser.Exists(By.CssSelector("ul[data-valmsg-summary]")).GetAttribute("class");
        Assert.Contains("validation-summary-valid", summaryValidClasses);
        Assert.DoesNotContain("validation-summary-errors", summaryValidClasses);
    }

    [Fact]
    public void CarrierElement_IsRenderedInsideForm_WithExpectedJsonShape()
    {
        NavigateToClientValidationPage("basic-validation");

        // Exactly one carrier, and it is a descendant of the form.
        Assert.Single(Browser.FindElements(By.CssSelector("form blazor-client-validation-data")));

        var json = (string)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return document.querySelector('blazor-client-validation-data').getAttribute('data-rules');");

        using var document = JsonDocument.Parse(json);
        var fields = document.RootElement.GetProperty("fields").EnumerateArray().ToList();

        var fieldNames = fields
            .Select(f => f.GetProperty("name").GetString())
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        // The carrier must emit exactly the four validatable fields on this page,
        // unexpected fields (or a missing one) fail the test.
        Assert.Equal(
            new[] { "Form.ConfirmPassword", "Form.Email", "Form.Name", "Form.Password" },
            fieldNames);

        // The Email field carries exactly a 'required' and an 'email' rule.
        var emailRules = fields
            .Single(f => f.GetProperty("name").GetString() == "Form.Email")
            .GetProperty("rules").EnumerateArray()
            .Select(r => r.GetProperty("name").GetString())
            .OrderBy(r => r, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(new[] { "email", "required" }, emailRules);
    }

    // Tests checking that the JS adapter layer supports enhanced navigation correctly.
    // Enhanced navigation updates the page by DOM morphing, which reuses the carrier element
    // (its connectedCallback does not re-fire) and strips the JS-added novalidate.
    // These tests cover the transitions: form->form->back (form->form is its first leg),
    // form->no-form->form, and no-form->form.

    [Fact]
    public void EnhancedNavigation_FormToFormAndBack_EachValidatesOwnRules()
    {
        NavigateToClientValidationPage("enhanced-nav-a");
        MarkEnhancedNavProbe();

        // A -> B
        Browser.Exists(By.Id("go-to-b")).Click();
        Browser.Equal("Enhanced navigation form B", () => Browser.Exists(By.Id("page-title")).Text);
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Beta is required.", () => FieldMessage("Form.Beta"));

        // B -> A: the reused carrier's payload changes back to A's rules.
        Browser.Exists(By.Id("go-to-a")).Click();
        Browser.Equal("Enhanced navigation form A", () => Browser.Exists(By.Id("page-title")).Text);
        AssertWasEnhancedNavigation();
        Browser.Exists(By.CssSelector("form[novalidate]"));
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Alpha is required.", () => FieldMessage("Form.Alpha"));
    }

    [Fact]
    public void EnhancedNavigation_FormToNoFormAndBack_RevalidatesForm()
    {
        NavigateToClientValidationPage("enhanced-nav-a");
        MarkEnhancedNavProbe();

        // A -> no-form page: the carrier is removed; nothing should remain tracked.
        Browser.Exists(By.Id("go-to-noform")).Click();
        Browser.Equal("Enhanced navigation no-form page", () => Browser.Exists(By.Id("page-title")).Text);
        AssertWasEnhancedNavigation();
        Browser.DoesNotExist(By.CssSelector("blazor-client-validation-data"));

        // no-form -> A: the form must be (re-)registered even though the service already exists.
        Browser.Exists(By.Id("go-to-a")).Click();
        Browser.Equal("Enhanced navigation form A", () => Browser.Exists(By.Id("page-title")).Text);
        Browser.Exists(By.CssSelector("form[novalidate]"));
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Alpha is required.", () => FieldMessage("Form.Alpha"));
    }

    [Fact]
    public void EnhancedNavigation_NoFormToForm_RegistersValidation()
    {
        // Start on a page with no carrier (the service is not created on this page).
        Navigate("subdir/forms/client-validation/enhanced-nav-noform");
        Browser.Exists(By.Id("blazor-started"));
        Browser.Equal("Enhanced navigation no-form page", () => Browser.Exists(By.Id("page-title")).Text);
        MarkEnhancedNavProbe();

        // no-form -> form A: the service is created on first carrier sighting and registers A.
        Browser.Exists(By.Id("go-to-a")).Click();
        Browser.Equal("Enhanced navigation form A", () => Browser.Exists(By.Id("page-title")).Text);
        AssertWasEnhancedNavigation();
        Browser.Exists(By.CssSelector("form[novalidate]"));

        ((IJavaScriptExecutor)Browser).ExecuteScript(
            "document.addEventListener('submit', function (e) { e.preventDefault(); }, false);");
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Alpha is required.", () => FieldMessage("Form.Alpha"));
    }

    [Fact]
    public void StreamingRendering_FormDeliveredViaStream_RegistersValidation()
    {
        // The form is rendered after a streaming delay, so it arrives via a streamed DOM update
        // (the same merge + 'enhancedload' path as enhanced navigation). The helper waits for
        // form[novalidate], which only appears once the streamed form is registered.
        NavigateToClientValidationPage("streaming-validation");

        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Gamma is required.", () => FieldMessage("Form.Gamma"));
    }

    [Fact]
    public void MultipleForms_OnSamePage_ValidateIndependently()
    {
        NavigateToClientValidationPage("multiple-forms");

        // Submitting form A validates only form A; form B stays untouched.
        Browser.Exists(By.Id("submit-a")).Click();
        Browser.Equal("Name A is required.",
            () => Browser.Exists(By.CssSelector("#form-a [data-valmsg-for='FormA.Name']")).Text);
        Browser.Equal("",
            () => Browser.Exists(By.CssSelector("#form-b [data-valmsg-for='FormB.Name']")).Text);

        // Submitting form B now validates only form B.
        Browser.Exists(By.Id("submit-b")).Click();
        Browser.Equal("Name B is required.",
            () => Browser.Exists(By.CssSelector("#form-b [data-valmsg-for='FormB.Name']")).Text);
    }

    [Fact]
    public void CustomValidator_RegisteredViaAddValidator_FiresFromEmittedRule()
    {
        NavigateToClientValidationPage("custom-validator");

        // Wait until the page has registered the 'startswith' JS validator.
        Browser.Exists(By.Id("custom-validator-ready"));

        Browser.Exists(By.Id("code")).SendKeys("XYZ-123");
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Code must start with 'ABC-'.", () => FieldMessage("Form.Code"));

        // A value satisfying the rule clears the error.
        ReplaceText(By.Id("code"), "ABC-123");
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("", () => FieldMessage("Form.Code"));
    }

    [Fact]
    public void LocalizedValidation_RoundTripsAcrossCultureSwitches()
    {
        // French: the carrier payload carries fr-localized display name + message.
        NavigateToClientValidationPage("localized-validation?culture=fr");
        Browser.Exists(By.Id("submit")).Click();
        var frenchMessage = Browser.Exists(By.CssSelector("[data-valmsg-for='Form.Email']")).Text;
        Assert.Equal("Le champ Adresse e-mail est requis (fr)", frenchMessage);

        // German: the per-request localization must not be poisoned by the earlier French request.
        NavigateToClientValidationPage("localized-validation?culture=de");
        Browser.Exists(By.Id("submit")).Click();
        var germanMessage = Browser.Exists(By.CssSelector("[data-valmsg-for='Form.Email']")).Text;
        Assert.Equal("Das Feld E-Mail-Adresse ist erforderlich (de)", germanMessage);

        Assert.NotEqual(frenchMessage, germanMessage);
    }

    [Fact]
    public void DisableClientValidationTrue_EmitsNoCarrier()
    {
        // The page is reachable but emits no carrier: the JS engine never activates.
        NavigateToClientValidationPage(
            "basic-validation?disable-client-validation=true",
            expectTrackedForm: false);

        Browser.DoesNotExist(By.CssSelector("blazor-client-validation-data"));
        Browser.DoesNotExist(By.CssSelector("form[novalidate]"));
    }

    [Fact]
    public void InteractiveRenderMode_EmitsNoCarrier()
    {
        // Inputs rendered in an interactive render mode do not register for client validation
        // (no ClientValidationProvider outside Endpoints), so no carrier is emitted.
        NavigateToClientValidationPage("interactive-validation", expectTrackedForm: false);

        Browser.DoesNotExist(By.CssSelector("blazor-client-validation-data"));
    }

    private string FieldMessage(string fieldName)
        => Browser.Exists(By.CssSelector($"[data-valmsg-for='{fieldName}']")).Text;

    private void AssertMessagePlaceholderHidden(string fieldName)
        => Browser.Equal("none", () => Browser.Exists(By.CssSelector($"[data-valmsg-for='{fieldName}']")).GetCssValue("display"));

    private void AssertSummaryHidden()
        => Browser.Equal("none", () => Browser.Exists(By.CssSelector("ul[data-valmsg-summary]")).GetCssValue("display"));

    private void ReplaceText(By selector, string text)
    {
        var element = Browser.Exists(selector);
        element.Clear();
        element.SendKeys(text);
    }

    // Sets a window-scoped flag. Enhanced navigation preserves the JS context, so the flag
    // survives; a full page reload would clear it. Used to assert a transition was an enhanced
    // navigation (i.e. went through the DOM-merge path this fix targets), not a full reload.
    private void MarkEnhancedNavProbe()
        => ((IJavaScriptExecutor)Browser).ExecuteScript("window.__enhancedNavProbe = true;");

    private void AssertWasEnhancedNavigation()
        => Browser.True(() => (bool?)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return window.__enhancedNavProbe === true;") == true);
}
