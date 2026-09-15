// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using DojoAgent;
using DojoClient.E2E.Tests.Fixtures;
using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DojoClient.E2E.Tests.Tests;

[UITest]
public partial class FunctionInvocationTests : DojoTestBase
{
    [TestMethod]
    public async Task BuiltInMapping_RendersLoadingThenMatchingResult()
    {
        // AGUIChatClient buffers calls until a result or interrupt; this checks pre-result informational rendering.
        var dojo = await GetDojoAsync(DojoBackendKind.Direct);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(dojo.UI));
        var page = await context.NewPageAsync();
        await page.GotoAsync(dojo.GetScenarioUrl("/function-invocation"));
        await page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
        var threadId = await page.Locator(".function-invocation-scenario").GetAttributeAsync("data-thread-id");
        Assert.IsNotNull(threadId);
        await using var control = new FunctionScenarioClient(dojo.Model, threadId);

        await page.FillAsync("textarea.sc-ai-input__textarea", "Show the weather");
        await page.ClickAsync("button.sc-ai-input__send");

        var card = page.Locator(".function-invocation-card");
        await Expect(card).ToHaveCountAsync(1);
        await Expect(card).ToHaveClassAsync(
            "function-invocation-card function-invocation-card--loading");
        await Expect(card.Locator(".function-invocation-card__tool")).ToHaveTextAsync("get_weather");
        await Expect(card.Locator(".function-invocation-card__location")).ToHaveTextAsync("Seattle");
        await Expect(card).ToHaveAttributeAsync("data-informational", "True");
        await Expect(card.Locator(".function-invocation-card__status")).ToHaveTextAsync("Loading...");

        await page.Locator("button.release-function-result").ClickAsync();

        await Expect(card).ToHaveClassAsync(
            "function-invocation-card function-invocation-card--complete");
        await Expect(card.Locator(".function-invocation-card__result")).ToHaveTextAsync("sunny");
        await Expect(card).ToHaveAttributeAsync("data-informational", "True");
        await Expect(card.Locator(".function-invocation-card__status")).ToHaveCountAsync(0);
        await Expect(page.Locator(".sc-ai-message--assistant"))
            .ToContainTextAsync("The weather in Seattle is sunny.");
        Assert.AreEqual(1, await control.GetInvocationCountAsync());
    }
}
