// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests;

/// <summary>
/// E2E tests for client-side validation attribute generation in Blazor SSR forms.
/// These tests verify that the C# layer correctly emits data-val-* attributes
/// on form inputs rendered in SSR mode.
///
/// Expected to FAIL until the C# attribute generation implementation is complete.
/// </summary>
public class ClientValidationAttributeTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public ClientValidationAttributeTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    // -----------------------------------------------------------------------
    // Scenario 1/2: SSR form emits data-val-* attributes for each annotation
    // -----------------------------------------------------------------------

    [Fact]
    public void RequiredField_GetsDataValRequired()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("Name"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-required"));
    }

    [Fact]
    public void EmailField_GetsDataValEmail()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("Email"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-required"));
        Assert.NotNull(input.GetDomAttribute("data-val-email"));
    }

    [Fact]
    public void StringLengthField_GetsDataValLengthWithMinMax()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("Password"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-length"));
        Assert.Equal("8", input.GetDomAttribute("data-val-length-min"));
        Assert.Equal("100", input.GetDomAttribute("data-val-length-max"));
    }

    [Fact]
    public void CompareField_GetsDataValEqualtoWithOtherProperty()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("ConfirmPassword"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-equalto"));
        Assert.Equal("*.Password", input.GetDomAttribute("data-val-equalto-other"));
    }

    [Fact]
    public void RangeField_GetsDataValRangeWithMinMax()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("Age"));
        Assert.NotNull(input.GetDomAttribute("data-val-range"));
        Assert.Equal("18", input.GetDomAttribute("data-val-range-min"));
        Assert.Equal("120", input.GetDomAttribute("data-val-range-max"));
    }

    [Fact]
    public void PhoneField_GetsDataValPhone()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("Phone"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-phone"));
    }

    [Fact]
    public void UrlField_GetsDataValUrl()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("Website"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-url"));
    }

    [Fact]
    public void CreditCardField_GetsDataValCreditcard()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("CreditCard"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-creditcard"));
    }

    [Fact]
    public void RegexField_GetsDataValRegexWithPattern()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("ZipCode"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-regex"));
        Assert.Equal(@"\d{5}(-\d{4})?", input.GetDomAttribute("data-val-regex-pattern"));
    }

    [Fact]
    public void FileExtensionsField_GetsDataValFileextensionsWithExtensions()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("AvatarFilename"));
        Assert.Equal("true", input.GetDomAttribute("data-val"));
        Assert.NotNull(input.GetDomAttribute("data-val-fileextensions"));
        Assert.Equal(".png,.jpg,.jpeg,.gif", input.GetDomAttribute("data-val-fileextensions-extensions"));
    }

    // -----------------------------------------------------------------------
    // Scenario 1: ValidationMessage emits data-valmsg-for
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidationMessage_RendersDataValmsgFor()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        // Find message element for the Name field
        var input = Browser.Exists(By.Id("Name"));
        var fieldName = input.GetDomAttribute("name");
        Assert.NotNull(fieldName);

        var msgElement = Browser.Exists(By.CssSelector($"[data-valmsg-for='{fieldName}']"));
        Assert.Equal("true", msgElement.GetDomAttribute("data-valmsg-replace"));
    }

    // -----------------------------------------------------------------------
    // Scenario 1: ValidationSummary emits data-valmsg-summary
    // -----------------------------------------------------------------------

    [Fact]
    public void ValidationSummary_RendersDataValmsgSummary()
    {
        Navigate($"{ServerPathBase}/forms/client-validation");
        Browser.Exists(By.Id("page-title"));

        var summary = Browser.Exists(By.CssSelector("[data-valmsg-summary='true']"));
        Assert.NotNull(summary);

        // Should contain a <ul> element
        var ul = summary.FindElement(By.TagName("ul"));
        Assert.NotNull(ul);
    }

    // -----------------------------------------------------------------------
    // Scenario 6: Opt-out via EnableClientValidation="false"
    // -----------------------------------------------------------------------

    [Fact]
    public void OptOut_NoDataValAttributes()
    {
        Navigate($"{ServerPathBase}/forms/client-validation-optout");
        Browser.Exists(By.Id("page-title"));

        var input = Browser.Exists(By.Id("OptoutName"));
        Assert.Null(input.GetDomAttribute("data-val"));
    }

    [Fact]
    public void OptOut_NoDataValmsgForAttributes()
    {
        Navigate($"{ServerPathBase}/forms/client-validation-optout");
        Browser.Exists(By.Id("page-title"));

        var msgElements = Browser.FindElements(By.CssSelector("#optout-form [data-valmsg-for]"));
        Assert.Empty(msgElements);
    }

    // -----------------------------------------------------------------------
    // Scenario 7: Interactive render mode does not emit data-val-*
    // -----------------------------------------------------------------------

    [Fact]
    public void InteractiveForm_NoDataValAttributes()
    {
        Navigate($"{ServerPathBase}/forms/client-validation-interactive");
        // Wait for interactive rendering to complete
        Browser.Exists(By.Id("is-interactive"));

        var input = Browser.Exists(By.Id("InteractiveName"));
        Assert.Null(input.GetDomAttribute("data-val"));
    }

    [Fact]
    public void InteractiveForm_NoDataValmsgFor()
    {
        Navigate($"{ServerPathBase}/forms/client-validation-interactive");
        Browser.Exists(By.Id("is-interactive"));

        var msgElements = Browser.FindElements(By.CssSelector("#interactive-form [data-valmsg-for]"));
        Assert.Empty(msgElements);
    }

    [Fact]
    public void InteractiveForm_NoDataValmsgSummary()
    {
        Navigate($"{ServerPathBase}/forms/client-validation-interactive");
        Browser.Exists(By.Id("is-interactive"));

        var summaryElements = Browser.FindElements(By.CssSelector("#interactive-form [data-valmsg-summary]"));
        Assert.Empty(summaryElements);
    }
}
