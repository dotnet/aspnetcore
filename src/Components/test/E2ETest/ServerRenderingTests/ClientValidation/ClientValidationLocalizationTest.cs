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

// Verifies that DefaultClientValidationService renders culture-appropriate data-val-* attributes
// when a server-side IValidationLocalizer is configured. The TestServer registers an inline
// IValidationLocalizer that returns hardcoded translations based on CurrentUICulture, which is
// set per-request via the ?culture= query string (see RazorComponentEndpointsStartup).
public class ClientValidationLocalizationTest : ClientValidationTestBase
{
    public ClientValidationLocalizationTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    [Fact]
    public void DefaultCulture_UsesLiteralAttributeValuesAndDefaultMessages()
    {
        // No ?culture= → CurrentUICulture stays at the default (en-US). The localizer's lookup
        // misses for English keys → falls back to the literal display name and the attribute's
        // default formatted message ("RequiredKey" / "RangeKey" — they are the literal
        // ErrorMessage values, not real keys; this asserts the no-localization-hit path).
        NavigateToClientValidationPage("localized-validation");

        Assert.Equal("RequiredKey", GetDataValAttribute("email", "data-val-required"));
        Assert.Equal("RangeKey", GetDataValAttribute("age", "data-val-range"));
    }

    [Fact]
    public void FrenchCulture_LocalizesDisplayNameAndErrorMessage()
    {
        Navigate("subdir/forms/client-validation/localized-validation?culture=fr");
        Browser.Exists(By.Id("blazor-started"));
        Browser.Exists(By.Id("page-title"));
        Browser.Exists(By.CssSelector("form[novalidate]"));

        // Display name "Email Address" → "Adresse e-mail" (fr); template "Le champ {0} est requis (fr)"
        // is formatted with the localized display name.
        Assert.Equal("Le champ Adresse e-mail est requis (fr)", GetDataValAttribute("email", "data-val-required"));
        // Range template gets {0}=display, {1}=Min, {2}=Max substituted by our test localizer.
        Assert.Equal("Le champ Âge doit être entre 18 et 120 (fr)", GetDataValAttribute("age", "data-val-range"));
    }

    [Fact]
    public void GermanCulture_LocalizesDisplayNameAndErrorMessage()
    {
        Navigate("subdir/forms/client-validation/localized-validation?culture=de");
        Browser.Exists(By.Id("blazor-started"));
        Browser.Exists(By.Id("page-title"));
        Browser.Exists(By.CssSelector("form[novalidate]"));

        Assert.Equal("Das Feld E-Mail-Adresse ist erforderlich (de)", GetDataValAttribute("email", "data-val-required"));
        Assert.Equal("Das Feld Alter muss zwischen 18 und 120 liegen (de)", GetDataValAttribute("age", "data-val-range"));
    }

    [Fact]
    public void DifferentCulturesProduceDifferentOutput_NoCachePoisoning()
    {
        // Regression guard: visit French first, then German on the SAME server (which holds the
        // singleton DefaultClientValidationService). The reflection cache is shared across
        // requests but the per-call display-name and error-message resolution must respect the
        // current request's CultureInfo.CurrentUICulture.
        Navigate("subdir/forms/client-validation/localized-validation?culture=fr");
        Browser.Exists(By.Id("blazor-started"));
        Browser.Exists(By.CssSelector("form[novalidate]"));
        var frRequired = GetDataValAttribute("email", "data-val-required");

        Navigate("subdir/forms/client-validation/localized-validation?culture=de");
        Browser.Exists(By.Id("blazor-started"));
        Browser.Exists(By.CssSelector("form[novalidate]"));
        var deRequired = GetDataValAttribute("email", "data-val-required");

        Assert.NotEqual(frRequired, deRequired);
        Assert.Contains("(fr)", frRequired);
        Assert.Contains("(de)", deRequired);
    }

    private string GetDataValAttribute(string inputId, string attributeName)
        => Browser.Exists(By.Id(inputId)).GetAttribute(attributeName);
}
