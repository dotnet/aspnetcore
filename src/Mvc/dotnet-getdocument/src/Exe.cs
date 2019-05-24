// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Microsoft.Extensions.ApiDescription.Tool
{
    internal static class Exe
    {
        public static int Run(
            string executable,
            IReadOnlyList<string> args,
            string workingDirectory = null,
            bool interceptOutput = false)
        {
            var arguments = ToArguments(args);

            Reporter.WriteVerbose(executable + " " + arguments);

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = interceptOutput
            };
            if (workingDirectory != null)
            {
                startInfo.WorkingDirectory = workingDirectory;
            }

            using (var process = Process.Start(startInfo))
            {
                if (interceptOutput)
                {
                    string line;
                    while ((line = process.StandardOutput.ReadLine()) != null)
                    {
                        Reporter.WriteVerbose(line);
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

        private static string ToArguments(IReadOnlyList<string> args)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < args.Count; i++)
            {
                if (i != 0)
                {
                    builder.Append(" ");
                }

                if (args[i].IndexOf(' ') == -1)
                {
                    builder.Append(args[i]);

                    continue;
                }

                builder.Append("\"");

                var pendingBackslashs = 0;
                for (var j = 0; j < args[i].Length; j++)
                {
                    switch (args[i][j])
                    {
                        case '\"':
                            if (pendingBackslashs != 0)
                            {
                                builder.Append('\\', pendingBackslashs * 2);
                                pendingBackslashs = 0;
                            }
                            builder.Append("\\\"");
                            break;

                        case '\\':
                            pendingBackslashs++;
                            break;

                        default:
                            if (pendingBackslashs != 0)
                            {
                                if (pendingBackslashs == 1)
                                {
                                    builder.Append("\\");
                                }
                                else
                                {
                                    builder.Append('\\', pendingBackslashs * 2);
                                }

                                pendingBackslashs = 0;
                            }

                            builder.Append(args[i][j]);
                            break;
                    }
                }

                if (pendingBackslashs != 0)
                {
                    builder.Append('\\', pendingBackslashs * 2);
                }

                builder.Append("\"");
            }

            return builder.ToString();
        }
    }
}
