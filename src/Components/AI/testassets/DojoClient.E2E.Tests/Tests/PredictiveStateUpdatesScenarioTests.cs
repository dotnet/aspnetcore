// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using DojoClient.E2E.Tests.Fixtures;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[UITest]
public partial class PredictiveStateUpdatesScenarioTests : DojoTestBase
{
    private const string InitialDocument =
        "# Harbor Notes\n\nThe crew is preparing for a quiet voyage.";
    private const string PirateOpening =
        "# Candy Beard's Voyage\n\nCandy Beard sailed from Gumdrop Harbor in search of the Sugar Star.";
    private const string PirateDocument =
        PirateOpening +
        "\n\nWhen dark clouds gathered, the crew shared their courage and found the way home.";
    private const string EditedPirateDocument =
        PirateDocument + "\n\nThe map now points toward Mermaid Lagoon.";
    private const string CourageDocument =
        EditedPirateDocument +
        "\n\nCourage joined the crew and offered to guide them through Mermaid Lagoon.";

    private DojoTestSession _dojo = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;

    private async Task InitializeScenarioAsync(DojoBackendKind backend)
    {
        _dojo = await GetDojoAsync(backend, DojoRecording.PredictiveStateUpdates);
        _checkpoints = _dojo.Checkpoints;

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_dojo.UI));
        _page = await context.NewPageAsync();
        await _page.GotoAsync(_dojo.GetScenarioUrl("/predictive_state_updates"));
        await _page.WaitForInteractiveAsync("[aria-label='Document editor']");
    }

    [TestMethod]
    [DojoBackends]
    public async Task DocumentEditor_StreamsPredictionAndSupportsAcceptAndReject(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend);
        var scenario = _page.Locator("[data-scenario='predictive_state_updates']");
        var editor = scenario.GetByRole(
            AriaRole.Textbox,
            new() { Name = "Document editor", Exact = true });
        var send = scenario.Locator(".sc-ai-input__send");

        await editor.FillAsync(InitialDocument);

        var piratePrompt = "Please write a story about a pirate named Candy Beard.";
        await scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Write a pirate story", Exact = true }).ClickAsync();

        await AssertReadOnlyDiffAsync(editor, PirateOpening);
        await Expect(scenario.Locator(".confirm-changes")).ToHaveCountAsync(0);
        await Expect(send).ToBeDisabledAsync();

        await _checkpoints.ReleaseAsync(piratePrompt, "pirate-opening");

        await AssertReadOnlyDiffAsync(editor, PirateDocument);
        await Expect(scenario.Locator(".confirm-changes")).ToHaveCountAsync(0);

        await _checkpoints.ReleaseAsync(piratePrompt, "pirate-complete");

        var dialog = scenario.Locator(".confirm-changes").Last;
        await Expect(dialog.Locator(".confirm-changes__message"))
            .ToHaveTextAsync("Do you want to accept the changes?");
        await dialog.GetByRole(
            AriaRole.Button,
            new() { Name = "Confirm", Exact = true }).ClickAsync();

        await _checkpoints.ReleaseAsync(piratePrompt, "before-pirate-summary");
        await Expect(scenario.Locator(
            ".sc-ai-message--assistant .sc-ai-message__content").Last)
            .ToHaveTextAsync("Candy Beard's voyage is ready.");

        await Expect(editor).ToHaveClassAsync("document-editor__input");
        await Expect(editor).ToHaveValueAsync(PirateDocument);
        await AssertNoInternalMetadataAsync(editor);

        await editor.FillAsync(EditedPirateDocument);
        var couragePrompt = "Please add a character named Courage.";
        await scenario.GetByRole(
            AriaRole.Button,
            new() { Name = "Add character", Exact = true }).ClickAsync();

        await AssertReadOnlyDiffAsync(editor, "Courage joined the crew");
        await Expect(scenario.Locator(".confirm-changes")).ToHaveCountAsync(1);

        await _checkpoints.ReleaseAsync(couragePrompt, "courage-draft");

        await AssertReadOnlyDiffAsync(editor, CourageDocument);

        await _checkpoints.ReleaseAsync(couragePrompt, "courage-complete");

        await Expect(scenario.Locator(".confirm-changes")).ToHaveCountAsync(2);
        dialog = scenario.Locator(".confirm-changes").Last;
        await dialog.GetByRole(
            AriaRole.Button,
            new() { Name = "Reject", Exact = true }).ClickAsync();

        await _checkpoints.ReleaseAsync(couragePrompt, "before-courage-summary");
        await Expect(scenario.Locator(
            ".sc-ai-message--assistant .sc-ai-message__content").Last)
            .ToHaveTextAsync("I left the document unchanged.");

        await Expect(editor).ToHaveClassAsync("document-editor__input");
        await Expect(editor).ToHaveValueAsync(EditedPirateDocument);
        await AssertNoInternalMetadataAsync(editor);
        await Expect(send).ToBeEnabledAsync();
    }

    [TestMethod]
    [TestCategory("BrowserInfrastructure")]
    public async Task CandidateTimeout_ReportsExpectedAndRenderedDocument()
    {
        var context = await NewContext();
        var page = await context.NewPageAsync();
        await page.SetContentAsync(
            """<div id="editor"><pre class="document-editor__diff">kept <s>removed</s><em>candidate</em></pre></div>""");

        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => WaitForCandidateAsync(page.Locator("#editor"), "missing document", timeout: 50));

        StringAssert.Contains(exception.Message, "Expected:\nmissing document");
        StringAssert.Contains(exception.Message, "Actual:\nkept candidate");
        Assert.IsInstanceOfType<TimeoutException>(exception.InnerException);
    }

    private static async Task AssertReadOnlyDiffAsync(ILocator editor, string expected)
    {
        await Expect(editor).ToHaveClassAsync("document-editor__surface");
        await Expect(editor).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(editor.Locator("em").First).ToBeVisibleAsync();
        await WaitForCandidateAsync(editor, expected, TestRoot.ExpectTimeout);
        Assert.IsGreaterThan(0, await editor.Locator("em").CountAsync());
        await AssertNoInternalMetadataAsync(editor);
    }

    private static async Task WaitForCandidateAsync(ILocator editor, string expected, float timeout)
    {
        await using var editorElement = await editor.ElementHandleAsync();
        Assert.IsNotNull(editorElement);
        // Releasing the model checkpoint does not wait for the circuit's render batch.
        // Retry the candidate-only projection, not the already-visible previous diff.
        try
        {
            await using var candidate = await editor.Page.WaitForFunctionAsync(
                """
                ({ editor, expected }) => {
                    const diff = editor.querySelector('.document-editor__diff');
                    if (!diff) {
                        return false;
                    }

                    const clone = diff.cloneNode(true);
                    clone.querySelectorAll('s').forEach(item => item.remove());
                    return clone.textContent.includes(expected);
                }
                """,
                new { editor = editorElement, expected },
                new() { Timeout = timeout });
        }
        catch (TimeoutException exception)
        {
            var actual = await editor.EvaluateAsync<string>(
                """
                editor => {
                    const diff = editor.querySelector('.document-editor__diff');
                    if (!diff) {
                        return '<candidate diff unavailable>';
                    }

                    const clone = diff.cloneNode(true);
                    clone.querySelectorAll('s').forEach(item => item.remove());
                    return clone.textContent;
                }
                """);
            throw new TimeoutException(
                $"The rendered candidate did not contain the expected document.\nExpected:\n{expected}\nActual:\n{actual}",
                exception);
        }
    }

    private static async Task AssertNoInternalMetadataAsync(ILocator editor)
    {
        var html = await editor.EvaluateAsync<string>("element => element.outerHTML");
        foreach (var value in new[]
        {
            "runStartDocument",
            "_runStartDocument",
            "write_document_local",
            "confirm_changes",
        })
        {
            Assert.DoesNotContain(value, html, StringComparison.OrdinalIgnoreCase);
        }
    }
}
