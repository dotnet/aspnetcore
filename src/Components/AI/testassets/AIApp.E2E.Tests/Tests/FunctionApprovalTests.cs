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
public partial class FunctionApprovalTests : BrowserTest
{
    [TestMethod]
    public async Task ServerTool_ExecutesAfterApproval()
    {
        var page = await OpenScenarioAsync();

        await SendAsync(page);

        var scenario = page.Locator(".function-approval-scenario");
        var approval = page.Locator(".sc-ai-approval");
        await Expect(approval).ToHaveCountAsync(1);
        await Expect(approval.Locator(".sc-ai-approval__tool-name"))
            .ToHaveTextAsync("get_weather");
        await Expect(approval.Locator(".sc-ai-approval__arguments"))
            .ToContainTextAsync("Seattle");
        await Expect(scenario).ToHaveAttributeAsync("data-tool-invocations", "0");
        await Expect(page.Locator("button.sc-ai-input__send")).ToBeDisabledAsync();

        await approval.GetByRole(
            AriaRole.Button,
            new() { Name = "Approve", Exact = true }).ClickAsync();

        await Expect(approval.Locator(".sc-ai-approval__status"))
            .ToHaveTextAsync("Approved");
        await Expect(scenario).ToHaveAttributeAsync("data-tool-invocations", "1");
        await Expect(page.Locator(".sc-ai-message--assistant"))
            .ToContainTextAsync("The approved weather lookup returned sunny conditions.");
        await Expect(page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
    }

    [TestMethod]
    public async Task ServerTool_DoesNotExecuteAfterRejection()
    {
        var page = await OpenScenarioAsync();

        await SendAsync(page);

        var scenario = page.Locator(".function-approval-scenario");
        var approval = page.Locator(".sc-ai-approval");
        await Expect(approval).ToHaveCountAsync(1);
        await Expect(scenario).ToHaveAttributeAsync("data-tool-invocations", "0");

        await approval.GetByRole(
            AriaRole.Button,
            new() { Name = "Reject", Exact = true }).ClickAsync();

        await Expect(approval.Locator(".sc-ai-approval__status"))
            .ToHaveTextAsync("Rejected");
        await Expect(scenario).ToHaveAttributeAsync("data-tool-invocations", "0");
        await Expect(page.Locator(".sc-ai-message--assistant"))
            .ToContainTextAsync("The weather lookup was rejected and was not executed.");
        await Expect(page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
    }

    private async Task<IPage> OpenScenarioAsync()
    {
        var server = await StartServerAsync<App>(TestRoot.Servers);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(server));
        var page = await context.NewPageAsync();
        await page.GotoAsync($"{server.TestUrl}/function-approval");
        await page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");

        return page;
    }

    private static async Task SendAsync(IPage page)
    {
        await page.FillAsync("textarea.sc-ai-input__textarea", "Show the weather");
        await page.ClickAsync("button.sc-ai-input__send");
    }
}
