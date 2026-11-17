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
[UITest]
public partial class TracingTests : BrowserTest
{
    private ServerInstance _server = null!;

    protected override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync();
        _server = await StartServerAsync<App>(TestRoot.Servers);
    }

    [TestMethod]
    public async Task HomePage_WithTracing_DisplaysContent()
    {
        var context = await NewTracedContextAsync(_server);

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);

        await Expect(page).ToHaveTitleAsync("Home");
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [TestMethod]
    public async Task Counter_WithTracing_IncrementsOnClick()
    {
        var context = await NewTracedContextAsync(_server);
        var page = await context.NewPageAsync();

        await page.GotoAsync($"{_server.TestUrl}/counter");

        var button = page.GetByRole(AriaRole.Button, new() { Name = "Click me" });
        await Expect(button).ToBeVisibleAsync();

        await page.WaitForInteractiveAsync("button.btn-primary");

        await button.ClickAsync();

        var countLocator = page.Locator("p[role='status']");
        await Expect(countLocator).ToHaveTextAsync("Current count: 1");
    }

    [TestMethod]
    public async Task NewTracedContext_ReturnsBrowserContext()
    {
        var context = await NewTracedContextAsync(_server);

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [TestMethod]
    public async Task ManualTracing_RegistersContextForArtifacts()
    {
        var context = await NewContext(
            new BrowserNewContextOptions()
                .WithServerRouting(_server));

        await TraceAsync(context);

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [TestMethod]
    public async Task ArtifactDirectory_IsCreated_WhenTracingStarts()
    {
        var context = await NewTracedContextAsync(_server);

        var testName = TestContext.TestName ?? "unknown";
        var expectedDir = TestArtifactDirectory.GetPath(testName);

        Assert.IsTrue(Directory.Exists(expectedDir),
            $"Expected artifact directory to exist at: {expectedDir}");

        var page = await context.NewPageAsync();
        await page.GotoAsync(_server.TestUrl);
        await Expect(page.Locator("h1")).ToHaveTextAsync("Hello, world!");
    }

    [TestMethod]
    public async Task ServerStartupFailure_CapturesOutputArtifacts()
    {
        const string stdoutMarker = "Intentional startup failure stdout";
        const string stderrMarker = "Intentional startup failure stderr";
        var artifactRoot = Path.Combine(
            TestArtifactDirectory.GetPath(TestContext.TestName ?? "unknown"),
            "server-output");
        var existingFiles = Directory.Exists(artifactRoot)
            ? Directory.GetFiles(artifactRoot, "*", SearchOption.AllDirectories).ToHashSet()
            : [];

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => StartServerAsync<App>(
                TestRoot.Servers,
                options => options.EnvironmentVariables["E2E_FAIL_ON_STARTUP"] = "1"));

        Assert.Contains("Intentional startup failure", exception.Message);

        var newFiles = Directory.GetFiles(artifactRoot, "*", SearchOption.AllDirectories)
            .Where(path => !existingFiles.Contains(path))
            .ToArray();
        Assert.HasCount(3, newFiles);
        Assert.IsTrue(newFiles.Any(path => path.EndsWith(".startup.log", StringComparison.Ordinal)));
        Assert.IsTrue(newFiles.Any(path =>
            path.EndsWith(".stdout.log", StringComparison.Ordinal) &&
            File.ReadAllText(path).Contains(stdoutMarker, StringComparison.Ordinal)));
        Assert.IsTrue(newFiles.Any(path =>
            path.EndsWith(".stderr.log", StringComparison.Ordinal) &&
            File.ReadAllText(path).Contains(stderrMarker, StringComparison.Ordinal)));
    }
}
