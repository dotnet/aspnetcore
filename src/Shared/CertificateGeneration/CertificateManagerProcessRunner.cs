// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Certificates.Generation;

internal static class CertificateManagerProcessRunner
{
    internal static ProcessExecutionResult Run(ProcessStartInfo processInfo)
    {
        using var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException($"Failed to start process '{processInfo.FileName}'.");

        var standardOutputTask = processInfo.RedirectStandardOutput
            ? process.StandardOutput.ReadToEndAsync()
            : Task.FromResult(string.Empty);
        var standardErrorTask = processInfo.RedirectStandardError
            ? process.StandardError.ReadToEndAsync()
            : Task.FromResult(string.Empty);

        process.WaitForExit();
        Task.WaitAll(standardOutputTask, standardErrorTask);

        return new ProcessExecutionResult(
            process.ExitCode,
            standardOutputTask.GetAwaiter().GetResult(),
            standardErrorTask.GetAwaiter().GetResult());
    }
}

internal readonly record struct ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError);
