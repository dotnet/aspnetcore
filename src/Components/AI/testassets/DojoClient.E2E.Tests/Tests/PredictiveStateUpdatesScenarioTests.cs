// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AGUIDojoApi;
using DojoClient.E2E.Tests.Fixtures;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

// Only the API model is recorded. DojoClient still crosses HTTP/SSE through AGUIChatClient.
[UITest]
public partial class PredictiveStateUpdatesScenarioTests : BrowserTest
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

    private ServerInstance _api = null!;
    private ServerInstance _ui = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        _api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<DojoModelOverrides>(
                nameof(DojoModelOverrides.PredictiveStateUpdates));
        });
        _ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["AGUI_DOJO_API_URL"] = _api.AppUrl;
        });
        _checkpoints = new ApiCheckpointClient(_api);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
        await _page.GotoAsync($"{_ui.TestUrl}/predictive_state_updates");
        await _page.WaitForInteractiveAsync("[aria-label='Document editor']");
    }

    [TestMethod]
    public async Task DocumentEditor_StreamsPredictionAndSupportsAcceptAndReject()
    {
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

    private static async Task AssertReadOnlyDiffAsync(ILocator editor, string expected)
    {
        await Expect(editor).ToHaveClassAsync("document-editor__surface");
        await Expect(editor).ToHaveAttributeAsync("aria-readonly", "true");
        await Expect(editor.Locator("em").First).ToBeVisibleAsync();
        var proposedDocument = await editor.Locator(".document-editor__diff")
            .EvaluateAsync<string>(
                """
                element => {
                    const clone = element.cloneNode(true);
                    clone.querySelectorAll('s').forEach(item => item.remove());
                    return clone.textContent;
                }
                """);
        StringAssert.Contains(proposedDocument, expected);
        Assert.IsGreaterThan(0, await editor.Locator("em").CountAsync());
        await AssertNoInternalMetadataAsync(editor);
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
