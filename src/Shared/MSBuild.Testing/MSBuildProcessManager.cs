// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.CommandLineUtils;

namespace Microsoft.AspNetCore.Razor.Design.IntegrationTests
{
    internal static class MSBuildProcessManager
    {
        internal static Task<MSBuildResult> DotnetMSBuild(
            ProjectDirectory project,
             string target = "Build",
             string args = null,
             string buildServerPipeName = null)
        {
            var buildArgumentList = new List<string>
            {
                // Disable node-reuse. We don't want msbuild processes to stick around
                // once the test is completed.
                "/nr:false",

                // Always generate a bin log for debugging purposes
                "/bl",

                // Let the test app know it is running as part of a test.
                "/p:RunningAsTest=true",

                $"/p:MicrosoftNETCoreAppRuntimeVersion={BuildVariables.MicrosoftNETCoreAppRuntimeVersion}",
                $"/p:MicrosoftNetCompilersToolsetVersion={BuildVariables.MicrosoftNetCompilersToolsetVersion}",
                $"/p:RazorSdkDirectoryRoot={BuildVariables.RazorSdkDirectoryRoot}",
                $"/p:RepoRoot={BuildVariables.RepoRoot}",
                $"/p:Configuration={project.Configuration}",
                $"/t:{target}",
                args,
            };

            if (buildServerPipeName != null)
            {
                buildArgumentList.Add($@"/p:_RazorBuildServerPipeName=""{buildServerPipeName}""");
            }

            var buildArguments = string.Join(" ", buildArgumentList);

            return RunProcessAsync(
                project,
                buildArguments,
                timeout: null,
                MSBuildProcessKind.Dotnet);
        }

        public static async Task<MSBuildResult> RunProcessAsync(
            ProjectDirectory project,
            string arguments,
            TimeSpan? timeout = null,
            MSBuildProcessKind msBuildProcessKind = MSBuildProcessKind.Dotnet)
        {
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
                    throw new ArgumentException("Unable to locate MSBuild.exe to run desktop tests. " +
                        "MSBuild.exe is located using state created as part of running build[cmd|sh] at the root of the repository. Run build /t:Prepare to set this up if this hasn't been done.");
                }

                processStartInfo.FileName = BuildVariables.MSBuildPath;
                processStartInfo.Arguments = arguments;
            }
            else
            {
                processStartInfo.FileName = DotNetMuxer.MuxerPathOrDefault();
                processStartInfo.Arguments = $"msbuild {arguments}";

                // Suppresses the 'Welcome to .NET Core!' output that times out tests and causes locked file issues.
                // When using dotnet we're not guarunteed to run in an environment where the dotnet.exe has had its first run experience already invoked.
                processStartInfo.EnvironmentVariables["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "true";
            }

            ProcessResult processResult;
            try
            {
                processResult = await RunProcessCoreAsync(processStartInfo, timeout);
            }
            catch (TimeoutException ex)
            {
                // Copy the binlog to the artifacts directory if executing MSBuild throws.
                // This would help diagnosing failures on the CI.
                var binaryLogFile = Path.Combine(project.ProjectFilePath, "msbuild.binlog");

                var artifactsLogDir = Assembly.GetExecutingAssembly()
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(ama => ama.Key == "ArtifactsLogDir")?.Value;

                if (!string.IsNullOrEmpty(artifactsLogDir) && File.Exists(binaryLogFile))
                {
                    var targetPath = Path.Combine(artifactsLogDir, Path.GetFileNameWithoutExtension(project.ProjectFilePath) + "." + Path.GetRandomFileName() + ".binlog");
                    File.Copy(binaryLogFile, targetPath);

                    throw new TimeoutException(ex.Message + $"{Environment.NewLine}Captured binlog at {targetPath}");
                }

                throw;
            }

            return new MSBuildResult(project, processResult.FileName, processResult.Arguments, processResult.ExitCode, processResult.Output);
        }

        internal static Task<ProcessResult> RunProcessCoreAsync(
            ProcessStartInfo processStartInfo,
            TimeSpan? timeout = null)
        {
            timeout = timeout ?? TimeSpan.FromSeconds(8 * 60);

            var process = new Process()
            {
                StartInfo = processStartInfo,
                EnableRaisingEvents = true,
            };

            var output = new StringBuilder();
            var outputLock = new object();

            var diagnostics = new StringBuilder();
            diagnostics.AppendLine("Process execution diagnostics:");

            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.OutputDataReceived += Process_OutputDataReceived;

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var timeoutTask = GetTimeoutForProcess(process, timeout, diagnostics);

            var waitTask = Task.Run(() =>
            {
                // We need to use two WaitForExit calls to ensure that all of the output/events are processed. Previously
                // this code used Process.Exited, which could result in us missing some output due to the ordering of
                // events.
                //
                // See the remarks here: https://msdn.microsoft.com/en-us/library/ty0d8k56(v=vs.110).aspx
                if (!process.WaitForExit(Int32.MaxValue))
                {
                    // unreachable - the timeoutTask will kill the process before this happens.
                    throw new TimeoutException();
                }

                process.WaitForExit();


                string outputString;
                lock (outputLock)
                {
                    // This marks the end of the diagnostic info which we collect when something goes wrong.
                    diagnostics.AppendLine("Process output:");

                    // Expected output
                    // Process execution diagnostics:
                    // ...
                    // Process output:
                    outputString = diagnostics.ToString();
                    outputString += output.ToString();
                }

                var result = new ProcessResult(process.StartInfo.FileName, process.StartInfo.Arguments, process.ExitCode, outputString);
                return result;
            });

            return Task.WhenAny<ProcessResult>(waitTask, timeoutTask).Unwrap();

            void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
            {
                lock (outputLock)
                {
                    output.AppendLine(e.Data);
                }
            }

            void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
            {
                lock (outputLock)
                {
                    output.AppendLine(e.Data);
                }
            }

            async Task<ProcessResult> GetTimeoutForProcess(Process process, TimeSpan? timeout, StringBuilder diagnostics)
            {
                await Task.Delay(timeout.Value);

                // Don't timeout during debug sessions
                while (Debugger.IsAttached)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
                if (!process.HasExited)
                {
                    // This is a timeout.
                    process.Kill();
                }

                throw new TimeoutException($"command '${process.StartInfo.FileName} {process.StartInfo.Arguments}' timed out after {timeout}. Output: {output.ToString()}");
            }
        }

        internal class ProcessResult
        {
            public ProcessResult(string fileName, string arguments, int exitCode, string output)
            {
                FileName = fileName;
                Arguments = arguments;
                ExitCode = exitCode;
                Output = output;
            }

            public string Arguments { get; }

            public string FileName { get; }

            public int ExitCode { get; }

            public string Output { get; }
        }
    }
}
