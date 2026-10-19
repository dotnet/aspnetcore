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
// AGUIDojoApi is replaced so the server still executes get_weather through function invocation.
[UITest]
public partial class BackendToolRenderingScenarioTests : BrowserTest
{
    private ServerInstance _api = null!;
    private ServerInstance _ui = null!;
    private IPage _page = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();

        _api = await StartServerAsync<AGUIDojoApiAssembly>(TestRoot.Servers, options =>
        {
            options.ConfigureServices<DojoModelOverrides>(
                nameof(DojoModelOverrides.BackendToolRendering));
        });
        _ui = await StartServerAsync<global::DojoClient.Components.App>(TestRoot.Servers, options =>
        {
            options.EnvironmentVariables["AGUI_DOJO_API_URL"] = _api.AppUrl;
        });

        var context = await NewContext(new BrowserNewContextOptions().WithServerRouting(_ui));
        _page = await context.NewPageAsync();
    }

    [TestMethod]
    public async Task BackendToolRendering_RendersServerWeatherResult()
    {
        await _page.GotoAsync($"{_ui.TestUrl}/backend_tool_rendering");
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
