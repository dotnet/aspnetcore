// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.WebSockets.ConformanceTest.Autobahn;

public class Executable
{
    private static readonly string _exeSuffix = OperatingSystem.IsWindows() ? ".exe" : string.Empty;

    public string Location { get; }

    protected Executable(string path)
    {
        Location = path;
    }

    public static string Locate(string name)
    {
        foreach (var dir in Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator))
        {
            var candidate = Path.Combine(dir, name + _exeSuffix);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }

    public async Task<int> ExecAsync(string args, CancellationToken cancellationToken, ILogger logger)
    {
        using var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = Location,
                Arguments = args,
                InheritedHandles = [],
                RedirectStandardError = true,
                RedirectStandardOutput = true
            }
        };

        process.Start();

        Task<ProcessExitStatus> exitTask = process.SafeHandle.WaitForExitOrKillOnCancellationAsync(cancellationToken);
        ProcessExitStatus status;
        try
        {
            await foreach (ProcessOutputLine line in process.ReadAllLinesAsync(cancellationToken))
            {
                if (line.StandardError)
                {
                    LogIfNotNull(logger.LogError, "stderr: {0}", line.Content);
                }
                else
                {
                    LogIfNotNull(logger.LogInformation, "stdout: {0}", line.Content);
                }
            }

            status = await exitTask;

            // Cancellation after output reaches EOF must still propagate instead of returning the process exit code.
            cancellationToken.ThrowIfCancellationRequested();

            return status.ExitCode;
        }
        catch
        {
            process.Kill();
            await exitTask;

            throw;
        }
    }

    private void LogIfNotNull(Action<string, object[]> logger, string message, string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            logger(message, new[] { data });
        }
    }
}
