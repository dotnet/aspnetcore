// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using AIApp.Components;
using AIApp.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AIApp.E2E.Tests.Tests;

[UITest]
public partial class RichTextTests : BrowserTest
{
    [TestMethod]
    public async Task RichText_RendersTheStructuredContentMatrix()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{server.TestUrl}/rich-text");
        await page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");

        await page.FillAsync("textarea.sc-ai-input__textarea", "Render rich text");
        await page.ClickAsync("button.sc-ai-input__send");

        var assistant = page.Locator(".sc-ai-message--assistant .sc-ai-message__content");
        await Expect(assistant.Locator("h2")).ToHaveTextAsync("Components.AI rich text");
        await Expect(assistant.Locator("strong")).ToHaveTextAsync("strong");
        await Expect(assistant.Locator("em")).ToHaveTextAsync("emphasized");
        await Expect(assistant.Locator("s")).ToHaveTextAsync("struck-through");
        await Expect(assistant.Locator("code").First).ToHaveTextAsync("C#");
        await Expect(assistant.Locator("blockquote"))
            .ToContainTextAsync("Streaming never exposes a partial tree.");
        await Expect(assistant.Locator("li")).ToHaveCountAsync(2);
        await Expect(assistant.Locator("input[type=checkbox]")).ToBeCheckedAsync();
        await Expect(assistant.Locator("pre code"))
            .ToHaveTextAsync("Console.WriteLine(\"Rich text\");");
        await Expect(assistant.Locator("table tr")).ToHaveCountAsync(2);
        await Expect(assistant.GetByRole(AriaRole.Link, new() { Name = "Components documentation" }))
            .ToHaveAttributeAsync("href", "https://learn.microsoft.com/aspnet/core/blazor/");
        await Expect(assistant.GetByAltText("Decorative rich text sample")).ToBeVisibleAsync();
        await Expect(assistant.Locator("sup")).ToHaveTextAsync("snapshot");
        await Expect(assistant).ToContainTextAsync("<mark>Encoded HTML source</mark>");
        await Expect(assistant.Locator("mark")).ToHaveCountAsync(0);
    }
}
