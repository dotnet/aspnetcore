// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    public class ApplicationPublisher
    {
        public string ApplicationPath { get; }

        public ApplicationPublisher(string applicationPath)
        {
            ApplicationPath = applicationPath;
        }

        public static readonly string DotnetCommandName = "dotnet";

        public virtual Task<PublishedApplication> Publish(DeploymentParameters deploymentParameters, ILogger logger)
        {
            var publishDirectory = CreateTempDirectory();
            using (logger.BeginScope("dotnet-publish"))
            {
                if (string.IsNullOrEmpty(deploymentParameters.TargetFramework))
                {
                    throw new Exception($"A target framework must be specified in the deployment parameters for applications that require publishing before deployment");
                }

                var parameters = $"publish "
                                 + $" --output \"{publishDirectory.FullName}\""
                                 + $" --framework {deploymentParameters.TargetFramework}"
                                 + $" --configuration {deploymentParameters.Configuration}"
                                 + (deploymentParameters.RestoreOnPublish
                                     ? string.Empty
                                     : " --no-restore -p:VerifyMatchingImplicitPackageVersion=false");
                // Set VerifyMatchingImplicitPackageVersion to disable errors when Microsoft.NETCore.App's version is overridden externally
                // This verification doesn't matter if we are skipping restore during tests.

                if (deploymentParameters.ApplicationType == ApplicationType.Standalone)
                {
                    parameters += $" --runtime {GetRuntimeIdentifier(deploymentParameters)}";
                }

                parameters += $" {deploymentParameters.AdditionalPublishParameters}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = DotnetCommandName,
                    Arguments = parameters,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = deploymentParameters.ApplicationPath,
                };

                ProcessHelpers.AddEnvironmentVariablesToProcess(startInfo, deploymentParameters.PublishEnvironmentVariables, logger);

                var hostProcess = new Process() { StartInfo = startInfo };

                logger.LogInformation($"Executing command {DotnetCommandName} {parameters}");

                hostProcess.StartAndCaptureOutAndErrToLogger("dotnet-publish", logger);

                // A timeout is passed to Process.WaitForExit() for two reasons:
                //
                // 1. When process output is read asynchronously, WaitForExit() without a timeout blocks until child processes
                //    are killed, which can cause hangs due to MSBuild NodeReuse child processes started by dotnet.exe.
                //    With a timeout, WaitForExit() returns when the parent process is killed and ignores child processes.
                //    https://stackoverflow.com/a/37983587/102052
                //
                // 2. If "dotnet publish" does hang indefinitely for some reason, tests should fail fast with an error message.
                const int timeoutMinutes = 5;
                if (hostProcess.WaitForExit(milliseconds: timeoutMinutes * 60 * 1000))
                {
                    if (hostProcess.ExitCode != 0)
                    {
                        var message = $"{DotnetCommandName} publish exited with exit code : {hostProcess.ExitCode}";
                        logger.LogError(message);
                        throw new Exception(message);
                    }
                }
                else
                {
                    var message = $"{DotnetCommandName} publish failed to exit after {timeoutMinutes} minutes";
                    logger.LogError(message);
                    throw new Exception(message);
                }

                logger.LogInformation($"{DotnetCommandName} publish finished with exit code : {hostProcess.ExitCode}");
            }

            return Task.FromResult(new PublishedApplication(publishDirectory.FullName, logger));
        }

        private static string GetRuntimeIdentifier(DeploymentParameters deploymentParameters)
        {
            var architecture = deploymentParameters.RuntimeArchitecture;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win7-" + architecture;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux-" + architecture;
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx-" + architecture;
            }
            throw new InvalidOperationException("Unrecognized operation system platform");
        }

        protected static DirectoryInfo CreateTempDirectory()
        {
            var tempPath = Path.GetTempPath() + Guid.NewGuid().ToString("N");
            var target = new DirectoryInfo(tempPath);
            target.Create();
            return target;
        }
    }
}