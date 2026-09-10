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
public partial class AgenticChatScenarioTests : DojoTestBase
{
    private const string FirstPrompt = "Tell me about Blazor";
    private const string SecondPrompt = "And what about streaming";
    private const string BackgroundPrompt = "Change the background to something new";
    private const string Background =
        "linear-gradient(135deg, #ff9a9e, #fad0c4)";

    private DojoTestSession _dojo = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;

    private async Task InitializeScenarioAsync(DojoBackendKind backend, bool usesClientToolRecording = false)
    {
        _dojo = await GetDojoAsync(backend, usesClientToolRecording
            ? DojoRecording.AgenticChatClientTool
            : DojoRecording.AgenticChat);
        _checkpoints = _dojo.Checkpoints;

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_dojo.UI));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    [DojoBackends]
    public async Task AgenticChat_StreamsAssistantTextIncrementally(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend);
        await GoToScenarioAsync();
        var prompt = FirstPrompt;

        await SendAsync(prompt);

        await Expect(UserMessage).ToContainTextAsync(prompt);
        await Expect(AssistantMessage).ToContainTextAsync("Blazor renders");
        await Expect(AssistantMessage).Not.ToContainTextAsync("with C#.");
        await Expect(TypingIndicator).ToBeVisibleAsync();
        await Expect(_page.Locator(".sc-ai-message__content--streaming")).ToBeVisibleAsync();
    }

    [TestMethod]
    [DojoBackends]
    public async Task AgenticChat_CompletesTheResponse(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend);
        await GoToScenarioAsync();
        var prompt = FirstPrompt;

        await SendAsync(prompt);
        await Expect(AssistantMessage).ToContainTextAsync("Blazor renders");
        await _checkpoints.ReleaseAsync(prompt, "partial");

        await Expect(AssistantMessage)
            .ToContainTextAsync("Blazor renders interactive web UI with C#.");
        await Expect(TypingIndicator).Not.ToBeVisibleAsync();
        await Expect(_page.Locator(".sc-ai-message__content--streaming")).Not.ToBeVisibleAsync();
    }

    [TestMethod]
    [DojoBackends]
    public async Task AgenticChat_KeepsBothTurnsAfterASecondMessage(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend);
        await GoToScenarioAsync();
        var firstPrompt = FirstPrompt;
        var secondPrompt = SecondPrompt;

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
    [DojoBackends]
    public async Task AgenticChat_ClientToolExecutesAndContinuesWithOneResult(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend, usesClientToolRecording: true);
        await GoToScenarioAsync();
        var prompt = BackgroundPrompt;

        await SendAsync(prompt);

        var scenario = _page.Locator(".agentic-chat");
        await Expect(scenario).ToHaveAttributeAsync("data-background", Background);
        await Expect(_page.Locator(".agentic-chat__action-status"))
            .ToContainTextAsync("Background updated");
        await Expect(AssistantMessage)
            .ToContainTextAsync("Background changed to a sunset gradient.");
    }

    [TestMethod]
    [DojoBackends]
    public async Task AgenticChat_ClientToolStateIsIsolatedPerCircuit(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend, usesClientToolRecording: true);
        await GoToScenarioAsync();

        var secondContext = await NewContext(
            new BrowserNewContextOptions().WithServerRouting(_dojo.UI));
        var secondPage = await secondContext.NewPageAsync();
        await secondPage.GotoAsync(_dojo.GetScenarioUrl("/agentic_chat"));
        await secondPage.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");

        await SendAsync(BackgroundPrompt);

        await Expect(_page.Locator(".agentic-chat"))
            .ToHaveAttributeAsync("data-background", Background);
        Assert.IsNull(await secondPage.Locator(".agentic-chat")
            .GetAttributeAsync("data-background"));
    }

    private ILocator UserMessage => _page.Locator(".sc-ai-message--user .sc-ai-message__content");

    private ILocator AssistantMessage => _page.Locator(".sc-ai-message--assistant .sc-ai-message__content");

    private ILocator TypingIndicator => _page.Locator(".sc-ai-typing");

    private async Task GoToScenarioAsync()
    {
        await _page.GotoAsync(_dojo.GetScenarioUrl("/agentic_chat"));
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    private async Task SendAsync(string prompt)
    {
        await _page.FillAsync("textarea.sc-ai-input__textarea", prompt);
        await _page.ClickAsync("button.sc-ai-input__send");
    }

}
