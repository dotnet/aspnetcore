// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace System.Diagnostics
{
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
}
