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
public partial class BackendToolRenderingScenarioTests : DojoTestBase
{
    private DojoTestSession _dojo = null!;
    private IPage _page = null!;

    private async Task InitializeScenarioAsync(DojoBackendKind backend)
    {
        _dojo = await GetDojoAsync(backend, DojoRecording.BackendToolRendering);

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_dojo.UI));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    [DojoBackends]
    public async Task BackendToolRendering_RendersServerWeatherResult(DojoBackendKind backend)
    {
        await InitializeScenarioAsync(backend);
        await _page.GotoAsync(_dojo.GetScenarioUrl("/backend_tool_rendering"));
        await _page.WaitForInteractiveAsync("textarea.sc-ai-input__textarea");

        await _page.FillAsync(
            "textarea.sc-ai-input__textarea",
            "What is the weather in San Francisco?");
        await _page.ClickAsync("button.sc-ai-input__send");

        var weatherCard = _page.Locator(".weather-card");
        await Expect(weatherCard).ToHaveCountAsync(1);
        await Expect(_page.Locator(".weather-card--loading")).ToHaveCountAsync(0);
        await Expect(weatherCard.Locator(".weather-card__location"))
            .ToHaveTextAsync("San Francisco");
        await Expect(weatherCard.Locator(".weather-card__temp-value")).ToHaveTextAsync("20");
        await Expect(weatherCard.Locator(".weather-card__condition")).ToHaveTextAsync("sunny");
        await Expect(_page.Locator(".sc-ai-message--assistant"))
            .ToContainTextAsync(
                "The weather in San Francisco is sunny with a temperature of 20\u00b0C.");
    }
}
