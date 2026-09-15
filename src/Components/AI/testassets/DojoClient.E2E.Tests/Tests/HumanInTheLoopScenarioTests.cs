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
public partial class HumanInTheLoopScenarioTests : DojoTestBase
{
    private const string ApprovalPrompt = "Please plan a trip to mars in 5 steps.";
    private const string RejectionPrompt = "Please create a simple Mars mission plan.";

    private DojoTestSession _dojo = null!;
    private IPage _page = null!;

    private async Task InitializeScenarioAsync(DojoBackendKind backend)
    {
        _dojo = await GetDojoAsync(backend, DojoRecording.HumanInTheLoop);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_dojo.UI));
        _page = await context.NewPageAsync();
        await _page.GotoAsync(_dojo.GetScenarioUrl("/human_in_the_loop"));
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    [TestMethod]
    [DojoBackends]
    public async Task TaskSteps_SelectsAndApprovesBeforeContinuing(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend);
        var prompt = ApprovalPrompt;

        await SendAsync(prompt);

        var card = _page.Locator(".task-steps-card");
        var steps = card.Locator(".task-step-item");
        var checkboxes = card.GetByRole(AriaRole.Checkbox);
        await Expect(card.Locator("h2")).ToHaveTextAsync("Select steps");
        await Expect(card.Locator(".task-steps-card__count")).ToHaveTextAsync("5 / 5 selected");
        await Expect(steps).ToHaveCountAsync(5);
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeDisabledAsync();

        await checkboxes.Nth(1).ClickAsync();
        await checkboxes.Nth(3).ClickAsync();
        await Expect(card.Locator(".task-steps-card__count")).ToHaveTextAsync("3 / 5 selected");

        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Confirm", Exact = true }).ClickAsync();

        await Expect(card).ToHaveClassAsync("task-steps-card task-steps-card--responded");
        await Expect(card).ToHaveTextAsync("Accepted");
        await Expect(_page.Locator(".sc-ai-message--assistant"))
            .ToContainTextAsync(
                "I'll move forward with the selected tasks: " +
                "Define mission goals and timeline, " +
                "Select and train the astronaut crew, " +
                "Prepare communications and contingency plans.");
        await Expect(_page.Locator(".sc-ai-message--assistant"))
            .Not.ToContainTextAsync("Design and test the spacecraft");
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
    }

    [TestMethod]
    [DojoBackends]
    public async Task TaskSteps_RejectsBeforeContinuing(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend);
        var prompt = RejectionPrompt;

        await SendAsync(prompt);

        var card = _page.Locator(".task-steps-card");
        await Expect(card.Locator(".task-step-item")).ToHaveCountAsync(5);
        await card.GetByRole(
            AriaRole.Button,
            new() { Name = "Reject", Exact = true }).ClickAsync();

        await Expect(card).ToHaveClassAsync("task-steps-card task-steps-card--responded");
        await Expect(card).ToHaveTextAsync("Rejected");
        await Expect(_page.Locator(".sc-ai-message--assistant"))
            .ToContainTextAsync(
                "No tasks were selected, so I won't move forward with any proposed steps.");
        await Expect(_page.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
    }

    private async Task SendAsync(string prompt)
    {
        await _page.FillAsync("textarea.sc-ai-input__textarea", prompt);
        await _page.ClickAsync("button.sc-ai-input__send");
    }
}
