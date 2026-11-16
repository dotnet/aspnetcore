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
public partial class FunctionInvocationTests : BrowserTest
{
    [TestMethod]
    public async Task BuiltInMapping_RendersLoadingThenMatchingResult()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{server.TestUrl}/function-invocation");
        await page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");

        await page.FillAsync("textarea.sc-ai-input__textarea", "Show the weather");
        await page.ClickAsync("button.sc-ai-input__send");

        var card = page.Locator(".function-invocation-card");
        await Expect(card).ToHaveCountAsync(1);
        await Expect(card).ToHaveClassAsync(
            "function-invocation-card function-invocation-card--loading");
        await Expect(card.Locator(".function-invocation-card__tool"))
            .ToHaveTextAsync("get_weather");
        await Expect(card.Locator(".function-invocation-card__location"))
            .ToHaveTextAsync("Seattle");
        await Expect(card).ToHaveAttributeAsync("data-informational", "True");
        await Expect(card.Locator(".function-invocation-card__status"))
            .ToHaveTextAsync("Loading...");

        await page.Locator("button.release-function-result").ClickAsync();

        await Expect(card).ToHaveClassAsync(
            "function-invocation-card function-invocation-card--complete");
        await Expect(card.Locator(".function-invocation-card__result"))
            .ToHaveTextAsync("sunny");
        await Expect(card).ToHaveAttributeAsync("data-informational", "True");
        await Expect(card.Locator(".function-invocation-card__status")).ToHaveCountAsync(0);
    }
}
