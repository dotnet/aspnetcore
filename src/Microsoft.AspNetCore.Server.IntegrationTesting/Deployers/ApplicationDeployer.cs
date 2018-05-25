// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.IntegrationTesting
{
    /// <summary>
    /// Abstract base class of all deployers with implementation of some of the common helpers.
    /// </summary>
    public abstract class ApplicationDeployer : IDisposable
    {
        public static readonly string DotnetCommandName = "dotnet";

        // This is the argument that separates the dotnet arguments for the args being passed to the
        // app being run when running dotnet run
        public static readonly string DotnetArgumentSeparator = "--";

        private readonly Stopwatch _stopwatch = new Stopwatch();

        public ApplicationDeployer(DeploymentParameters deploymentParameters, ILoggerFactory loggerFactory)
        {
            DeploymentParameters = deploymentParameters;
            LoggerFactory = loggerFactory;
            Logger = LoggerFactory.CreateLogger(GetType().FullName);

            ValidateParameters();
        }

        private void ValidateParameters()
        {
            if (DeploymentParameters.ServerType == ServerType.None)
            {
                throw new ArgumentException($"Invalid ServerType '{DeploymentParameters.ServerType}'.");
            }

            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.None && !string.IsNullOrEmpty(DeploymentParameters.TargetFramework))
            {
                DeploymentParameters.RuntimeFlavor = GetRuntimeFlavor(DeploymentParameters.TargetFramework);
            }

            if (string.IsNullOrEmpty(DeploymentParameters.ApplicationPath))
            {
                throw new ArgumentException("ApplicationPath cannot be null.");
            }

            if (!Directory.Exists(DeploymentParameters.ApplicationPath))
            {
                throw new DirectoryNotFoundException(string.Format("Application path {0} does not exist.", DeploymentParameters.ApplicationPath));
            }

            if (string.IsNullOrEmpty(DeploymentParameters.ApplicationName))
            {
                DeploymentParameters.ApplicationName = new DirectoryInfo(DeploymentParameters.ApplicationPath).Name;
            }
        }

        private RuntimeFlavor GetRuntimeFlavor(string tfm)
        {
            if (Tfm.Matches(Tfm.Net461, tfm))
            {
                return RuntimeFlavor.Clr;
            }
            return RuntimeFlavor.CoreClr;
        }

        protected DeploymentParameters DeploymentParameters { get; }

        protected ILoggerFactory LoggerFactory { get; }

        protected ILogger Logger { get; }

        public abstract Task<DeploymentResult> DeployAsync();

        protected void DotnetPublish(string publishRoot = null)
        {
            using (Logger.BeginScope("dotnet-publish"))
            {
                if (string.IsNullOrEmpty(DeploymentParameters.TargetFramework))
                {
                    throw new Exception($"A target framework must be specified in the deployment parameters for applications that require publishing before deployment");
                }

                DeploymentParameters.PublishedApplicationRootPath = publishRoot ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

                var parameters = $"publish "
                    + $" --output \"{DeploymentParameters.PublishedApplicationRootPath}\""
                    + $" --framework {DeploymentParameters.TargetFramework}"
                    + $" --configuration {DeploymentParameters.Configuration}"
                    + (DeploymentParameters.RestoreOnPublish 
                        ? string.Empty
                        : " --no-restore -p:VerifyMatchingImplicitPackageVersion=false");
                        // Set VerifyMatchingImplicitPackageVersion to disable errors when Microsoft.NETCore.App's version is overridden externally
                        // This verification doesn't matter if we are skipping restore during tests.

                if (DeploymentParameters.ApplicationType == ApplicationType.Standalone)
                {
                    parameters += $" --runtime {GetRuntimeIdentifier()}";
                }

                parameters += $" {DeploymentParameters.AdditionalPublishParameters}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = DotnetCommandName,
                    Arguments = parameters,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = DeploymentParameters.ApplicationPath,
                };

                AddEnvironmentVariablesToProcess(startInfo, DeploymentParameters.PublishEnvironmentVariables);

                var hostProcess = new Process() { StartInfo = startInfo };

                Logger.LogInformation($"Executing command {DotnetCommandName} {parameters}");

                hostProcess.StartAndCaptureOutAndErrToLogger("dotnet-publish", Logger);

                hostProcess.WaitForExit();

                if (hostProcess.ExitCode != 0)
                {
                    var message = $"{DotnetCommandName} publish exited with exit code : {hostProcess.ExitCode}";
                    Logger.LogError(message);
                    throw new Exception(message);
                }

                Logger.LogInformation($"{DotnetCommandName} publish finished with exit code : {hostProcess.ExitCode}");
            }
        }

        protected void CleanPublishedOutput()
        {
            using (Logger.BeginScope("CleanPublishedOutput"))
            {
                if (DeploymentParameters.PreservePublishedApplicationForDebugging)
                {
                    Logger.LogWarning(
                        "Skipping deleting the locally published folder as property " +
                        $"'{nameof(DeploymentParameters.PreservePublishedApplicationForDebugging)}' is set to 'true'.");
                }
                else
                {
                    RetryHelper.RetryOperation(
                        () => Directory.Delete(DeploymentParameters.PublishedApplicationRootPath, true),
                        e => Logger.LogWarning($"Failed to delete directory : {e.Message}"),
                        retryCount: 3,
                        retryDelayMilliseconds: 100);
                }
            }
        }

        protected string GetDotNetExeForArchitecture()
        {
            var executableName = DotnetCommandName;
            // We expect x64 dotnet.exe to be on the path but we have to go searching for the x86 version.
            if (DotNetCommands.IsRunningX86OnX64(DeploymentParameters.RuntimeArchitecture))
            {
                executableName = DotNetCommands.GetDotNetExecutable(DeploymentParameters.RuntimeArchitecture);
                if (!File.Exists(executableName))
                {
                    throw new Exception($"Unable to find '{executableName}'.'");
                }
            }

            return executableName;
        }

        protected void ShutDownIfAnyHostProcess(Process hostProcess)
        {
            if (hostProcess != null && !hostProcess.HasExited)
            {
                Logger.LogInformation("Attempting to cancel process {0}", hostProcess.Id);

                // Shutdown the host process.
                hostProcess.KillTree();
                if (!hostProcess.HasExited)
                {
                    Logger.LogWarning("Unable to terminate the host process with process Id '{processId}", hostProcess.Id);
                }
                else
                {
                    Logger.LogInformation("Successfully terminated host process with process Id '{processId}'", hostProcess.Id);
                }
            }
            else
            {
                Logger.LogWarning("Host process already exited or never started successfully.");
            }
        }

        protected void AddEnvironmentVariablesToProcess(ProcessStartInfo startInfo, IDictionary<string, string> environmentVariables)
        {
            var environment = startInfo.Environment;
            SetEnvironmentVariable(environment, "ASPNETCORE_ENVIRONMENT", DeploymentParameters.EnvironmentName);

            foreach (var environmentVariable in environmentVariables)
            {
                SetEnvironmentVariable(environment, environmentVariable.Key, environmentVariable.Value);
            }
        }

        protected void SetEnvironmentVariable(IDictionary<string, string> environment, string name, string value)
        {
            if (value == null)
            {
                Logger.LogInformation("Removing environment variable {name}", name);
                environment.Remove(name);
            }
            else
            {
                Logger.LogInformation("SET {name}={value}", name, value);
                environment[name] = value;
            }
        }

        protected void InvokeUserApplicationCleanup()
        {
            using (Logger.BeginScope("UserAdditionalCleanup"))
            {
                if (DeploymentParameters.UserAdditionalCleanup != null)
                {
                    // User cleanup.
                    try
                    {
                        DeploymentParameters.UserAdditionalCleanup(DeploymentParameters);
                    }
                    catch (Exception exception)
                    {
                        Logger.LogWarning("User cleanup code failed with exception : {exception}", exception.Message);
                    }
                }
            }
        }

        protected void TriggerHostShutdown(CancellationTokenSource hostShutdownSource)
        {
            Logger.LogInformation("Host process shutting down.");
            try
            {
                hostShutdownSource.Cancel();
            }
            catch (Exception)
            {
                // Suppress errors.
            }
        }

        protected void StartTimer()
        {
            Logger.LogInformation($"Deploying {DeploymentParameters.ToString()}");
            _stopwatch.Start();
        }

        protected void StopTimer()
        {
            _stopwatch.Stop();
            Logger.LogInformation("[Time]: Total time taken for this test variation '{t}' seconds", _stopwatch.Elapsed.TotalSeconds);
        }

        public abstract void Dispose();

        private string GetRuntimeIdentifier()
        {
            var architecture = DeploymentParameters.RuntimeArchitecture;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win7-" + architecture;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux-" + architecture;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "osx-" + architecture;
            }
            else
            {
                throw new InvalidOperationException("Unrecognized operation system platform");
            }
        }
    }
}
