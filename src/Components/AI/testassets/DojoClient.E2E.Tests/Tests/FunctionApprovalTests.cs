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
public partial class FunctionApprovalTests : DojoTestBase
{
    [TestMethod]
    [DojoBackends]
    public async Task ServerTool_ExecutesAfterApproval(DojoBackendKind backend)
    {
        var (page, control) = await OpenScenarioAsync(backend);
        await using var _ = control;

        await SendAsync(page);

        var approval = page.Locator(".sc-ai-approval");
        await Expect(approval).ToHaveCountAsync(1);
        await Expect(approval.Locator(".sc-ai-approval__tool-name"))
            .ToHaveTextAsync("get_weather");
        await Expect(approval.Locator(".sc-ai-approval__arguments"))
            .ToContainTextAsync("Seattle");
        Assert.AreEqual(0, await control.GetInvocationCountAsync());
        await Expect(page.Locator("button.sc-ai-input__send")).ToBeDisabledAsync();

        await approval.GetByRole(
            AriaRole.Button,
            new() { Name = "Approve", Exact = true }).ClickAsync();

        await Expect(approval.Locator(".sc-ai-approval__status")).ToHaveTextAsync("Approved");
        await Expect(page.Locator(".sc-ai-message--assistant"))
            .ToContainTextAsync("The approved weather lookup returned sunny conditions.");
        Assert.AreEqual(1, await control.GetInvocationCountAsync());
        await Expect(page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
    }

    [TestMethod]
    [DojoBackends]
    public async Task ServerTool_DoesNotExecuteAfterRejection(DojoBackendKind backend)
    {
        var (page, control) = await OpenScenarioAsync(backend);
        await using var _ = control;

        await SendAsync(page);

        var approval = page.Locator(".sc-ai-approval");
        await Expect(approval).ToHaveCountAsync(1);
        Assert.AreEqual(0, await control.GetInvocationCountAsync());

        await approval.GetByRole(
            AriaRole.Button,
            new() { Name = "Reject", Exact = true }).ClickAsync();

        await Expect(approval.Locator(".sc-ai-approval__status")).ToHaveTextAsync("Rejected");
        await Expect(page.Locator(".sc-ai-message--assistant"))
            .ToContainTextAsync("The weather lookup was rejected and was not executed.");
        Assert.AreEqual(0, await control.GetInvocationCountAsync());
        await Expect(page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
    }

    private async Task<(IPage Page, FunctionScenarioClient Control)> OpenScenarioAsync(DojoBackendKind backend)
    {
        var dojo = await GetDojoAsync(backend);
        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(dojo.UI));
        var page = await context.NewPageAsync();
        await page.GotoAsync(dojo.GetScenarioUrl("/function-approval"));
        await page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
        var threadId = await page.Locator(".function-approval-scenario").GetAttributeAsync("data-thread-id");
        Assert.IsNotNull(threadId);

        return (page, new FunctionScenarioClient(dojo.Model, threadId));
    }

    private static async Task SendAsync(IPage page)
    {
        await page.FillAsync("textarea.sc-ai-input__textarea", "Show the weather");
        await page.ClickAsync("button.sc-ai-input__send");
    }
}
