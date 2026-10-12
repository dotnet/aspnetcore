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

// Covers the canonical AG-UI "Agentic Chat" scenario across both dojo applications: the
// browser drives DojoClient, DojoClient posts to AGUIDojoApi over AG-UI, and the response is
// streamed back as Server-Sent Events. Only the model inside the API is recorded.
[UITest]
public partial class AgenticChatScenarioTests : BrowserTest
{
    private const string FirstPrompt = "Tell me about Blazor";
    private const string SecondPrompt = "And what about streaming";
    private const string BackgroundPrompt = "Change the background to something new";
    private const string Background =
        "linear-gradient(135deg, #ff9a9e, #fad0c4)";

    private ServerInstance _api = null!;
    private ServerInstance _ui = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;
    private string _runId = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        // Every test types a message that carries a unique run id, so the recorded script is
        // shared while the checkpoint gates stay isolated per test and per run.
        _runId = Guid.NewGuid().ToString("N")[..8];

        _api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            var usesClientToolRecording = TestContext.TestName?.Contains(
                "ClientTool",
                StringComparison.Ordinal) == true;
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

        _ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["AGUI_DOJO_API_URL"] = _api.AppUrl;
        });

        _checkpoints = new ApiCheckpointClient(_api);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task AgenticChat_StreamsAssistantTextIncrementally()
    {
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
    public async Task AgenticChat_CompletesTheResponse()
    {
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
    public async Task AgenticChat_KeepsBothTurnsAfterASecondMessage()
    {
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
    public async Task AgenticChat_ClientToolExecutesAndContinuesWithOneResult()
    {
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
    public async Task AgenticChat_ClientToolStateIsIsolatedPerCircuit()
    {
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
