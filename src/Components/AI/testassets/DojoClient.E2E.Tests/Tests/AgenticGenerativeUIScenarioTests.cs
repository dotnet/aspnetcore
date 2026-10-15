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

// The browser and DojoClient use the real AG-UI HTTP/SSE transport. Only the API model is
// recorded so snapshots, JSON Patch deltas, and typed state mapping all cross the wire.
[UITest]
public partial class AgenticGenerativeUIScenarioTests : BrowserTest
{
    private const string SimplePlanPrompt = "Please build a plan to go to mars in 5 steps.";

    private static readonly string[] s_stepDescriptions =
    [
        "Develop a comprehensive mission plan, detailing objectives, budget, and timeline.",
        "Design and test a spacecraft capable of transporting humans and cargo to Mars.",
        "Select and train astronaut crew for the mission.",
        "Establish communication systems and infrastructure for Mars exploration.",
        "Launch the spacecraft and execute the mission to Mars.",
    ];

    private ServerInstance _api = null!;
    private ServerInstance _ui = null!;
    private ApiCheckpointClient _checkpoints = null!;
    private IPage _page = null!;
    private string _prompt = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        _prompt = SimplePlanPrompt;
        _api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<DojoModelOverrides>(
                nameof(DojoModelOverrides.AgenticGenerativeUI));
        });
        _ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["AGUI_DOJO_API_URL"] = _api.AppUrl;
        });
        _checkpoints = new ApiCheckpointClient(_api);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
        await _page.GotoAsync($"{_ui.TestUrl}/agentic_generative_ui");
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    [TestMethod]
    public async Task PlanTask_StreamsSnapshotAndEachDeltaBeforeCompleting()
    {
        var scenario = _page.Locator("[data-scenario='agentic_generative_ui']");
        var activity = scenario.Locator(".plan-activity");
        var send = scenario.Locator("button.sc-ai-input__send");
        var suggestions = scenario.Locator(".plan-suggestion");

        await Expect(activity).ToHaveCountAsync(0);
        await Expect(suggestions).ToHaveCountAsync(2);
        await Expect(suggestions.Nth(0)).ToContainTextAsync("Simple plan");
        await Expect(suggestions.Nth(1)).ToContainTextAsync("Complex plan");
        await suggestions.Nth(0).ClickAsync();

        await AssertPlanAsync(activity, completedCount: 0, isRunning: true);
        await Expect(suggestions).ToHaveCountAsync(0);
        await Expect(send).ToBeDisabledAsync();

        for (var completedCount = 1; completedCount <= s_stepDescriptions.Length; completedCount++)
        {
            await _checkpoints.ReleaseAsync(_prompt, $"before-step-{completedCount}");
            await AssertPlanAsync(activity, completedCount, isRunning: true);
            await Expect(send).ToBeDisabledAsync();
        }

        await _checkpoints.ReleaseAsync(_prompt, "before-summary");

        var assistant = scenario.Locator(
            ".sc-ai-message--assistant .sc-ai-message__content");
        await Expect(assistant).ToHaveTextAsync(
            "All five steps in the Mars mission plan are complete.");
        await Expect(assistant).ToHaveClassAsync(
            "sc-ai-message__content sc-ai-message__content--streaming");
        await AssertPlanAsync(activity, completedCount: 5, isRunning: true);

        await _checkpoints.ReleaseAsync(_prompt, "summary-complete");

        await AssertPlanAsync(activity, completedCount: 5, isRunning: false);
        await Expect(assistant).ToHaveClassAsync("sc-ai-message__content");
        await Expect(send).ToBeEnabledAsync();
    }

    private static async Task AssertPlanAsync(
        ILocator activity,
        int completedCount,
        bool isRunning)
    {
        await Expect(activity).ToHaveCountAsync(1);
        await Expect(activity).ToHaveAttributeAsync(
            "data-task-status",
            isRunning ? "running" : "done");
        await Expect(activity.Locator(".plan-activity__status"))
            .ToHaveTextAsync(isRunning ? "Running" : "Done");

        var card = activity.Locator(".plan-progress-card");
        await Expect(card.Locator(".plan-progress-card__count"))
            .ToHaveTextAsync($"{completedCount}/{s_stepDescriptions.Length} Complete");
        await Expect(card.Locator(".plan-progress-card__bar-fill"))
            .ToHaveAttributeAsync("style", $"width: {completedCount * 20}%");
        await Expect(card.Locator(".plan-progress-card__bar-shimmer")).ToHaveCountAsync(1);

        var steps = card.Locator(".plan-step");
        await Expect(steps).ToHaveCountAsync(s_stepDescriptions.Length);
        for (var index = 0; index < s_stepDescriptions.Length; index++)
        {
            var step = steps.Nth(index);
            await Expect(step.Locator(".plan-step__description"))
                .ToHaveTextAsync(s_stepDescriptions[index]);

            var expectedState = index < completedCount
                ? "completed"
                : completedCount < s_stepDescriptions.Length && index == completedCount
                    ? "current"
                    : "pending";
            await Expect(step).ToHaveAttributeAsync("data-step-state", expectedState);
            await Expect(step).ToHaveClassAsync($"plan-step plan-step--{expectedState}");
            await Expect(step.Locator(".plan-step__processing"))
                .ToHaveCountAsync(expectedState == "current" ? 1 : 0);
        }
    }
}

[UITest]
public partial class AgenticGenerativeUISuggestionTests : BrowserTest
{
    private const string ComplexPlanPrompt = "Please build a plan to go to make pizza in 10 steps.";

    private ServerInstance _api = null!;
    private ServerInstance _ui = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        _api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["OPENAI_BASE_URL"] = "";
            options.EnvironmentVariables["OPENAI_API_KEY"] = "";
        });
        _ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["AGUI_DOJO_API_URL"] = _api.AppUrl;
        });

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
        await _page.GotoAsync($"{_ui.TestUrl}/agentic_generative_ui");
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");
    }

    [TestMethod]
    public async Task ComplexPlanSuggestion_SubmitsTenStepPizzaPlan()
    {
        var scenario = _page.Locator("[data-scenario='agentic_generative_ui']");
        var suggestion = scenario.Locator(".plan-suggestion").Filter(
            new LocatorFilterOptions { HasText = "Complex plan" });

        await Expect(suggestion).ToHaveCountAsync(1);
        await suggestion.ClickAsync();

        await Expect(scenario.Locator(".sc-ai-message--user"))
            .ToContainTextAsync(ComplexPlanPrompt);
        await Expect(scenario.Locator(".plan-step")).ToHaveCountAsync(10);
        await Expect(scenario.Locator(".plan-progress-card__count"))
            .ToHaveTextAsync("10/10 Complete", new() { Timeout = 20_000 });
        await Expect(scenario.Locator(".sc-ai-message--assistant .sc-ai-message__content"))
            .ToHaveTextAsync(
                "All 10 steps in the pizza plan are complete.",
                new() { Timeout = 20_000 });
        await Expect(scenario.Locator("button.sc-ai-input__send")).ToBeEnabledAsync();
    }
}
