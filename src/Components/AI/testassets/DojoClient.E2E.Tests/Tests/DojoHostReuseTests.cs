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
public partial class DojoHostReuseTests : DojoTestBase
{
    [TestMethod]
    [DojoBackends]
    public async Task SharedHosts_IsolateRecordingsAndIdenticalPromptCheckpoints(DojoBackendKind backend)
    {
        var first = await GetDojoAsync(backend, DojoRecording.AgenticChat);
        var second = await GetDojoAsync(backend, DojoRecording.AgenticChat);
        var formatted = await GetDojoAsync(backend, DojoRecording.AgenticChatRichText);
        Assert.AreSame(first.UI, second.UI);
        Assert.AreSame(first.Model, second.Model);
        Assert.AreSame(first.UI, formatted.UI);
        Assert.AreSame(first.Model, formatted.Model);

        var firstPage = await OpenChatAsync(first);
        var secondPage = await OpenChatAsync(second);
        const string prompt = "Tell me about Blazor";
        await SendAsync(firstPage, prompt);
        await SendAsync(secondPage, prompt);

        var firstResponse = firstPage.Locator(".sc-ai-message--assistant .sc-ai-message__content");
        var secondResponse = secondPage.Locator(".sc-ai-message--assistant .sc-ai-message__content");
        await Expect(firstResponse).ToContainTextAsync("Blazor renders");
        await Expect(secondResponse).ToContainTextAsync("Blazor renders");

        await first.Checkpoints.ReleaseAsync(prompt, "partial");
        await Expect(firstResponse).ToContainTextAsync("with C#.");
        await Expect(firstPage.Locator(".sc-ai-message__content--streaming")).ToHaveCountAsync(0);
        Assert.IsFalse(await second.Checkpoints.IsReleasedAsync(prompt, "partial"));
        await Expect(secondResponse).Not.ToContainTextAsync("with C#.");
        await Expect(secondPage.Locator(".sc-ai-typing")).ToBeVisibleAsync();

        await second.Checkpoints.ReleaseAsync(prompt, "partial");
        await Expect(secondResponse).ToContainTextAsync("with C#.");

        var formattedPage = await OpenChatAsync(formatted);
        const string formattedPrompt = "Show a formatted Blazor overview";
        await SendAsync(formattedPage, formattedPrompt);
        var formattedResponse = formattedPage.Locator(".sc-ai-message--assistant .sc-ai-message__content");
        await Expect(formattedResponse.Locator("h2")).ToHaveTextAsync("Blazor components");
        await Expect(formattedResponse.Locator("li")).ToHaveCountAsync(0);
        await formatted.Checkpoints.ReleaseAsync(formattedPrompt, "structure");
        await Expect(formattedResponse.Locator("li")).ToHaveCountAsync(2);
        await Expect(formattedPage.Locator(".sc-ai-message__content--streaming")).ToHaveCountAsync(0);
    }

    private async Task<IPage> OpenChatAsync(DojoTestSession session)
    {
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(session.UI));
        var page = await context.NewPageAsync();
        await page.GotoAsync(session.GetScenarioUrl("/agentic_chat"));
        await page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");

        return page;
    }

    private static async Task SendAsync(IPage page, string prompt)
    {
        await page.Locator("textarea.sc-ai-input__textarea").FillAsync(prompt);
        await page.Locator("button.sc-ai-input__send").ClickAsync();
    }
}
