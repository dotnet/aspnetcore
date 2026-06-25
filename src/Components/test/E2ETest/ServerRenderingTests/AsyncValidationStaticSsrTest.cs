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

// Async form validation under static SSR (no interactivity). On POST the framework awaits ValidateAsync,
// so async DataAnnotations validation runs as part of request handling and its messages render in the response.
// This covers form-level async validation through both the static Validator and the Microsoft.Extensions.Validation path.
// Per-field on-edit validation and the pending async UX are not applicable without interactivity.
public class AsyncValidationStaticSsrTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public AsyncValidationStaticSsrTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    protected override void InitializeAsyncCore()
    {
        Navigate("subdir/forms/async-validation-static-ssr");
        Browser.Exists(By.Id("validator-form"));
        // The App root renders pages statically (no @rendermode), so the page must not become interactive.
        Assert.Empty(Browser.FindElements(By.Id("is-interactive")));
    }

    [Fact]
    public void ValidatorPath_FormLevelAsyncValidation_RendersMessagesOnPost()
    {
        // An async [ValidationAttribute] on a property: "taken" is rejected by the per-property async check.
        Submit("validator-username", "validator-submit", "taken");
        Browser.Equal(new[] { "Username is taken" }, FormMessages("validator-form"));

        // IAsyncValidatableObject object-level check: "reserved" passes the property check but is rejected here.
        Submit("validator-username", "validator-submit", "reserved");
        Browser.Equal(new[] { "Username is reserved" }, FormMessages("validator-form"));
    }

    [Fact]
    public void MevPath_FormLevelAsyncValidation_RendersMessagesOnPost()
    {
        Submit("mev-username", "mev-submit", "taken");
        Browser.Equal(new[] { "Username is taken" }, FormMessages("mev-form"));

        Submit("mev-username", "mev-submit", "reserved");
        Browser.Equal(new[] { "Username is reserved" }, FormMessages("mev-form"));
    }

    private void Submit(string inputId, string submitId, string value)
    {
        var input = Browser.Exists(By.Id(inputId));
        input.Clear();
        input.SendKeys(value);
        Browser.Exists(By.Id(submitId)).Click();
    }

    private Func<string[]> FormMessages(string formId)
        => () => Browser.FindElements(By.CssSelector($"#{formId} .validation-message"))
            .Select(e => e.Text)
            .OrderBy(x => x)
            .ToArray();
}
