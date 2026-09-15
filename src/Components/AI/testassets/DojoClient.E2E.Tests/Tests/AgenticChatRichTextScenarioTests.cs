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

[UITest]
public partial class AgenticChatRichTextScenarioTests : BrowserTest
{
    private const string PromptText = "Show a formatted Blazor overview";

    [TestMethod]
    public async Task AgenticChat_RendersFormattedAssistantResponseOverAgui()
    {
        var api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<DojoModelOverrides>(
                nameof(DojoModelOverrides.AgenticChatRichText));
        });
        var ui = await StartServerAsync<global::DojoClient.Components.App>(
            TestRoot.Servers,
            options => options.EnvironmentVariables["AGUI_DOJO_API_URL"] = api.AppUrl);
        var checkpoints = new ApiCheckpointClient(api);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(ui));
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{ui.TestUrl}/agentic_chat");
        await page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");

        var prompt = $"{PromptText} ({Guid.NewGuid():N})";
        await page.FillAsync("textarea.sc-ai-input__textarea", prompt);
        await page.ClickAsync("button.sc-ai-input__send");

        var assistant = page.Locator(".sc-ai-message--assistant .sc-ai-message__content");
        await Expect(assistant.Locator("h2")).ToHaveTextAsync("Blazor components");
        await Expect(assistant.Locator("strong")).ToHaveTextAsync("interactive UI");
        await Expect(assistant.Locator("code")).ToHaveTextAsync("C#");
        await Expect(assistant.Locator("li")).ToHaveCountAsync(0);

        await checkpoints.ReleaseAsync(prompt, "structure");

        await Expect(assistant.Locator("li")).ToHaveCountAsync(2);
        await Expect(assistant.GetByRole(AriaRole.Link, new() { Name = "Server rendering" }))
            .ToHaveAttributeAsync(
                "href",
                "https://learn.microsoft.com/aspnet/core/blazor/");
        await Expect(page.Locator(".sc-ai-message__content--streaming")).Not.ToBeVisibleAsync();
    }
}
