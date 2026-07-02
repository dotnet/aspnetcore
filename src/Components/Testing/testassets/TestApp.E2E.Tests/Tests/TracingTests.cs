// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.AspNetCore.Components.Testing.Playwright;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestApp.Components;
using TestApp.E2E.Tests.Fixtures;

namespace TestApp.E2E.Tests.Tests;

// Validates the tracing infrastructure.
[TestClass]
public class TracingTests : BrowserTest
{
    private ServerInstance _server = null!;

    [TestInitialize]
    public async Task Init()
    {
        _server = await TestRoot.Servers.StartServerAsync<App>();
    }

    [TestCleanup]
    public void AttachServerOutput() => TestContext.AttachServerOutputIfFailed(_server);

    [TestMethod]
    public async Task HomePage_WithTracing_DisplaysContent()
    {
        await using var ctx = await Browser.NewTracedContextAsync(TestContext, _server);

        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);

        await Expect(page).ToHaveTitleAsync("Home");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [TestMethod]
    public async Task Counter_WithTracing_IncrementsOnClick()
    {
        await using var ctx = await Browser.NewTracedContextAsync(TestContext, _server);
        var page = await ctx.NewPageAsync();

        await page.GotoAsync($"{_server.TestUrl}/counter");

        var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Expect(button).ToBeVisibleAsync();

        await page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = page.Locator("p[role='status']");
        await Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [TestMethod]
    public async Task TracedContext_ContextProperty_ExposesUnderlyingContext()
    {
        await using var traced = await Browser.NewTracedContextAsync(TestContext, _server);

        var context = traced.Context;
        Assert.IsNotNull(context);

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [TestMethod]
    public async Task ManualTracing_TraceAndWithArtifacts_WorkTogether()
    {
        var artifactDir = Path.Combine(
            AppContext.BaseDirectory, "test-artifacts", "manual-tracing-test");

        var context = await NewContext(
            new BrowserNewContextOptions()
                .WithServerRouting(_server)
                .WithArtifacts(artifactDir));

        await using var tracing = await context.TraceAsync(TestContext, artifactDir);

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [TestMethod]
    public async Task ArtifactDirectory_IsCreated_WhenTracingStarts()
    {
        await using var ctx = await Browser.NewTracedContextAsync(TestContext, _server);

        var testName = TestContext.TestName ?? "unknown";
        var sanitized = PlaywrightExtensions.SanitizeFileName(testName);
        var expectedDir = Path.Combine(
            AppContext.BaseDirectory, "test-artifacts", sanitized);

        Assert.IsTrue(Directory.Exists(expectedDir),
            $"Expected artifact directory to exist at: {expectedDir}");

        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
