// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Playwright;
using Microsoft.Playwright.Xunit.v3;
using Xunit;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Extension methods for Playwright types used in E2E tests.
/// </summary>
public static class PlaywrightExtensions
{
    // Cross-platform invalid file name characters. Path.GetInvalidFileNameChars()
    // is OS-dependent (Linux only returns '/' and '\0'), so we use a fixed set
    // covering Windows, macOS, and Linux to ensure consistent sanitization.
    private static readonly HashSet<char> s_invalidFileNameChars =
    [
        '\\', '/', ':', '*', '?', '"', '<', '>', '|', '\0',
        .. Enumerable.Range(1, 31).Select(i => (char)i)
    ];

    // Toggle video recording via environment variable.
    // Set PLAYWRIGHT_RECORD_VIDEO=1 to enable video capture for all tests.
    private static readonly bool s_recordVideo =
        string.Equals(
            Environment.GetEnvironmentVariable("PLAYWRIGHT_RECORD_VIDEO"),
            "1",
            StringComparison.Ordinal);

    // Override the default artifact root directory via E2E_ARTIFACTS_DIR environment variable.
    // Defaults to a test-artifacts/ subdirectory next to the test assembly.
    private static readonly string s_artifactsRoot =
        Environment.GetEnvironmentVariable("E2E_ARTIFACTS_DIR")
        ?? Path.Combine(AppContext.BaseDirectory, "test-artifacts");

    /// <summary>
    /// Sets the <c>X-Test-Backend</c> header on browser context options
    /// so the YARP proxy routes requests to the correct <see cref="ServerInstance"/>.
    /// </summary>
    /// <param name="options">The browser context options to configure.</param>
    /// <param name="server">The server instance to route traffic to.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static BrowserNewContextOptions WithServerRouting(
        this BrowserNewContextOptions options, ServerInstance server)
    {
        var headers = options.ExtraHTTPHeaders?.ToDictionary(h => h.Key, h => h.Value)
            ?? new Dictionary<string, string>();
        headers["X-Test-Backend"] = server.Id;
        options.ExtraHTTPHeaders = headers;
        return options;
    }

    /// <summary>
    /// Configures video recording on the browser context options when
    /// the <c>PLAYWRIGHT_RECORD_VIDEO=1</c> environment variable is set.
    /// Must be called before creating the context (<c>RecordVideoDir</c> cannot be set after creation).
    /// </summary>
    /// <param name="options">The browser context options to configure.</param>
    /// <param name="artifactDir">The directory to store video files in.</param>
    /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
    public static BrowserNewContextOptions WithArtifacts(
        this BrowserNewContextOptions options, string? artifactDir = null)
    {
        if (s_recordVideo && artifactDir is not null)
        {
            options.RecordVideoDir = artifactDir;
        }
        return options;
    }

    /// <summary>
    /// Starts tracing on an existing browser context. Returns a <see cref="TracingSession"/>
    /// that saves or discards the trace (and video) on disposal based on the
    /// test outcome from <see cref="TestContext.Current"/>.
    /// </summary>
    /// <param name="context">The browser context to trace.</param>
    /// <param name="artifactDir">The directory to store trace artifacts in.</param>
    /// <returns>A <see cref="TracingSession"/> that manages trace lifecycle.</returns>
    public static async Task<TracingSession> TraceAsync(
        this IBrowserContext context, string artifactDir)
    {
        return await TracingSession.StartAsync(context, artifactDir, s_recordVideo).ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new browser context with server routing, artifact capture (video if enabled),
    /// and active tracing. Returns a <see cref="TracedContext"/> that wraps the context and tracing session.
    /// </summary>
    /// <remarks>
    /// Internally calls <see cref="BrowserTest.NewContext"/> so the context is tracked
    /// and auto-disposed by <see cref="BrowserTest"/>'s lifecycle.
    /// </remarks>
    /// <param name="test">The <see cref="BrowserTest"/> instance to create the context on.</param>
    /// <param name="server">The server instance to route traffic to.</param>
    /// <param name="options">Optional browser context options. If <c>null</c>, defaults are used.</param>
    /// <returns>A <see cref="TracedContext"/> wrapping the browser context and tracing session.</returns>
    public static async Task<TracedContext> NewTracedContextAsync(
        this BrowserTest test,
        ServerInstance server,
        BrowserNewContextOptions? options = null)
    {
        var testName = TestContext.Current.Test?.TestDisplayName ?? "unknown";
        var sanitized = SanitizeFileName(testName);
        var artifactDir = Path.Combine(s_artifactsRoot, sanitized);

        options ??= new BrowserNewContextOptions();
        options = options
            .WithServerRouting(server)
            .WithArtifacts(artifactDir);

        var context = await test.NewContext(options).ConfigureAwait(false);
        var session = await TracingSession.StartAsync(context, artifactDir, s_recordVideo).ConfigureAwait(false);
        return new TracedContext(context, session);
    }

    /// <summary>
    /// Sets the <c>test-session-id</c> cookie on a browser context. The cookie targets
    /// the proxy URL so YARP forwards it to the app, where the test infrastructure
    /// reads it into <see cref="TestSessionContext.Id"/>.
    /// </summary>
    /// <param name="context">The browser context to add the cookie to.</param>
    /// <param name="server">The server instance providing the proxy URL.</param>
    /// <param name="sessionId">The session identifier value.</param>
    public static async Task SetTestSession(
        this IBrowserContext context, ServerInstance server, string sessionId)
    {
        await context.AddCookiesAsync(
        [
            new Cookie
            {
                Name = "test-session-id",
                Value = sessionId,
                Url = server.TestUrl
            }
        ]).ConfigureAwait(false);
    }

    /// <summary>
    /// Waits for the Blazor framework to load on the page by checking that the
    /// global <c>Blazor</c> object exists.
    /// </summary>
    /// <param name="page">The page to wait on.</param>
    public static Task WaitForBlazorAsync(this IPage page)
        => page.WaitForFunctionAsync("() => typeof Blazor !== 'undefined'");

    /// <summary>
    /// Waits for a Blazor component to become interactive by detecting event handler
    /// registrations on the element matching the CSS selector. Blazor's EventDelegator
    /// stores handler info as an expando property (<c>_blazorEvents_{id}</c>) on DOM
    /// elements when <c>@onclick</c>, <c>@onchange</c>, etc. are registered.
    /// </summary>
    /// <param name="page">The page to wait on.</param>
    /// <param name="selector">CSS selector identifying the element to check.</param>
    public static Task WaitForInteractiveAsync(this IPage page, string selector)
        => page.WaitForFunctionAsync("""
            (selector) => {
                const el = document.querySelector(selector);
                return el && Object.getOwnPropertyNames(el)
                    .some(k => k.startsWith('_blazorEvents_'));
            }
            """, selector);

    /// <summary>
    /// Registers a one-shot listener for the Blazor <c>enhancedload</c> event
    /// and returns a task that completes when the event fires. Enhanced navigation
    /// patches the DOM instead of doing a full page reload.
    /// Call before the action that triggers navigation, then await the returned task.
    /// </summary>
    /// <param name="page">The page to listen on.</param>
    /// <returns>A task that completes when enhanced navigation finishes.</returns>
    public static async Task WaitForEnhancedNavigationAsync(this IPage page)
    {
        await using var handle = await page.EvaluateHandleAsync(
            "() => new Promise(resolve => Blazor.addEventListener('enhancedload', () => resolve(true), { once: true }))").ConfigureAwait(false);
    }

    /// <summary>
    /// Installs a one-shot <c>enhancedload</c> listener, executes the navigation action,
    /// and waits for the enhanced navigation to complete.
    /// </summary>
    /// <param name="page">The page to listen on.</param>
    /// <param name="navigationAction">The async action that triggers enhanced navigation (e.g., clicking a link).</param>
    public static async Task WaitForEnhancedNavigationAsync(this IPage page, Func<Task> navigationAction)
    {
        var navTask = page.WaitForEnhancedNavigationAsync();
        await navigationAction().ConfigureAwait(false);
        await navTask.ConfigureAwait(false);
    }

    /// <summary>
    /// Replaces characters that are invalid in file names with underscores.
    /// Uses a fixed cross-platform set of invalid characters so that file names
    /// are safe on Windows, macOS, and Linux regardless of the current OS.
    /// </summary>
    /// <param name="name">The file name to sanitize.</param>
    /// <returns>A sanitized file name safe for use on the file system.</returns>
    public static string SanitizeFileName(string name)
    {
        return string.Concat(name.Select(c => s_invalidFileNameChars.Contains(c) ? '_' : c));
    }
}
