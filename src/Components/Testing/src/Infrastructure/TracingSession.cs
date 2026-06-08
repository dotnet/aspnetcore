// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Manages the lifecycle of a Playwright trace (and optionally video) for a
/// single browser context. On disposal, saves or discards artifacts based on
/// the outcome of the test captured at construction time, and attaches the
/// surviving files to the test result via
/// <see cref="TestContext.AddResultFile(string)"/>.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="TestContext"/> is captured at <see cref="StartAsync"/> time.
/// Outcome-driven save/discard depends on when the session is disposed:
/// </para>
/// <list type="bullet">
///   <item><description>
///     When disposed from <c>[TestCleanup]</c> (or any code that runs after the
///     test method body has returned), MSTest has set
///     <see cref="TestContext.CurrentTestOutcome"/> to its final value, so traces
///     for passing tests are correctly discarded and traces for failing tests
///     are saved.
///   </description></item>
///   <item><description>
///     When disposed via <c>await using</c> inside the test method body itself,
///     <see cref="TestContext.CurrentTestOutcome"/> is still
///     <see cref="UnitTestOutcome.InProgress"/>. The session conservatively
///     saves the trace in that case so failures aren't silently lost — at the
///     cost of also producing a <c>trace.zip</c> for passing tests. Consumers
///     that want strictly per-failure artifacts should dispose from
///     <c>[TestCleanup]</c> instead.
///   </description></item>
/// </list>
/// </remarks>
public sealed class TracingSession : IAsyncDisposable
{
    private readonly IBrowserContext _context;
    private readonly TestContext _test;
    private readonly string _artifactDir;
    private readonly bool _recordVideo;

    TracingSession(IBrowserContext context, TestContext test, string artifactDir, bool recordVideo)
    {
        _context = context;
        _test = test;
        _artifactDir = artifactDir;
        _recordVideo = recordVideo;
    }

    /// <summary>
    /// Starts tracing on the given browser context with screenshots, snapshots, and sources enabled.
    /// </summary>
    /// <param name="context">The browser context to trace.</param>
    /// <param name="test">The MSTest test context for the currently running test.</param>
    /// <param name="artifactDir">The directory to store trace artifacts in.</param>
    /// <param name="recordVideo">Whether video recording is enabled.</param>
    /// <returns>A <see cref="TracingSession"/> managing the trace lifecycle.</returns>
    public static async Task<TracingSession> StartAsync(
        IBrowserContext context, TestContext test, string artifactDir, bool recordVideo)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(artifactDir);

        Directory.CreateDirectory(artifactDir);

        await context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        }).ConfigureAwait(false);

        return new TracingSession(context, test, artifactDir, recordVideo);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // Save the trace whenever the test is failing or whenever we don't yet know
        // its outcome (InProgress / Unknown — typical when disposed via `await using`
        // in the test body). Discard only when the outcome is definitively Passed
        // (or Inconclusive). See class remarks for guidance.
        var outcome = _test.CurrentTestOutcome;
        var shouldSave =
            outcome is UnitTestOutcome.Failed
                    or UnitTestOutcome.Timeout
                    or UnitTestOutcome.Aborted
                    or UnitTestOutcome.Error
                    or UnitTestOutcome.InProgress
                    or UnitTestOutcome.Unknown;

        // 1. Stop tracing — save to file or discard
        var tracePath = Path.Combine(_artifactDir, "trace.zip");
        if (shouldSave)
        {
            await _context.Tracing.StopAsync(new() { Path = tracePath }).ConfigureAwait(false);
            _test.AddResultFile(tracePath);
        }
        else
        {
            await _context.Tracing.StopAsync().ConfigureAwait(false); // discard
        }

        // 2. Handle video: close context to flush video files, then keep or delete
        if (_recordVideo)
        {
            var pages = _context.Pages.ToList();
            await _context.CloseAsync().ConfigureAwait(false); // flushes video to disk

            foreach (var page in pages)
            {
                if (page.Video is null)
                {
                    continue;
                }

                if (shouldSave)
                {
                    try
                    {
                        var videoPath = await page.Video.PathAsync().ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(videoPath))
                        {
                            _test.AddResultFile(videoPath);
                        }
                    }
                    catch
                    {
                        // video file may not be available
                    }
                }
                else
                {
                    try { await page.Video.DeleteAsync().ConfigureAwait(false); }
                    catch { /* video file may not exist */ }
                }
            }
        }

        // 3. Diagnostic line — keeps the existing CI-side directory pointer for
        //    consumers that also glob test-artifacts/** directly.
        if (shouldSave && Directory.Exists(_artifactDir))
        {
            var files = Directory.GetFiles(_artifactDir);
            if (files.Length > 0)
            {
                _test.WriteLine($"[E2E] Test artifacts saved to: {_artifactDir}");
                foreach (var file in files)
                {
                    _test.WriteLine($"[E2E]   {Path.GetFileName(file)}");
                }
            }
        }
        else if (!shouldSave && Directory.Exists(_artifactDir))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(_artifactDir).Any())
                {
                    Directory.Delete(_artifactDir);
                }
            }
            catch { /* best effort cleanup */ }
        }
    }
}
