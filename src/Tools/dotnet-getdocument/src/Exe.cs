// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.CommandLineUtils;
using Microsoft.Extensions.Tools.Internal;

namespace Microsoft.Extensions.ApiDescription.Tool;

internal static class Exe
{
    public static int Run(
        string executable,
        IReadOnlyList<string> args,
        IReporter reporter,
        string workingDirectory = null,
        bool interceptOutput = false)
    {
        reporter.WriteVerbose(executable + " " + string.Join(" ", args));

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            InheritedHandles = [],
            RedirectStandardOutput = interceptOutput
        };
        foreach (var argument in args)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (workingDirectory != null)
        {
            startInfo.WorkingDirectory = workingDirectory;
        }

        using var process = Process.Start(startInfo)!;
        if (interceptOutput)
        {
            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                reporter.WriteVerbose(line);
            }
        }

        // Timeout is double how long the inside man waits for the IDocumentProcessor to wrap up.
        ProcessExitStatus status = process.SafeHandle.WaitForExitOrKillOnTimeout(TimeSpan.FromMinutes(2));
        if (status.Canceled)
        {
            // Should be unreachable in almost every case.
            throw new TimeoutException($"Process {executable} timed out after 2 minutes.");
        }

        return status.ExitCode;
    }
}
