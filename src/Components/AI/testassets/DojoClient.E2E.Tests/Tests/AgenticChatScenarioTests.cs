// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoClient.E2E.Tests.Fixtures;
using DojoClient.E2E.Tests.ServiceOverrides;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[UITest]
public partial class AgenticChatScenarioTests : DojoTestBase
{
    private const string FirstPrompt = "Tell me about Blazor";
    private const string SecondPrompt = "And what about streaming";
    private const string BackgroundPrompt = "Change the background to something new";
    private const string Background =
        "linear-gradient(135deg, #ff9a9e, #fad0c4)";

    private ServerInstance _ui = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;
    private string _runId = null!;

    private async Task InitializeScenarioAsync(string backend, bool usesClientToolRecording = false)
    {
        // Every test types a message that carries a unique run id, so the recorded script is
        // shared while the checkpoint gates stay isolated per test and per run.
        _runId = Guid.NewGuid().ToString("N")[..8];

        var (ui, model) = await StartDojoAsync(backend, options =>
        {
            if (usesClientToolRecording)
            {
                options.ConfigureServices<DojoModelOverrides>(
                    nameof(DojoModelOverrides.AgenticChatClientTool));
            }
            else
            {
                options.ConfigureServices<DojoModelOverrides>(
                    nameof(DojoModelOverrides.AgenticChat));
            }
        });
        _ui = ui;
        _checkpoints = new ApiCheckpointClient(model);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    [DataRow("AGUI")]
    [DataRow("Direct")]
    public async Task AgenticChat_StreamsAssistantTextIncrementally(string backend)
    {
        await InitializeScenarioAsync(backend);
        await GoToScenarioAsync();
        var prompt = Prompt(FirstPrompt);

        await SendAsync(prompt);

        await Expect(UserMessage).ToContainTextAsync(prompt);
        await Expect(AssistantMessage).ToContainTextAsync("Blazor renders");
        await Expect(AssistantMessage).Not.ToContainTextAsync("with C#.");
        await Expect(TypingIndicator).ToBeVisibleAsync();
        await Expect(_page.Locator(".sc-ai-message__content--streaming")).ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow("AGUI")]
    [DataRow("Direct")]
    public async Task AgenticChat_CompletesTheResponse(string backend)
    {
        await InitializeScenarioAsync(backend);
        await GoToScenarioAsync();
        var prompt = Prompt(FirstPrompt);

        await SendAsync(prompt);
        await Expect(AssistantMessage).ToContainTextAsync("Blazor renders");
        await _checkpoints.ReleaseAsync(prompt, "partial");

        await Expect(AssistantMessage)
            .ToContainTextAsync("Blazor renders interactive web UI with C#.");
        await Expect(TypingIndicator).Not.ToBeVisibleAsync();
        await Expect(_page.Locator(".sc-ai-message__content--streaming")).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow("AGUI")]
    [DataRow("Direct")]
    public async Task AgenticChat_KeepsBothTurnsAfterASecondMessage(string backend)
    {
        await InitializeScenarioAsync(backend);
        await GoToScenarioAsync();
        var firstPrompt = Prompt(FirstPrompt);
        var secondPrompt = Prompt(SecondPrompt);

        await SendAsync(firstPrompt);
        await Expect(AssistantMessage).ToContainTextAsync("Blazor renders");
        await _checkpoints.ReleaseAsync(firstPrompt, "partial");
        await Expect(AssistantMessage).ToContainTextAsync("with C#.");

        await SendAsync(secondPrompt);

        await Expect(_page.Locator(".sc-ai-turn")).ToHaveCountAsync(2);
        await Expect(UserMessage).ToHaveCountAsync(2);
        await Expect(AssistantMessage.Nth(1))
            .ToContainTextAsync("Streaming updates arrive token by token.");
        await Expect(AssistantMessage.Nth(0)).ToContainTextAsync("Blazor renders");
    }

    [TestMethod]
    [DataRow("AGUI")]
    [DataRow("Direct")]
    public async Task AgenticChat_ClientToolExecutesAndContinuesWithOneResult(string backend)
    {
        await InitializeScenarioAsync(backend, usesClientToolRecording: true);
        await GoToScenarioAsync();
        var prompt = Prompt(BackgroundPrompt);

        await SendAsync(prompt);

        var scenario = _page.Locator(".agentic-chat");
        await Expect(scenario).ToHaveAttributeAsync("data-background", Background);
        await Expect(_page.Locator(".agentic-chat__action-status"))
            .ToContainTextAsync("Background updated");
        await Expect(AssistantMessage)
            .ToContainTextAsync("Background changed to a sunset gradient.");
    }

    [TestMethod]
    [DataRow("AGUI")]
    [DataRow("Direct")]
    public async Task AgenticChat_ClientToolStateIsIsolatedPerCircuit(string backend)
    {
        await InitializeScenarioAsync(backend, usesClientToolRecording: true);
        await GoToScenarioAsync();

        var secondContext = await NewContext(
            new BrowserNewContextOptions().WithServerRouting(_ui));
        var secondPage = await secondContext.NewPageAsync();
        await secondPage.GotoAsync($"{_ui.TestUrl}/agentic_chat");
        await secondPage.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");

        await SendAsync(Prompt(BackgroundPrompt));

        await Expect(_page.Locator(".agentic-chat"))
            .ToHaveAttributeAsync("data-background", Background);
        Assert.IsNull(await secondPage.Locator(".agentic-chat")
            .GetAttributeAsync("data-background"));
    }

    private ILocator UserMessage => _page.Locator(".sc-ai-message--user .sc-ai-message__content");

    private ILocator AssistantMessage => _page.Locator(".sc-ai-message--assistant .sc-ai-message__content");

    private ILocator TypingIndicator => _page.Locator(".sc-ai-typing");

    private string Prompt(string prompt) => $"{prompt} ({_runId})";

    private async Task GoToScenarioAsync()
    {
        await _page.GotoAsync($"{_ui.TestUrl}/agentic_chat");
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    private async Task SendAsync(string prompt)
    {
        await _page.FillAsync("textarea.sc-ai-input__textarea", prompt);
        await _page.ClickAsync("button.sc-ai-input__send");
    }

}
