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
            UseShellExecute = false,
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

        using var process = Process.Start(startInfo);
        if (interceptOutput)
        {
            string line;
            while ((line = process.StandardOutput.ReadLine()) != null)
            {
                reporter.WriteVerbose(line);
            }
        }

        // Follow precedent set in Razor integration tests and ensure process events and output are complete.
        // https://github.com/aspnet/Razor/blob/d719920fdcc7d1db3a6f74cd5404d66fa098f057/test/Microsoft.NET.Sdk.Razor.Test/IntegrationTests/MSBuildProcessManager.cs#L91-L102
        // Timeout is double how long the inside man waits for the IDocumentProcessor to wrap up.
        if (!process.WaitForExit((int)(TimeSpan.FromMinutes(2).TotalMilliseconds)))
        {
            process.Kill();

            // Should be unreachable in almost every case.
            throw new TimeoutException($"Process {executable} timed out after 2 minutes.");
        }

        process.WaitForExit();

        return process.ExitCode;
    }
}
