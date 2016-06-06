// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Testing
{
    /// <summary>
    /// Abstract base class of all deployers with implementation of some of the common helpers.
    /// </summary>
    public abstract class ApplicationDeployer : IApplicationDeployer
    {
        public static readonly string DotnetCommandName = "dotnet";

        // This is the argument that separates the dotnet arguments for the args being passed to the
        // app being run when running dotnet run
        public static readonly string DotnetArgumentSeparator = "--";
        private static readonly bool IsWindows =
            RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        private readonly Stopwatch _stopwatch = new Stopwatch();

        public ApplicationDeployer(DeploymentParameters deploymentParameters, ILogger logger)
        {
            DeploymentParameters = deploymentParameters;
            Logger = logger;
        }

        protected DeploymentParameters DeploymentParameters { get; }

        protected ILogger Logger { get; }

        public abstract DeploymentResult Deploy();

        protected void DotnetPublish(string publishRoot = null)
        {
            if (string.IsNullOrEmpty(DeploymentParameters.TargetFramework))
            {
                throw new Exception($"A target framework must be specified in the deployment parameters for applications that require publishing before deployment");
            }

            DeploymentParameters.PublishedApplicationRootPath = publishRoot ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var parameters = $"publish \"{DeploymentParameters.ApplicationPath}\""
                + $" -o \"{DeploymentParameters.PublishedApplicationRootPath}\""
                + $" --framework {DeploymentParameters.TargetFramework}";

            Logger.LogInformation($"Executing command {DotnetCommandName} {parameters}");

            var startInfo = new ProcessStartInfo
            {
                FileName = DotnetCommandName,
                Arguments = parameters,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            var hostProcess = new Process() { StartInfo = startInfo };
            hostProcess.ErrorDataReceived += (sender, dataArgs) => { Logger.LogWarning(dataArgs.Data ?? string.Empty); };
            hostProcess.OutputDataReceived += (sender, dataArgs) => { Logger.LogTrace(dataArgs.Data ?? string.Empty); };
            hostProcess.Start();
            hostProcess.BeginErrorReadLine();
            hostProcess.BeginOutputReadLine();
            hostProcess.WaitForExit();

            if (hostProcess.ExitCode != 0)
            {
                throw new Exception($"{DotnetCommandName} publish exited with exit code : {hostProcess.ExitCode}");
            }

            Logger.LogInformation($"{DotnetCommandName} publish finished with exit code : {hostProcess.ExitCode}");
        }

        protected void CleanPublishedOutput()
        {
            try
            {
                // We've originally published the application in a temp folder. We need to delete it.
                Directory.Delete(DeploymentParameters.PublishedApplicationRootPath, true);
            }
            catch (Exception exception)
            {
                Logger.LogWarning($"Failed to delete directory : {exception.Message}");
            }
        }

        protected void ShutDownIfAnyHostProcess(Process hostProcess)
        {
            if (hostProcess != null && !hostProcess.HasExited)
            {
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

        protected void AddEnvironmentVariablesToProcess(ProcessStartInfo startInfo)
        {
            var environment =
#if NET451
                startInfo.EnvironmentVariables;
#else
                startInfo.Environment;
#endif

            SetEnvironmentVariable(environment, "ASPNETCORE_ENVIRONMENT", DeploymentParameters.EnvironmentName);

            foreach (var environmentVariable in DeploymentParameters.EnvironmentVariables)
            {
                SetEnvironmentVariable(environment, environmentVariable.Key, environmentVariable.Value);
            }
        }

#if NET451
        protected void SetEnvironmentVariable(System.Collections.Specialized.StringDictionary environment, string name, string value)
        {
#else
        protected void SetEnvironmentVariable(System.Collections.Generic.IDictionary<string, string> environment, string name, string value)
        {
#endif
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

        protected static string GetOSPrefix()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return "win";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return "linux";
            }
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return "darwin";
            }
            throw new InvalidOperationException("Unrecognized operating system");
        }
    }
}