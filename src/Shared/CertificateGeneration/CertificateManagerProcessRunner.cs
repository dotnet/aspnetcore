// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Certificates.Generation;

internal static class CertificateManagerProcessRunner
{
    internal static ProcessExecutionResult Run(ProcessStartInfo processInfo)
    {
        using var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException($"Failed to start process '{processInfo.FileName}'.");

        StringBuilder? outBuilder = null;
        StringBuilder? errBuilder = null;
        if (processInfo.RedirectStandardOutput)
        {
            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    (outBuilder ??= new()).AppendLine(e.Data);
                }
            };

            process.BeginOutputReadLine();
        }

        if (processInfo.RedirectStandardError)
        {
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                {
                    (errBuilder ??= new()).AppendLine(e.Data);
                }
            };

            process.BeginErrorReadLine();
        }

        process.WaitForExit();

        return new ProcessExecutionResult(process.ExitCode, outBuilder?.ToString() ?? string.Empty, errBuilder?.ToString() ?? string.Empty);
    }

    internal static int RunAndDiscardOutput(ProcessStartInfo processInfo)
    {
        using var process = Process.Start(processInfo)
            ?? throw new InvalidOperationException($"Failed to start process '{processInfo.FileName}'.");

        if (processInfo.RedirectStandardOutput)
        {
            process.OutputDataReceived += static (_, _) => { };
            process.BeginOutputReadLine();
        }

        if (processInfo.RedirectStandardError)
        {
            process.ErrorDataReceived += static (_, _) => { };
            process.BeginErrorReadLine();
        }

        process.WaitForExit();
        return process.ExitCode;
    }
}

internal readonly record struct ProcessExecutionResult(int ExitCode, string StandardOutput, string StandardError);
