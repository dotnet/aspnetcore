// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;

namespace System.Diagnostics;

public static class ProcessLoggingExtensions
{
    public static void StartAndCaptureOutAndErrToLogger(this Process process, string prefix, ILogger logger)
    {
        process.EnableRaisingEvents = true;
        process.OutputDataReceived += (_, dataArgs) =>
        {
            if (!string.IsNullOrEmpty(dataArgs.Data))
            {
                logger.LogInformation($"{prefix} stdout: {{line}}", dataArgs.Data);
            }
        };

        process.ErrorDataReceived += (_, dataArgs) =>
        {
            if (!string.IsNullOrEmpty(dataArgs.Data))
            {
                logger.LogWarning($"{prefix} stderr: {{line}}", dataArgs.Data);
            }
        };

        process.Start();
        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
    }
}
