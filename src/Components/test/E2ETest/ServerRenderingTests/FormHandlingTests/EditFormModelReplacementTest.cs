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

// Regression test for https://github.com/dotnet/aspnetcore/issues/41621
// Replacing the EditForm.Model causes a new EditContext to be created, which (by design) keys
// the child region on the EditContext hash. This causes all child components to be destroyed and
// recreated. The tests here document the current behavior and will need updating if/when the
// framework adds an opt-in mechanism (e.g. AllowModelChange) to preserve child state across model
// swaps.
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

    [Fact]
    public void ReplacingModel_DestroysAndRecreatesChildComponents()
    {
        Navigate($"{ServerPathBase}/forms/editform-model-replacement");

        // Wait for interactive rendering to be active.
        Browser.Exists(By.Id("replace-model"));

        // After initial render, both the inside and outside reporters should have been
        // initialised exactly once and received their first OnParametersSet call.
        // The inside reporter emits: init, params
        Browser.Contains("init, params", () => Browser.FindElement(By.Id("inside-lifecycle")).Text);
        Browser.Contains("init, params", () => Browser.FindElement(By.Id("outside-lifecycle")).Text);

        // Replace the model — this creates a new EditContext instance.
        Browser.Click(By.Id("replace-model"));
        Browser.Equal("Swaps: 1", () => Browser.FindElement(By.Id("swap-count")).Text);

        // Bug (by design): the inside component is disposed and recreated because EditForm keys
        // its child region on the EditContext instance hash. Each swap appends dispose+init+params.
        Browser.Contains("dispose", () => Browser.FindElement(By.Id("inside-lifecycle")).Text);
        Browser.Contains("init, params, dispose, init, params", () => Browser.FindElement(By.Id("inside-lifecycle")).Text);

        // The outside component — not a descendant of EditForm — is NOT destroyed; it only
        // receives an additional OnParametersSet call on each render.
        var outsideText = Browser.FindElement(By.Id("outside-lifecycle")).Text;
        Assert.DoesNotContain("dispose", outsideText);
    }

    [Fact]
    public void ReplacingModel_MultipleSwaps_EachSwapDestroysAndRecreatesChildComponent()
    {
        Navigate($"{ServerPathBase}/forms/editform-model-replacement");

        Browser.Exists(By.Id("replace-model"));

        // Perform two model swaps.
        Browser.Click(By.Id("replace-model"));
        Browser.Equal("Swaps: 1", () => Browser.FindElement(By.Id("swap-count")).Text);

        Browser.Click(By.Id("replace-model"));
        Browser.Equal("Swaps: 2", () => Browser.FindElement(By.Id("swap-count")).Text);

        // After two swaps the inside reporter has been disposed twice.
        var insideText = Browser.FindElement(By.Id("inside-lifecycle")).Text;

        // Count the number of "dispose" occurrences.
        var disposeCount = CountOccurrences(insideText, "dispose");
        Assert.Equal(2, disposeCount);

        // Count the number of "init" occurrences (initial + one per swap).
        var initCount = CountOccurrences(insideText, "init");
        Assert.Equal(3, initCount);
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }
        return count;
    }
}
