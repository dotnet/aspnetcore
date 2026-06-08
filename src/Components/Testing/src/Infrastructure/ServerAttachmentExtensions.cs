// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Components.Testing.Infrastructure;

/// <summary>
/// Helpers for attaching <see cref="ServerInstance"/>-side artifacts (captured
/// stdout / stderr) to the currently running MSTest test result via
/// <see cref="TestContext.AddResultFile(string)"/>.
/// </summary>
public static class ServerAttachmentExtensions
{
    /// <summary>
    /// When the current test has failed (or timed out / errored / aborted),
    /// flushes the captured stdout and stderr of each <paramref name="servers"/>
    /// entry to per-server files under a subdirectory of <c>test-artifacts/server-output/</c>
    /// and attaches them to the test result. No-op when the test has not failed.
    /// </summary>
    /// <param name="test">The MSTest <see cref="TestContext"/> for the current test.</param>
    /// <param name="servers">The server instances whose captured output should be attached. Must not be null or contain null elements.</param>
    public static void AttachServerOutputIfFailed(this TestContext test, params ServerInstance[] servers)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(servers);

        if (test.CurrentTestOutcome is not (UnitTestOutcome.Failed
                                          or UnitTestOutcome.Timeout
                                          or UnitTestOutcome.Aborted
                                          or UnitTestOutcome.Error))
        {
            return;
        }

        if (servers.Length == 0)
        {
            return;
        }

        var dir = Path.Combine(
            AppContext.BaseDirectory,
            "test-artifacts",
            "server-output",
            PlaywrightExtensions.SanitizeFileName(test.TestName ?? "unknown"));

        foreach (var server in servers)
        {
            ArgumentNullException.ThrowIfNull(server);

            server.WriteCapturedOutputTo(dir);

            var stdout = Path.Combine(dir, $"{server.AppName}-{server.Id}.stdout.log");
            var stderr = Path.Combine(dir, $"{server.AppName}-{server.Id}.stderr.log");
            if (File.Exists(stdout))
            {
                test.AddResultFile(stdout);
            }
            if (File.Exists(stderr))
            {
                test.AddResultFile(stderr);
            }
        }
    }
}
