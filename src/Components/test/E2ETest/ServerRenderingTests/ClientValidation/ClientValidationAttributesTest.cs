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

// E2E smoke coverage for individual built-in validators, custom validator
// registration, radio groups, and server-rendered message cleanup.
// Per-validator exhaustive coverage lives in the Jest unit tests on
// CoreValidators.test.ts; the tests here verify the integration path.
public class ClientValidationAttributesTest : ClientValidationTestBase
{
    public ClientValidationAttributesTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void RangeRejectsOutOfRangeValue()
    {
        NavigateToClientValidationPage("all-validators");

        Browser.Exists(By.Id("age")).SendKeys("999");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Age must be between 1 and 150.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Age']")).Text);
    }

    [Fact]
    public void RegexRejectsNonMatchingValue()
    {
        NavigateToClientValidationPage("all-validators");

        Browser.Exists(By.Id("zipcode")).SendKeys("not-a-zip");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Invalid zip code.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='ZipCode']")).Text);
    }

    [Fact]
    public void EmailRejectsInvalidEmail()
    {
        NavigateToClientValidationPage("all-validators");

        Browser.Exists(By.Id("email")).SendKeys("not-an-email");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Invalid email address.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Email']")).Text);
    }

    [Fact]
    public void EqualToRejectsMismatchedPasswords()
    {
        NavigateToClientValidationPage("all-validators");

        Browser.Exists(By.Id("password")).SendKeys("secret123");
        Browser.Exists(By.Id("confirm")).SendKeys("differentValue");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Passwords must match.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='ConfirmPassword']")).Text);
    }

    [Fact]
    public void EqualToAcceptsMatchingPasswords()
    {
        NavigateToClientValidationPage("all-validators");

        Browser.Exists(By.Id("password")).SendKeys("secret123");
        Browser.Exists(By.Id("confirm")).SendKeys("secret123");
        Browser.Exists(By.Id("submit")).Click();

        // The ConfirmPassword field has no error; the form is still invalid
        // because of the Anchor field, but ConfirmPassword's slot is empty.
        Browser.Equal("",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='ConfirmPassword']")).Text);
    }

    [Fact]
    public void FileExtensionsRejectsDisallowedExtension()
    {
        NavigateToClientValidationPage("all-validators");

        Browser.Exists(By.Id("avatar")).SendKeys("malware.exe");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Only image files are allowed.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Avatar']")).Text);
    }

    [Fact]
    public void RequiredRadioGroup_ShowsErrorWhenNoneSelected()
    {
        NavigateToClientValidationPage("radio-group");

        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Please select a color.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Color']")).Text);
    }

    [Fact]
    public void RequiredRadioGroup_ClearsErrorWhenOneSelected()
    {
        NavigateToClientValidationPage("radio-group");

        Browser.Exists(By.Id("submit")).Click();
        Browser.Equal("Please select a color.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Color']")).Text);

        Browser.Exists(By.Id("color-green")).Click();
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Color']")).Text);
    }

    [Fact]
    public void CustomValidatorRejectsInvalidInput()
    {
        NavigateToClientValidationPage("custom-validator");
        Browser.Exists(By.Id("custom-validator-ready"));

        Browser.Exists(By.Id("code")).SendKeys("XYZ-123");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("Code must start with 'ABC-'.",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Code']")).Text);
    }

    [Fact]
    public void CustomValidatorAcceptsValidInput()
    {
        NavigateToClientValidationPage("custom-validator");
        Browser.Exists(By.Id("custom-validator-ready"));

        Browser.Exists(By.Id("code")).SendKeys("ABC-123");
        Browser.Exists(By.Id("submit")).Click();

        Browser.Equal("",
            () => Browser.Exists(By.CssSelector("[data-valmsg-for='Code']")).Text);
    }

    [Fact]
    public void JsRemovesSiblingServerErrorsWhenFieldBecomesValid()
    {
        NavigateToClientValidationPage("server-rendered-messages");

        // Initially: the Name field has a primary message + one extra sibling
        // (server-rendered). Both are present in the DOM.
        Browser.True(() =>
            Browser.FindElements(By.CssSelector("#test-form > div:first-child > .validation-message")).Count == 2);

        // Fill Name and trigger blur to validate
        Browser.Exists(By.Id("name")).SendKeys("Alice");
        Browser.Exists(By.Id("name")).SendKeys(Keys.Tab);

        // Sibling cleanup: only the primary [data-valmsg-for] message remains.
        Browser.True(() =>
            Browser.FindElements(By.CssSelector("#test-form > div:first-child > .validation-message")).Count == 1);
    }
}
