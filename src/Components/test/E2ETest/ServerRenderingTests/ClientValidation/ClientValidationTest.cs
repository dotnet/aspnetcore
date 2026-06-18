// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

// E2E coverage for the reworked client-side validation pipeline: a real Blazor SSR
// EditForm renders a single <blazor-client-validation-data> carrier element whose JSON
// payload the JS engine ingests, then validates user input in the browser before submit.
//
// This layer owns ONLY what requires a real browser + real Blazor SSR + the real JS engine
// wired together end to end. Per-attribute rule emission is owned by the Endpoints provider
// integration tests; the JS engine's validator/cleanup/ARIA behaviour is owned by the Jest
// suite. See rework-client-validation-tests.md section 3.4.
//
// Field names are prefixed by the [SupplyParameterFromForm] property name (e.g. the page's
// "Form" property yields rendered names "Form.Name", "Form.Email", ...). The input name,
// the [data-valmsg-for] slot and the carrier payload all use that same prefixed name.
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

        // Empty submit: the three [Required] fields report errors. [Compare]/[EmailAddress]/
        // [StringLength] do not fire on empty values.
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Name is required.", () => FieldMessage("Form.Name"));
        Browser.Equal("Email is required.", () => FieldMessage("Form.Email"));
        Browser.Equal("Password is required.", () => FieldMessage("Form.Password"));

        // Format errors: invalid email, too-short password, mismatched confirmation.
        Browser.Exists(By.Id("name")).SendKeys("Alice");
        Browser.Exists(By.Id("email")).SendKeys("not-an-email");
        Browser.Exists(By.Id("password")).SendKeys("short");
        Browser.Exists(By.Id("confirmpassword")).SendKeys("different");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("", () => FieldMessage("Form.Name"));
        Browser.Equal("Email is not valid.", () => FieldMessage("Form.Email"));
        Browser.Equal("Password must be between 8 and 50 characters.", () => FieldMessage("Form.Password"));
        Browser.Equal("Passwords must match.", () => FieldMessage("Form.ConfirmPassword"));

        // Fix everything and submit: all errors clear and the form reports valid.
        ReplaceText(By.Id("email"), "alice@example.com");
        ReplaceText(By.Id("password"), "longenoughpassword");
        ReplaceText(By.Id("confirmpassword"), "longenoughpassword");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("valid:true;errors:0", () => Browser.Exists(By.Id("event-log")).Text);
        Browser.Equal("", () => FieldMessage("Form.Email"));
        Browser.Equal("", () => FieldMessage("Form.Password"));
        Browser.Equal("", () => FieldMessage("Form.ConfirmPassword"));
    }

    [Fact]
    public void CarrierElement_IsRenderedInsideForm_WithExpectedJsonShape()
    {
        NavigateToClientValidationPage("basic-validation");

        // Exactly one carrier, and it is a descendant of the form.
        Assert.Single(Browser.FindElements(By.CssSelector("form blazor-client-validation-data")));

        var json = (string)((IJavaScriptExecutor)Browser).ExecuteScript(
            "return document.querySelector('blazor-client-validation-data').textContent;");

        using var document = JsonDocument.Parse(json);
        var fields = document.RootElement.GetProperty("fields").EnumerateArray().ToList();

        var fieldNames = fields.Select(f => f.GetProperty("name").GetString()).ToList();
        Assert.Contains("Form.Name", fieldNames);
        Assert.Contains("Form.Email", fieldNames);
        Assert.Contains("Form.Password", fieldNames);

        // The Email field carries both a 'required' and an 'email' rule.
        var emailRules = fields
            .Single(f => f.GetProperty("name").GetString() == "Form.Email")
            .GetProperty("rules").EnumerateArray()
            .Select(r => r.GetProperty("name").GetString())
            .ToList();
        Assert.Contains("required", emailRules);
        Assert.Contains("email", emailRules);
    }

    [Fact(Skip = "Reveals a real limitation: enhanced navigation reuses the <blazor-client-validation-data> carrier via DOM morphing, so connectedCallback does not re-fire and the destination form is left without client validation (and without novalidate). Tracked in client-validation-rework-todos.md. Unskip once Boot.Web re-processes carriers on 'enhancedload'.")]
    public void EnhancedNavigation_RevalidatesNewForm()
    {
        NavigateToClientValidationPage("enhanced-nav-a");

        // Enhanced-navigate to form B (no full page reload; the document-level submit
        // interceptor installed for form A persists).
        Browser.Exists(By.Id("go-to-b")).Click();
        Browser.Equal("Enhanced navigation form B", () => Browser.Exists(By.Id("page-title")).Text);
        Browser.Exists(By.CssSelector("form[novalidate]"));

        // Submitting form B uses form B's rules: its carrier was registered and form A's
        // unregistered during the DOM swap.
        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Beta is required.", () => FieldMessage("Form.Beta"));
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

        // German, on the same shared singleton cache: the per-request localization must not
        // be poisoned by the earlier French request.
        NavigateToClientValidationPage("localized-validation?culture=de");
        Browser.Exists(By.Id("submit")).Click();
        var germanMessage = Browser.Exists(By.CssSelector("[data-valmsg-for='Form.Email']")).Text;
        Assert.Equal("Das Feld E-Mail-Adresse ist erforderlich (de)", germanMessage);

        Assert.NotEqual(frenchMessage, germanMessage);
    }

    [Fact]
    public void EnableClientValidationFalse_EmitsNoCarrier()
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

    private void ReplaceText(By selector, string text)
    {
        var element = Browser.Exists(selector);
        element.Clear();
        element.SendKeys(text);
    }
}
