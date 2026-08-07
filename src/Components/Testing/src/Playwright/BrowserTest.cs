// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Components.Testing.Infrastructure;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Playwright;

/// <summary>
/// Base class for MSTest tests that need a Playwright <see cref="IBrowser"/>. Mirrors the
/// shape of <c>Microsoft.Playwright.MSTest.BrowserTest</c> without taking a dependency on
/// that package.
/// </summary>
/// <remarks>
/// <para>
/// One <see cref="IBrowser"/> is created per test assembly (lazily, on first use) and
/// shared across every test. Browser type and launch options are controlled via
/// <see cref="BrowserName"/> and <see cref="GetBrowserLaunchOptions"/>; override in a
/// derived class to customize.
/// </para>
/// <para>
/// Use <see cref="NewContext"/> to create a per-test <see cref="IBrowserContext"/>; every
/// context obtained that way is tracked and closed automatically when the per-test instance
/// is disposed. The class therefore plays nicely with MSTest's default
/// method-level parallelism — contexts are per-test, no shared mutable browser-page state.
/// </para>
/// </remarks>
public abstract class BrowserTest : PlaywrightTest
{
    private static IBrowser? s_browser;
    private static readonly SemaphoreSlim s_browserInitLock = new(1, 1);

    /// <summary>The shared <see cref="IBrowser"/>. Initialized on first use by <see cref="EnsureBrowserAsync"/>.</summary>
    public IBrowser Browser =>
        s_browser ?? throw new InvalidOperationException(
            $"Browser has not been initialized. {nameof(EnsureBrowserAsync)} is called automatically " +
            "by the BrowserTest initialization hook; ensure your derived class calls base.InitializeCoreAsync().");

    /// <summary>The browser type to launch. Defaults to <c>chromium</c>. Override to use <c>firefox</c> or <c>webkit</c>.</summary>
    public virtual string BrowserName => "chromium";

    /// <summary>
    /// Override to customize <see cref="BrowserTypeLaunchOptions"/> passed to
    /// <see cref="IBrowserType.LaunchAsync"/>. Default returns an empty options object
    /// (headless launch with no extra arguments).
    /// </summary>
    public virtual BrowserTypeLaunchOptions GetBrowserLaunchOptions() => new();

    /// <summary>
    /// Returns the shared <see cref="IBrowser"/>, creating it on first call. Safe to
    /// invoke concurrently — initialization is serialized.
    /// </summary>
    public async Task<IBrowser> EnsureBrowserAsync()
    {
        if (s_browser is not null)
        {
            return s_browser;
        }

        var pw = await EnsurePlaywrightAsync().ConfigureAwait(false);

        await s_browserInitLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (s_browser is null)
            {
                var browserType = BrowserName.ToLowerInvariant() switch
                {
                    "firefox" => pw.Firefox,
                    "webkit" => pw.Webkit,
                    "chromium" => pw.Chromium,
                    _ => throw new ArgumentException(
                        $"Unknown browser name '{BrowserName}'. Use 'chromium', 'firefox', or 'webkit'."),
                };
                s_browser = await browserType.LaunchAsync(GetBrowserLaunchOptions()).ConfigureAwait(false);
            }
            return s_browser;
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

}
