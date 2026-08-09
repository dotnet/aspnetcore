// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using AIApp.Components;
using AIApp.E2E.Tests.Fixtures;
using AIApp.E2E.Tests.ServiceOverrides;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIApp.E2E.Tests.Tests;

[UITest]
public partial class ChatPageTests : BrowserTest
{
    private const string SendButtonSelector = ".sc-ai-input__send";
    private const string TextareaSelector = ".sc-ai-input__textarea";
    private const string TurnSelector = ".sc-ai-turn";
    [TestMethod]
    public async Task ChatPage_RendersInputAndSendButton()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{server.TestUrl}/chat");
        await page.WaitForInteractiveAsync(SendButtonSelector);

        var textarea = page.Locator(TextareaSelector);
        await Expect(textarea).ToBeVisibleAsync();

        var sendButton = page.Locator(SendButtonSelector);
        await Expect(sendButton).ToBeVisibleAsync();
        await Expect(sendButton).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task SingleTurn_DisplaysAssistantResponse()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.SingleTurnEcho));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{server.TestUrl}/chat");
        await page.WaitForInteractiveAsync(SendButtonSelector);

        // Type a message and send
        var textarea = page.Locator(TextareaSelector);
        await textarea.FillAsync("Hello");
        await page.Locator(SendButtonSelector).ClickAsync();

        // Wait for the assistant response to appear
        var responseBlock = page.Locator(".sc-ai-message__content",
            new() { HasText = "Hello! I'm your AI assistant. How can I help you today?" });
        await Expect(responseBlock).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task SingleTurn_DisplaysUserMessage()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.SingleTurnEcho));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{server.TestUrl}/chat");
        await page.WaitForInteractiveAsync(SendButtonSelector);

        // Type and send a message
        await page.Locator(TextareaSelector).FillAsync("Hello");
        await page.Locator(SendButtonSelector).ClickAsync();

        // The user's message should appear in a turn
        var userBlock = page.Locator(".sc-ai-turn--user .sc-ai-message__content",
            new() { HasText = "Hello" }).First;
        await Expect(userBlock).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task MultiTokenStreaming_AssemblesFullResponse()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.MultiTokenStreaming));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{server.TestUrl}/chat");
        await page.WaitForInteractiveAsync(SendButtonSelector);

        await page.Locator(TextareaSelector).FillAsync("Test streaming");
        await page.Locator(SendButtonSelector).ClickAsync();

        // The full streamed response text should appear once streaming completes
        var responseBlock = page.Locator(".sc-ai-message__content",
            new() { HasText = "This is a streamed response" });
        await Expect(responseBlock).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task MultiTurn_RendersMultipleConversationTurns()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.MultiTurn));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{server.TestUrl}/chat");
        await page.WaitForInteractiveAsync(SendButtonSelector);

        // First turn
        await page.Locator(TextareaSelector).FillAsync("Hi");
        await page.Locator(SendButtonSelector).ClickAsync();

        var firstResponse = page.Locator(".sc-ai-message__content",
            new() { HasText = "Hello! How can I help you?" });
        await Expect(firstResponse).ToBeVisibleAsync();

        // Second turn
        await page.Locator(TextareaSelector).FillAsync("Thanks");
        await page.Locator(SendButtonSelector).ClickAsync();

        var secondResponse = page.Locator(".sc-ai-message__content",
            new() { HasText = "You're welcome! Let me know if you need anything else." });
        await Expect(secondResponse).ToBeVisibleAsync();

        // Each conversation turn (a user message plus its assistant response) renders as a
        // single .sc-ai-turn element; user/assistant differentiation is at the message level.
        var turns = page.Locator(TurnSelector);
        await Expect(turns).ToHaveCountAsync(2); // 2 exchanges = 2 turns

        await Expect(page.Locator(".sc-ai-message--user")).ToHaveCountAsync(2);
        await Expect(page.Locator(".sc-ai-message--assistant")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task SendMessage_ClearsTextarea()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<ChatClientOverrides>(
                nameof(ChatClientOverrides.SingleTurnEcho));
        });
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{server.TestUrl}/chat");
        await page.WaitForInteractiveAsync(SendButtonSelector);

        var textarea = page.Locator(TextareaSelector);
        await textarea.FillAsync("Some text");
        await page.Locator(SendButtonSelector).ClickAsync();

        // After sending, textarea should be cleared
        await Expect(textarea).ToHaveValueAsync("");
    }

    [TestMethod]
    public async Task EmptyMessage_DoesNotSend()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{server.TestUrl}/chat");
        await page.WaitForInteractiveAsync(SendButtonSelector);

        // Click send with empty textarea
        await page.Locator(SendButtonSelector).ClickAsync();

        // No turns should appear
        var turns = page.Locator(TurnSelector);
        await Expect(turns).ToHaveCountAsync(0);
    }
}
