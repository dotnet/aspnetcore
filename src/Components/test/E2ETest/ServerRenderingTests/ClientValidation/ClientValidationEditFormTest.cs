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

// Verifies that DefaultClientValidationService produces correct data-val-* attributes when
// rendering through the full .NET pipeline (EditForm + DataAnnotationsValidator + InputBase).
// No IValidationLocalizer configured — this pins the non-localized baseline output for
// every supported [DataAnnotations] attribute. Localized output is covered separately by
// ClientValidationLocalizationTest.
[CollectionDefinition(nameof(ClientValidationBasicTest), DisableParallelization = true)]
public class ClientValidationEditFormTest : ClientValidationTestBase
{
    public ClientValidationEditFormTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact(Skip = "Rework in progress: see rework-client-validation-tests.md - existing E2E suite depends on the data-val-* wire protocol that is being replaced by <blazor-client-validation-data> in Phase 2 of the rework.")]
    public void Required_EmitsDataValRequired_WithDisplayNameInMessage()
    {
        NavigateToEditFormValidationPage();

        // [Required] + [Display(Name="Full Name")] — message uses the localized display name
        // resolved by DefaultClientValidationService.ResolveDisplayName.
        Assert.Equal("The Full Name field is required.", GetDataValAttribute("name", "data-val-required"));
    }

    [Fact(Skip = "Rework in progress: see rework-client-validation-tests.md - existing E2E suite depends on the data-val-* wire protocol that is being replaced by <blazor-client-validation-data> in Phase 2 of the rework.")]
    public void Required_NoDisplayAttribute_UsesPropertyNameInMessage()
    {
        NavigateToEditFormValidationPage();

        // [Required] without [Display] — message falls back to the property name.
        Assert.Equal("The Password field is required.", GetDataValAttribute("password", "data-val-required"));
    }

    [Fact(Skip = "Rework in progress: see rework-client-validation-tests.md - existing E2E suite depends on the data-val-* wire protocol that is being replaced by <blazor-client-validation-data> in Phase 2 of the rework.")]
    public void StringLength_EmitsLengthAttributesWithMinAndMax()
    {
        NavigateToEditFormValidationPage();

        var bio = Browser.Exists(By.Id("bio"));
        Assert.NotNull(bio.GetAttribute("data-val-length"));
        Assert.Equal("10", bio.GetAttribute("data-val-length-min"));
        Assert.Equal("500", bio.GetAttribute("data-val-length-max"));
    }

    [Fact(Skip = "Rework in progress: see rework-client-validation-tests.md - existing E2E suite depends on the data-val-* wire protocol that is being replaced by <blazor-client-validation-data> in Phase 2 of the rework.")]
    public void RegularExpression_EmitsRegexAttributeAndPattern()
    {
        NavigateToEditFormValidationPage();

        var zip = Browser.Exists(By.Id("zipcode"));
        Assert.NotNull(zip.GetAttribute("data-val-regex"));
        Assert.Equal(@"\d{5}", zip.GetAttribute("data-val-regex-pattern"));
    }

    [Fact(Skip = "Rework in progress: see rework-client-validation-tests.md - existing E2E suite depends on the data-val-* wire protocol that is being replaced by <blazor-client-validation-data> in Phase 2 of the rework.")]
    public void Url_EmitsDataValUrl()
    {
        NavigateToEditFormValidationPage();

        var website = Browser.Exists(By.Id("website"));
        Assert.NotNull(website.GetAttribute("data-val-url"));
    }

    [Fact(Skip = "Rework in progress: see rework-client-validation-tests.md - existing E2E suite depends on the data-val-* wire protocol that is being replaced by <blazor-client-validation-data> in Phase 2 of the rework.")]
    public void Compare_EmitsEqualToAttributeAndOther()
    {
        NavigateToEditFormValidationPage();

        var confirm = Browser.Exists(By.Id("confirmpassword"));
        Assert.NotNull(confirm.GetAttribute("data-val-equalto"));
        // The "*." prefix is the JS equalto convention for resolving the other field
        // relative to the current field's name prefix.
        Assert.Equal("*.Password", confirm.GetAttribute("data-val-equalto-other"));
    }

    [Fact(Skip = "Rework in progress: see rework-client-validation-tests.md - existing E2E suite depends on the data-val-* wire protocol that is being replaced by <blazor-client-validation-data> in Phase 2 of the rework.")]
    public void TrackedField_HasDataValTrueMarker()
    {
        NavigateToEditFormValidationPage();

        // Every input with at least one validation attribute gets data-val="true".
        Assert.Equal("true", Browser.Exists(By.Id("name")).GetAttribute("data-val"));
        Assert.Equal("true", Browser.Exists(By.Id("zipcode")).GetAttribute("data-val"));
    }

    [Fact(Skip = "Rework in progress: see rework-client-validation-tests.md - existing E2E suite depends on the data-val-* wire protocol that is being replaced by <blazor-client-validation-data> in Phase 2 of the rework.")]
    public void FormHasNovalidate_AfterJsValidationLibraryScans()
    {
        // Sanity check: the JS lib scans the rendered form and applies novalidate.
        // This is also what NavigateToClientValidationPage's expectTrackedForm waits for.
        NavigateToEditFormValidationPage();

        Browser.Exists(By.CssSelector("form[novalidate]"));
    }

    private void NavigateToEditFormValidationPage()
        => NavigateToClientValidationPage("editform-validation");

    private string GetDataValAttribute(string inputId, string attributeName)
        => Browser.Exists(By.Id(inputId)).GetAttribute(attributeName);
}
