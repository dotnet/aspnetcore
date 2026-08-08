// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;
using Microsoft.Playwright.TestAdapter;

namespace Microsoft.AspNetCore.Components.Testing.Playwright;

/// <summary>
/// Base class for MSTest tests that need a Playwright <see cref="IBrowser"/>. Mirrors the
/// shape of <c>Microsoft.Playwright.MSTest.BrowserTest</c> without taking a dependency on
/// that package.
/// </summary>
/// <remarks>
/// <para>
/// One <see cref="IBrowser"/> is created per test assembly (lazily, on first use) and
/// shared across every test. Browser type and launch options are configured using
/// Playwright's environment variables and runsettings support.
/// </para>
/// <para>
/// Use <see cref="NewContext"/> to create a per-test <see cref="IBrowserContext"/>; every
/// context obtained that way is tracked and closed automatically when the per-test instance
/// is disposed. MSTest runs tests sequentially by default, and contexts remain isolated
/// per test if the consumer opts into parallel execution.
/// </para>
/// </remarks>
public abstract class BrowserTest : PlaywrightTest
{
    private static BrowserState? s_browserState;
    private static readonly SemaphoreSlim s_browserInitLock = new(1, 1);

    /// <summary>The shared <see cref="IBrowser"/>. Initialized on first use by <see cref="EnsureBrowserAsync"/>.</summary>
    public IBrowser Browser =>
        s_browserState?.Browser ?? throw new InvalidOperationException(
            $"Browser has not been initialized. {nameof(EnsureBrowserAsync)} is called automatically " +
            "by the BrowserTest initialization hook; ensure your derived class calls base.InitializeCoreAsync().");

    /// <summary>Gets the configured browser type shared by all tests in the test assembly.</summary>
    public string BrowserName { get; private set; } = null!;

    /// <summary>
    /// Returns the shared <see cref="IBrowser"/>, creating it on first call. Safe to
    /// invoke concurrently — initialization is serialized.
    /// </summary>
    public async Task<IBrowser> EnsureBrowserAsync()
    {
        var state = s_browserState;
        if (state is not null)
        {
            BrowserName = state.BrowserName;
            return state.Browser;
        }

        var pw = await EnsurePlaywrightAsync().ConfigureAwait(false);

        await s_browserInitLock.WaitAsync().ConfigureAwait(false);
        try
        {
            state = s_browserState;
            if (state is null)
            {
                PlaywrightSettingsProvider.LoadViaEnvIfNeeded();
                var browserName = PlaywrightSettingsProvider.BrowserName;
                var browser = await pw[browserName]
                    .LaunchAsync(PlaywrightSettingsProvider.LaunchOptions)
                    .ConfigureAwait(false);
                state = new(browser, browserName);
                s_browserState = state;
            }

            BrowserName = state.BrowserName;
            return state.Browser;
        }
        finally
        {
            s_browserInitLock.Release();
        }
    }

    /// <summary>
    /// Creates a new <see cref="IBrowserContext"/> on the shared browser and tracks it for
    /// automatic disposal after the current test outcome has been finalized.
    /// </summary>
    /// <param name="options">Optional browser-context options.</param>
    public async Task<IBrowserContext> NewContext(BrowserNewContextOptions? options = null)
    {
        await EnsureBrowserAsync().ConfigureAwait(false);
        var ctx = await Browser.NewContextAsync(options).ConfigureAwait(false);
        return RegisterForDisposal(ctx);
    }

    /// <summary>
    /// Creates a routed browser context with tracing and optional video capture. The context is
    /// disposed automatically after the test framework finalizes the test outcome.
    /// </summary>
    /// <param name="server">The application server to route requests to.</param>
    /// <param name="options">Optional browser-context options.</param>
    /// <returns>The traced browser context.</returns>
    protected async Task<IBrowserContext> NewTracedContextAsync(
        ServerInstance server,
        BrowserNewContextOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(server);

        var directory = ArtifactManager.CreateArtifactDirectory("browser");
        var tracedContext = await Infrastructure.PlaywrightExtensions.NewTracedContextAsync(
            Browser, server, directory, ArtifactManager, options).ConfigureAwait(false);
        RegisterForDisposal(tracedContext);
        return tracedContext.Context;
    }

    /// <summary>Ensures the shared browser is initialized before the test runs.</summary>
    protected internal override async Task InitializeCoreAsync()
    {
        await base.InitializeCoreAsync().ConfigureAwait(false);
        await EnsureBrowserAsync().ConfigureAwait(false);
    }

    private sealed record BrowserState(IBrowser Browser, string BrowserName);
}
