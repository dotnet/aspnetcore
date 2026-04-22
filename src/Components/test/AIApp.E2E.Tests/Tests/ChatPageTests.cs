// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using AIApp.Components;
using AIApp.E2E.Tests.Fixtures;
using AIApp.E2E.Tests.ServiceOverrides;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace AIApp.E2E.Tests.Tests;

[Collection(nameof(E2ECollection))]
public class ChatPageTests : BrowserTest
{
    private const string SendButtonSelector = ".sc-ai-input__send";
    private const string TextareaSelector = ".sc-ai-input__textarea";
    private const string TurnSelector = ".sc-ai-turn";
    private readonly ServerFixture<E2ETestAssembly> _fixture;

    public ChatPageTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ChatPage_RendersInputAndSendButton()
    {
        var server = await _fixture.StartServerAsync<App>();
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

    [Fact]
    public async Task SingleTurn_DisplaysAssistantResponse()
    {
        var server = await _fixture.StartServerAsync<App>(options =>
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

    [Fact]
    public async Task SingleTurn_DisplaysUserMessage()
    {
        var server = await _fixture.StartServerAsync<App>(options =>
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

    [Fact]
    public async Task MultiTokenStreaming_AssemblesFullResponse()
    {
        var server = await _fixture.StartServerAsync<App>(options =>
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

    [Fact]
    public async Task MultiTurn_RendersMultipleConversationTurns()
    {
        var server = await _fixture.StartServerAsync<App>(options =>
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

        // Verify multiple turns are rendered
        var turns = page.Locator(TurnSelector);
        await Expect(turns).ToHaveCountAsync(4); // 2 user turns + 2 assistant turns
    }

    [Fact]
    public async Task SendMessage_ClearsTextarea()
    {
        var server = await _fixture.StartServerAsync<App>(options =>
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

    [Fact]
    public async Task EmptyMessage_DoesNotSend()
    {
        var server = await _fixture.StartServerAsync<App>();
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
