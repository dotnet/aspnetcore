// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Components.TestServer.RazorComponents;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure;
using Microsoft.AspNetCore.Components.E2ETest.Infrastructure.ServerFixtures;
using Microsoft.AspNetCore.E2ETesting;
using OpenQA.Selenium;
using TestServer;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Components.E2ETests.ServerRenderingTests.FormHandlingTests;

// Regression tests for https://github.com/dotnet/aspnetcore/issues/41621
//
// When EditForm.Model is replaced, EditForm.BuildRenderTree calls builder.OpenRegion(editContext.GetHashCode()).
// A new model → new EditContext → different hash → the entire child subtree is destroyed and recreated.
// The fix adds AllowModelChange="true", which suppresses the keyed region and cascades the EditContext with
// IsFixed=false, so children are updated in place instead of being destroyed.
//
// LifecycleReporter renders a per-instance GUID (set in OnInitialized). Comparing the GUID before and after a
// model swap proves whether the same component instance was preserved (fix) or a new one was created (bug).
[CollectionDefinition(nameof(EditFormModelReplacementTest), DisableParallelization = true)]
public class EditFormModelReplacementTest : ServerTestBase<BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>>>
{
    public EditFormModelReplacementTest(
        BrowserFixture browserFixture,
        BasicTestAppServerSiteFixture<RazorComponentEndpointsStartup<App>> serverFixture,
        ITestOutputHelper output)
        : base(browserFixture, serverFixture, output)
    {
    }

    public override Task InitializeAsync()
        => InitializeAsync(BrowserFixture.StreamingContext);

    // Proper regression test: it FAILS on the buggy framework (child is destroyed → GUID changes) and
    // PASSES once AllowModelChange is honored (same instance preserved → GUID unchanged).
    [Fact]
    public void AllowModelChange_ReplacingModel_PreservesChildComponentInstance()
    {
        Navigate($"{ServerPathBase}/forms/editform-model-replacement");

        // Wait for the interactive circuit: RendererInfo.IsInteractive flips to "True"
        // once the circuit re-renders the page interactively.
        Browser.Equal("True", () => Browser.FindElement(By.Id("is-interactive")).Text);

        // Capture the child component's instance GUID before swapping the model.
        var idBeforeSwap = Browser.FindElement(By.Id("allow-model-change-reporter")).Text;
        Assert.NotEmpty(idBeforeSwap);

        // Replace the EditForm.Model with a brand-new instance.
        Browser.Click(By.Id("replace-allow-model-change-model"));
        Browser.Equal("Swaps: 1", () => Browser.FindElement(By.Id("allow-model-change-swap-count")).Text);

        // The InputText reflects the new model's value — confirming the model really changed.
        Browser.Equal("New model #1", () => Browser.FindElement(By.Id("allow-model-change-name-input")).GetAttribute("value"));

        // The fix: with AllowModelChange="true", the child is updated in place, so its instance
        // GUID is UNCHANGED. Without the fix, the keyed region destroys and recreates the child,
        // producing a different GUID and failing this assertion.
        Browser.Equal(idBeforeSwap, () => Browser.FindElement(By.Id("allow-model-change-reporter")).Text);

        // The lifecycle log must NOT contain a Dispose for the child — the instance was preserved.
        var log = Browser.FindElements(By.CssSelector("#allow-model-change-log li")).Select(e => e.Text).ToList();
        Assert.Contains("── swap #1 ──", log);
        Assert.DoesNotContain(log, e => e.Contains("[allow-model-change-reporter] Dispose"));
    }
}
