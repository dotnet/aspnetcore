// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HelixTestRunner;

public static partial class ProcessUtil
{
    [LibraryImport("libc", SetLastError = true, EntryPoint = "kill")]
    private static partial int sys_kill(int pid, int sig);

    public static Task CaptureDumpAsync()
    {
        var dumpDirectoryPath = Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");

        if (dumpDirectoryPath == null)
        {
            return Task.CompletedTask;
        }

        var process = Process.GetCurrentProcess();
        var dumpFilePath = Path.Combine(dumpDirectoryPath, $"{process.ProcessName}-{process.Id}.dmp");

        return CaptureDumpAsync(process.Id, dumpFilePath);
    }

    public static Task CaptureDumpAsync(int pid)
    {
        var dumpDirectoryPath = Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");

        if (dumpDirectoryPath == null)
        {
            return Task.CompletedTask;
        }

        var process = Process.GetProcessById(pid);
        var dumpFilePath = Path.Combine(dumpDirectoryPath, $"{process.ProcessName}.{process.Id}.dmp");

        return CaptureDumpAsync(process.Id, dumpFilePath);
    }

    public static Task CaptureDumpAsync(int pid, string dumpFilePath)
    {
        // Skip this on OSX, we know it's unsupported right now
        if (OperatingSystem.IsMacOS())
        {
            // Can we capture stacks or do a gcdump instead?
            return Task.CompletedTask;
        }

        if (!File.Exists($"{Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT")}/dotnet-dump") &&
            !File.Exists($"{Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT")}/dotnet-dump.exe"))
        {
            return Task.CompletedTask;
        }

        return RunAsync($"{Environment.GetEnvironmentVariable("HELIX_WORKITEM_ROOT")}/dotnet-dump", $"collect -p {pid} -o \"{dumpFilePath}\"");
    }

    public static async Task<ProcessResult> RunAsync(
        string filename,
        string arguments,
        string? workingDirectory = null,
        string? dumpDirectoryPath = null,
        bool throwOnError = true,
        IDictionary<string, string?>? environmentVariables = null,
        Action<string>? outputDataReceived = null,
        Action<string>? errorDataReceived = null,
        Action<int>? onStart = null,
        CancellationToken cancellationToken = default)
    {
        PrintMessage($"Running '{filename} {arguments}'");
        using var process = new Process()
        {
            StartInfo =
            {
                FileName = filename,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            },
            EnableRaisingEvents = true
        };

        if (workingDirectory != null)
        {
            process.StartInfo.WorkingDirectory = workingDirectory;
        }

        dumpDirectoryPath ??= Environment.GetEnvironmentVariable("HELIX_DUMP_FOLDER");

        if (dumpDirectoryPath != null)
        {
            process.StartInfo.EnvironmentVariables["COMPlus_DbgEnableMiniDump"] = "1";
            process.StartInfo.EnvironmentVariables["COMPlus_DbgMiniDumpName"] = Path.Combine(dumpDirectoryPath, $"{Path.GetFileName(filename)}.%d.dmp");
        }

        if (environmentVariables != null)
        {
            foreach (var kvp in environmentVariables)
            {
                process.StartInfo.Environment.Add(kvp);
            }
        }

        var outputBuilder = new StringBuilder();
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                if (outputDataReceived != null)
                {
                    outputDataReceived.Invoke(e.Data);
                }
                else
                {
                    outputBuilder.AppendLine(e.Data);
                }
            }
        };

        var errorBuilder = new StringBuilder();
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                if (errorDataReceived != null)
                {
                    errorDataReceived.Invoke(e.Data);
                }
                else
                {
                    errorBuilder.AppendLine(e.Data);
                }
            }
        };

        var processLifetimeTask = new TaskCompletionSource<ProcessResult>();

        process.Exited += (_, e) =>
        {
            PrintMessage($"'{process.StartInfo.FileName} {process.StartInfo.Arguments}' completed with exit code '{process.ExitCode}'");
            if (throwOnError && process.ExitCode != 0)
            {
                processLifetimeTask.TrySetException(new InvalidOperationException($"Command {filename} {arguments} returned exit code {process.ExitCode} output: {outputBuilder.ToString()}"));
            }
            else
            {
                processLifetimeTask.TrySetResult(new ProcessResult(outputBuilder.ToString(), errorBuilder.ToString(), process.ExitCode));
            }
        };

        process.Start();
        onStart?.Invoke(process.Id);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var canceledTcs = new TaskCompletionSource<object?>();
        await using var _ = cancellationToken.Register(() => canceledTcs.TrySetResult(null));

        var result = await Task.WhenAny(processLifetimeTask.Task, canceledTcs.Task);

        if (result == canceledTcs.Task)
        {
            if (dumpDirectoryPath != null)
            {
                var dumpFilePath = Path.Combine(dumpDirectoryPath, $"{Path.GetFileName(filename)}.{process.Id}.dmp");
                // Capture a process dump if the dumpDirectory is set
                await CaptureDumpAsync(process.Id, dumpFilePath);
            }

            if (!OperatingSystem.IsWindows())
            {
                sys_kill(process.Id, sig: 2); // SIGINT

                var cancel = new CancellationTokenSource();

                await Task.WhenAny(processLifetimeTask.Task, Task.Delay(TimeSpan.FromSeconds(5), cancel.Token));

                cancel.Cancel();
            }

            if (!process.HasExited)
            {
                process.CloseMainWindow();

                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
        }

        return await processLifetimeTask.Task;
    }

    public static void PrintMessage(string message) => Console.WriteLine($"{DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} {message}");
    public static void PrintErrorMessage(string message) => Console.Error.WriteLine($"{DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} {message}");
}
