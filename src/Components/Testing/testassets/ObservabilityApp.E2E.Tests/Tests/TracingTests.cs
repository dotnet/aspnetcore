// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using ObservabilityApp.Components;
using ObservabilityApp.E2E.Tests.Fixtures;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace ObservabilityApp.E2E.Tests.Tests;

// Validates the tracing infrastructure:
//   - NewTracedContextAsync creates a traced browser context
//   - TracedContext.Context exposes the underlying IBrowserContext
//   - Lower-level WithArtifacts + TraceAsync APIs work together
//   - Artifact directories are created when tracing starts
//   - Counter interactivity works within a traced context
[Collection(nameof(E2ECollection))]
public class TracingTests : BrowserTest
{
    private readonly ServerFixture<E2ETestAssembly> _fixture;
    private ServerInstance _server = null!;

    public TracingTests(ServerFixture<E2ETestAssembly> fixture)
    {
        _fixture = fixture;
    }

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        _server = await _fixture.StartServerAsync<App>();
    }

    [Fact]
    public async Task HomePage_WithTracing_DisplaysContent()
    {
        // Use the convenience method — creates context with tracing + server routing
        await using var ctx = await this.NewTracedContextAsync(_server);

        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);

        await Expect(page).ToHaveTitleAsync("Home");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [Fact]
    public async Task Counter_WithTracing_IncrementsOnClick()
    {
        await using var ctx = await this.NewTracedContextAsync(_server);
        var page = await ctx.NewPageAsync();

        await page.GotoAsync($"{_server.TestUrl}/counter");

        var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Expect(button).ToBeVisibleAsync();

        // Wait for interactivity
        await page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = page.Locator("p[role='status']");
        await Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [Fact]
    public async Task TracedContext_ContextProperty_ExposesUnderlyingContext()
    {
        await using var traced = await this.NewTracedContextAsync(_server);

        // Verify the Context property exposes the IBrowserContext
        var context = traced.Context;
        Assert.NotNull(context);

        // Verify we can use the underlying context directly for full API access
        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [Fact]
    public async Task ManualTracing_TraceAndWithArtifacts_WorkTogether()
    {
        // Test the lower-level APIs (WithArtifacts + TraceAsync) separately
        var artifactDir = Path.Combine(
            AppContext.BaseDirectory, "test-artifacts", "manual-tracing-test");

        var context = await NewContext(
            new BrowserNewContextOptions()
                .WithServerRouting(_server)
                .WithArtifacts(artifactDir));

        await using var tracing = await context.TraceAsync(artifactDir);

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [Fact]
    public async Task ArtifactDirectory_IsCreated_WhenTracingStarts()
    {
        await using var ctx = await this.NewTracedContextAsync(_server);

        // The artifact directory should exist immediately after tracing starts
        var testName = TestContext.Current.Test?.TestDisplayName ?? "unknown";
        var sanitized = PlaywrightExtensions.SanitizeFileName(testName);
        var expectedDir = Path.Combine(
            AppContext.BaseDirectory, "test-artifacts", sanitized);

        Assert.True(Directory.Exists(expectedDir),
            $"Expected artifact directory to exist at: {expectedDir}");

        // Navigate to generate some trace content
        var page = await ctx.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }
}
