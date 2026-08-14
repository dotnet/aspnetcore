// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.Playwright;

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Manages the lifecycle of a Playwright trace (and optionally video) for a single
/// browser context. On disposal the session asks the supplied
/// <see cref="ITestArtifactManager"/> whether to keep the artifacts, then either saves and
/// publishes them or discards them.
/// </summary>
/// <remarks>
/// <para>
/// This type is intentionally decoupled from any test framework: it does not read a
/// test outcome or attach files itself. The save decision and the handling of saved
/// artifacts are provided by <see cref="ITestArtifactManager"/>, so a source-generated adapter in the
/// consumer test assembly can bridge the test framework's outcome/attachment APIs
/// (for example MSTest's <c>TestContext.CurrentTestOutcome</c> and
/// <c>TestContext.AddResultFile</c>) without the library taking a dependency on them.
/// </para>
/// <para>
/// <see cref="ITestArtifactManager.ShouldSaveArtifacts"/> is evaluated at disposal time,
/// after the test framework has finalized the test outcome.
/// </para>
/// </remarks>
internal sealed class TracingSession : IAsyncDisposable
{
    private readonly IBrowserContext _context;
    private readonly string _artifactDir;
    private readonly bool _recordVideo;
    private readonly ITestArtifactManager _artifactManager;

    TracingSession(
        IBrowserContext context,
        string artifactDir,
        bool recordVideo,
        ITestArtifactManager artifactManager)
    {
        _context = context;
        _artifactDir = artifactDir;
        _recordVideo = recordVideo;
        _artifactManager = artifactManager;
    }

    /// <summary>
    /// Starts tracing on the given browser context with screenshots, snapshots, and sources enabled.
    /// </summary>
    /// <param name="context">The browser context to trace.</param>
    /// <param name="artifactDir">The directory to store trace artifacts in.</param>
    /// <param name="recordVideo">Whether video recording is enabled.</param>
    /// <param name="artifactManager">Determines whether artifacts are retained and publishes retained files.</param>
    /// <returns>A <see cref="TracingSession"/> managing the trace lifecycle.</returns>
    internal static async Task<TracingSession> StartAsync(
        IBrowserContext context,
        string artifactDir,
        bool recordVideo,
        ITestArtifactManager artifactManager)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(artifactDir);
        ArgumentNullException.ThrowIfNull(artifactManager);

        Directory.CreateDirectory(artifactDir);

        await context.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
            Sources = true
        }).ConfigureAwait(false);

        return new TracingSession(context, artifactDir, recordVideo, artifactManager);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        var shouldSave = _artifactManager.ShouldSaveArtifacts();
        var savedFiles = new List<string>();

        // 1. Stop tracing — save to file or discard
        var tracePath = Path.Combine(_artifactDir, "trace.zip");
        if (shouldSave)
        {
            await _context.Tracing.StopAsync(new() { Path = tracePath }).ConfigureAwait(false);
            savedFiles.Add(tracePath);
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
                            savedFiles.Add(videoPath);
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

        // 3. Report saved artifacts to the caller (attach + log happen in the adapter),
        //    or best-effort remove an empty discarded directory.
        if (shouldSave)
        {
            if (savedFiles.Count > 0)
            {
                _artifactManager.AddArtifacts(savedFiles);
            }
        }
        else if (Directory.Exists(_artifactDir))
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
