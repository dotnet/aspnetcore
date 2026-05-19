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
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = Location,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            },
            EnableRaisingEvents = true
        };
        var tcs = new TaskCompletionSource<int>();

        using (cancellationToken.Register(() => Cancel(process, tcs)))
        {
            process.Exited += (_, __) => tcs.TrySetResult(process.ExitCode);
            process.OutputDataReceived += (_, a) => LogIfNotNull(logger.LogInformation, "stdout: {0}", a.Data);
            process.ErrorDataReceived += (_, a) => LogIfNotNull(logger.LogError, "stderr: {0}", a.Data);

            process.Start();

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            return await tcs.Task;
        }
    }

    private void LogIfNotNull(Action<string, object[]> logger, string message, string data)
    {
        if (!string.IsNullOrEmpty(data))
        {
            logger(message, new[] { data });
        }
    }

    private static void Cancel(Process process, TaskCompletionSource<int> tcs)
    {
        if (process != null && !process.HasExited)
        {
            process.Kill();
        }
        tcs.TrySetCanceled();
    }
}
