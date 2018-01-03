// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal static class MSBuildProcessManager
    {
        public static Task<MSBuildResult> RunProcessAsync(
            ProjectDirectory project,
            string arguments,
            TimeSpan? timeout = null,
            MSBuildProcessKind msBuildProcessKind = MSBuildProcessKind.Dotnet)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(30);

            var processStartInfo = new ProcessStartInfo()
            {
                WorkingDirectory = project.DirectoryPath,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            if (msBuildProcessKind == MSBuildProcessKind.Desktop)
            {
                if (string.IsNullOrEmpty(BuildVariables.MSBuildPath))
                {
                    throw new ArgumentException("Unable to locate MSBuild.exe to run desktop tests.");
                }

                processStartInfo.FileName = BuildVariables.MSBuildPath;
                processStartInfo.Arguments = arguments;
            }
            else
            {
                processStartInfo.FileName = "dotnet";
                processStartInfo.Arguments = $"msbuild {arguments}";
            }

            var process = new Process()
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true,
            };

            var completionSource = new TaskCompletionSource<MSBuildResult>();
            var output = new StringBuilder();

            process.Exited += Process_Exited;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.OutputDataReceived += Process_OutputDataReceived;

            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            var timeoutTask = Task.Delay(timeout.Value).ContinueWith((t) =>
            {
                // Don't timeout during debug sessions
                while (Debugger.IsAttached)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }

                if (process.HasExited)
                {
                    // This will happen on success, the 'real' task has already completed so this value will
                    // never be visible.
                    return (MSBuildResult)null;
                }

                // This is a timeout.
                process.Kill();
                throw new TimeoutException($"command '${process.StartInfo.FileName} {process.StartInfo.Arguments}' timed out after {timeout}.");
            });

            return Task.WhenAny<MSBuildResult>(completionSource.Task, timeoutTask).Unwrap();

            void Process_Exited(object sender, EventArgs e)
            {
                var result = new MSBuildResult(project, process.StartInfo.FileName, process.StartInfo.Arguments, process.ExitCode, output.ToString());
                completionSource.SetResult(result);
            }

            void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                output.AppendLine(e.Data);
            }

            void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                output.AppendLine(e.Data);
            }
        }
    }
}
