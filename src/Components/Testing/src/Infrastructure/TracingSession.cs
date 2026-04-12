// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Playwright;
using Xunit;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Manages the lifecycle of a Playwright trace (and optionally video) for a
/// single browser context. On disposal, saves or discards artifacts based on
/// the test outcome reported by xUnit's <see cref="TestContext"/>.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="TestContext.Current"/> provides test state availability:
/// </para>
/// <list type="bullet">
///   <item><description>During test method body (<c>await using</c>): <c>TestState</c> is <c>null</c> → always saves (conservative; wasteful on passing tests)</description></item>
///   <item><description>During <c>IAsyncLifetime.DisposeAsync</c>: <c>TestState</c> is populated → conditional save on failure only</description></item>
/// </list>
/// <para>
/// To avoid keeping artifacts for passing tests, dispose the <see cref="TracingSession"/>
/// inside <c>IAsyncLifetime.DisposeAsync</c> rather than via <c>await using</c> in the test body.
/// </para>
/// </remarks>
public sealed class TracingSession : IAsyncDisposable
{
    private readonly IBrowserContext _context;
    private readonly string _artifactDir;
    private readonly bool _recordVideo;

    TracingSession(IBrowserContext context, string artifactDir, bool recordVideo)
    {
        _context = context;
        _artifactDir = artifactDir;
        _recordVideo = recordVideo;
    }

    /// <summary>
    /// Starts tracing on the given browser context with screenshots, snapshots, and sources enabled.
    /// </summary>
    /// <param name="context">The browser context to trace.</param>
    /// <param name="artifactDir">The directory to store trace artifacts in.</param>
    /// <param name="recordVideo">Whether video recording is enabled.</param>
    /// <returns>A <see cref="TracingSession"/> managing the trace lifecycle.</returns>
    public static async Task<TracingSession> StartAsync(
        IBrowserContext context, string artifactDir, bool recordVideo)
    {
        Directory.CreateDirectory(artifactDir);

        await context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        }).ConfigureAwait(false);

        return new TracingSession(context, artifactDir, recordVideo);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        // TestState is populated during xUnit's CleaningUp phase (DisposeAsync of the
        // test class). When disposed via `await using` in the test method body, TestState
        // is null — we conservatively save artifacts (correct on failure, wasteful on success).
        var testState = TestContext.Current.TestState;
        var shouldSave = testState is null || testState.Result == TestResult.Failed;

        // 1. Stop tracing — save to file or discard
        var tracePath = Path.Combine(_artifactDir, "trace.zip");
        if (shouldSave)
        {
            await _context.Tracing.StopAsync(new() { Path = tracePath }).ConfigureAwait(false);
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

            if (!shouldSave)
            {
                foreach (var page in pages)
                {
                    if (page.Video is not null)
                    {
                        try { await page.Video.DeleteAsync().ConfigureAwait(false); }
                        catch { /* video file may not exist */ }
                    }
                }
            }
        }

        // 3. Report or clean up
        if (shouldSave && Directory.Exists(_artifactDir))
        {
            var files = Directory.GetFiles(_artifactDir);
            if (files.Length > 0)
            {
                Console.WriteLine($"[E2E] Test artifacts saved to: {_artifactDir}");
                foreach (var file in files)
                {
                    Console.WriteLine($"[E2E]   {Path.GetFileName(file)}");
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
