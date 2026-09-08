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

// The browser and DojoClient use the real AG-UI HTTP/SSE transport. Only the model inside
// AGUIDojoApi is replaced so task selection and continuation cross the protocol boundary.
[UITest]
public partial class HumanInTheLoopScenarioTests : BrowserTest
{
    private const string ApprovalPrompt = "Please plan a trip to mars in 5 steps.";
    private const string RejectionPrompt = "Please create a simple Mars mission plan.";

    private ServerInstance _api = null!;
    private ServerInstance _ui = null!;
    private IPage _page = null!;
    private string _runId = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        _runId = Guid.NewGuid().ToString("N")[..8];
        _api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<DojoModelOverrides>(
                nameof(DojoModelOverrides.HumanInTheLoop));
        });
        _ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["AGUI_DOJO_API_URL"] = _api.AppUrl;
        });

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
        await _page.GotoAsync($"{_ui.TestUrl}/human_in_the_loop");
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    [TestMethod]
    public async Task TaskSteps_SelectsAndApprovesBeforeContinuing()
    {
        var prompt = Prompt(ApprovalPrompt);

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
    public async Task TaskSteps_RejectsBeforeContinuing()
    {
        var prompt = Prompt(RejectionPrompt);

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

    private string Prompt(string prompt) => $"{prompt} ({_runId})";

    private async Task SendAsync(string prompt)
    {
        await _page.FillAsync("textarea.sc-ai-input__textarea", prompt);
        await _page.ClickAsync("button.sc-ai-input__send");
    }
}
