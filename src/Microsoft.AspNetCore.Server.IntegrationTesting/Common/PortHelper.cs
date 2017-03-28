// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class PortHelper
    {
        public static void LogPortStatus(ILogger logger, int port)
        {
            logger.LogInformation("Checking for processes currently using port {0}", port);

            var psi = new ProcessStartInfo
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                psi.FileName = "cmd";
                psi.Arguments = $"/C netstat -nq | find \"{port}\"";
            }
            else
            {
                psi.FileName = "lsof";
                psi.Arguments = $"-i :{port}";
            }

            var process = new Process
            {
                StartInfo = psi,
                EnableRaisingEvents = true
            };

            var linesLogged = false;

            process.OutputDataReceived += (sender, data) =>
            {
                if (!string.IsNullOrWhiteSpace(data.Data))
                {
                    linesLogged = true;
                    logger.LogInformation("portstatus: {0}", data.Data);
                }
            };
            process.ErrorDataReceived += (sender, data) =>
            {
                if (!string.IsNullOrWhiteSpace(data.Data))
                {
                    logger.LogWarning("portstatus: {0}", data.Data);
                }
            };

            try
            {
                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
                process.WaitForExit();

                if (!linesLogged)
                {
                    logger.LogInformation("portstatus: it appears the port {0} is not in use.", port);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("Failed to check port status. Executed: {0} {1}\nError: {2}", psi.FileName, psi.Arguments, ex.ToString());
            }
            return;
        }
    }
}
