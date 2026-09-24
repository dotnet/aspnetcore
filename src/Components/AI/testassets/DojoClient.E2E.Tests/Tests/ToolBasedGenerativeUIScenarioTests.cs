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

// The browser reaches AGUIDojoApi through DojoClient's real AGUIChatClient. Only the API model
// is recorded, so tool declaration, streamed SSE events, invocation, and continuation stay real.
[UITest]
public partial class ToolBasedGenerativeUIScenarioTests : BrowserTest
{
    private const string HaikuPrompt = "Write me a haiku about nature";

    private ServerInstance _api = null!;
    private ServerInstance _ui = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;
    private string _runId = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        _runId = Guid.NewGuid().ToString("N")[..8];
        _api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<DojoModelOverrides>(
                nameof(DojoModelOverrides.ToolBasedGenerativeUI));
        });
        _ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["AGUI_DOJO_API_URL"] = _api.AppUrl;
        });
        _checkpoints = new ApiCheckpointClient(_api);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
        await _page.GotoAsync($"{_ui.TestUrl}/tool_based_generative_ui");
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    [TestMethod]
    public async Task GenerateHaiku_RendersWhileStreamingAndNavigatesCarousel()
    {
        Assert.AreEqual(
            "linear-gradient(135deg, #667eea, #764ba2)",
            global::DojoClient.Components.Scenarios.ToolBasedGenerativeUI
                .HaikuData.NormalizeGradient(
                    "linear-gradient(135deg, #134e5e, #71b280); background: url(https://example.com)"));

        var prompt = $"{HaikuPrompt} ({_runId})";
        var carousel = _page.Locator(".tool-generative-ui__display .haiku-carousel");

        await AssertPlaceholderHaikuAsync(carousel.Locator(".haiku-card"));
        await Expect(carousel.Locator(".haiku-carousel__nav")).ToHaveCountAsync(0);

        await SendAsync(prompt);

        await AssertNatureHaikuAsync(carousel.Locator(".haiku-card"));
        await Expect(carousel.Locator(".haiku-carousel__nav")).ToHaveCountAsync(0);
        await Expect(carousel).Not.ToContainTextAsync("A placeholder verse");
        await Expect(_page.Locator(".haiku-action-status")).ToHaveTextAsync("Haiku ready");
        var transcriptCards = _page.Locator(".tool-generative-ui__chat .haiku-card");
        await Expect(transcriptCards).ToHaveCountAsync(1);
        await AssertNatureHaikuAsync(transcriptCards.First);

        var assistant = _page.Locator(
            ".sc-ai-message--assistant .sc-ai-message__content");
        await Expect(assistant).ToHaveTextAsync("Your nature haiku is ready");
        await Expect(assistant).ToHaveClassAsync(
            "sc-ai-message__content sc-ai-message__content--streaming");
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeDisabledAsync();

        await _checkpoints.ReleaseAsync(prompt, "haiku-summary-start");

        await Expect(assistant).ToHaveTextAsync(
            "Your nature haiku is ready\u2014a quiet pond awakened by a frog.");
        await Expect(assistant).ToHaveClassAsync("sc-ai-message__content");
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
        await Expect(_page.Locator(".sc-ai-typing")).ToHaveCountAsync(0);
    }

    private static async Task AssertPlaceholderHaikuAsync(ILocator card)
    {
        await Expect(card).ToHaveAttributeAsync(
            "style",
            "background: linear-gradient(135deg, #667eea, #764ba2);");
        await Expect(card.Locator(".haiku-card__japanese"))
            .ToHaveTextAsync(["\u3053\u3053\u306b\u4e00\u53e5", "\u4eee\u306e\u3046\u305f\u7f6e\u304f", "\u6625\u3092\u5f85\u3064"]);
        await Expect(card.Locator(".haiku-card__english"))
            .ToHaveTextAsync(["A placeholder verse\u2014", "Resting here for now,", "Awaiting your words."]);
    }

    private static async Task AssertNatureHaikuAsync(ILocator card)
    {
        await Expect(card).ToHaveAttributeAsync(
            "style",
            "background: linear-gradient(135deg, #134e5e, #71b280);");
        await Expect(card.Locator(".haiku-card__japanese"))
            .ToHaveTextAsync(["\u53e4\u6c60\u3084", "\u86d9\u98db\u3073\u3053\u3080", "\u6c34\u306e\u97f3"]);
        await Expect(card.Locator(".haiku-card__english"))
            .ToHaveTextAsync(["An ancient pond\u2014", "A frog leaps in,", "The sound of water."]);
        await Expect(card.Locator(".haiku-card__image"))
            .ToHaveAttributeAsync("src", "/images/ancient-pond.svg");
    }

    private async Task SendAsync(string prompt)
    {
        await _page.FillAsync("textarea.sc-ai-input__textarea", prompt);
        await _page.ClickAsync("button.sc-ai-input__send");
    }
}
