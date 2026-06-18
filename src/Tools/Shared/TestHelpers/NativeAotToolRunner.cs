// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Microsoft.Extensions.Tools.Internal;

public static class NativeAotToolRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(2);

    public static async Task<NativeAotToolResult> RunAsync(
        string toolName,
        IEnumerable<string> arguments,
        ITestOutputHelper output,
        string? workingDirectory = null,
        IReadOnlyDictionary<string, string>? environmentVariables = null,
        TimeSpan? timeout = null)
    {
        var toolPath = GetPublishedToolPath(toolName);
        var startInfo = new ProcessStartInfo
        {
            FileName = toolPath,
            WorkingDirectory = workingDirectory ?? AppContext.BaseDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environmentVariables is not null)
        {
            foreach (var variable in environmentVariables)
            {
                startInfo.Environment[variable.Key] = variable.Value;
            }
        }

        var commandLine = $"{toolPath} {string.Join(" ", startInfo.ArgumentList)}";
        output.WriteLine($"> {commandLine}");

        using var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true,
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var cts = new CancellationTokenSource(timeout ?? DefaultTimeout);
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException ex) when (process.HasExited)
            {
                output.WriteLine($"Process exited before timeout cleanup completed: {ex.Message}");
            }

            throw new TimeoutException($"Timed out after {(timeout ?? DefaultTimeout).TotalSeconds} seconds running '{commandLine}'.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        output.WriteLine($"Exit code: {process.ExitCode}");
        WriteOutput(output, "stdout", stdout);
        WriteOutput(output, "stderr", stderr);

        return new NativeAotToolResult(process.ExitCode, stdout, stderr);
    }

    public static Dictionary<string, string> CreateIsolatedUserProfileEnvironment(TemporaryDirectory directory)
    {
        var root = directory.Root;
        var environment = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["DOTNET_CLI_HOME"] = Path.Combine(root, ".dotnet"),
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            environment["APPDATA"] = Path.Combine(root, "AppData", "Roaming");
            environment["USERPROFILE"] = Path.Combine(root, "UserProfile");
        }
        else
        {
            environment["HOME"] = Path.Combine(root, "Home");
            environment["XDG_CONFIG_HOME"] = Path.Combine(root, "Config");
        }

        foreach (var path in environment.Values)
        {
            Directory.CreateDirectory(path);
        }

        return environment;
    }

    private static string GetPublishedToolPath(string toolName)
    {
        var executableName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? $"{toolName}.exe" : toolName;
        var toolPath = Path.Combine(AppContext.BaseDirectory, "nativeaot-tools", toolName, executableName);

        if (!File.Exists(toolPath))
        {
            throw new FileNotFoundException($"Could not find the published Native AOT tool executable '{toolPath}'.", toolPath);
        }

        return toolPath;
    }

    private static void WriteOutput(ITestOutputHelper output, string streamName, string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        output.WriteLine($"--- {streamName} ---");
        output.WriteLine(value);
    }
}

public sealed class NativeAotToolResult
{
    public NativeAotToolResult(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }

    public int ExitCode { get; }

    public string StandardOutput { get; }

    public string StandardError { get; }

    public string AllOutput
    {
        get
        {
            var builder = new StringBuilder();
            builder.Append(StandardOutput);
            builder.Append(StandardError);
            return builder.ToString();
        }
    }
}
