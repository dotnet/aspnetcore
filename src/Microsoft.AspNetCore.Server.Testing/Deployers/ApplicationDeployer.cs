// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.AspNetCore.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

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
            PlatformServices.Default.Runtime.OperatingSystem.Equals("Windows", StringComparison.OrdinalIgnoreCase);

        private readonly Stopwatch _stopwatch = new Stopwatch();

        public ApplicationDeployer(DeploymentParameters deploymentParameters, ILogger logger)
        {
            DeploymentParameters = deploymentParameters;
            Logger = logger;
        }

        protected DeploymentParameters DeploymentParameters { get; }

        protected ILogger Logger { get; }

        protected string TargetFrameworkName { get; private set; }

        public abstract DeploymentResult Deploy();

        protected void PickRuntime()
        {
            TargetFrameworkName = DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Clr ? "dnx451" : "dnxcore50";

            Logger.LogInformation($"Pick target framework {TargetFrameworkName}");
        }

        protected void DotnetPublish(string publishRoot = null)
        {
            DeploymentParameters.PublishedApplicationRootPath = publishRoot ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var parameters = $"publish {DeploymentParameters.ApplicationPath}"
                + $" -o {DeploymentParameters.PublishedApplicationRootPath}"
                + $" --framework {TargetFrameworkName}";

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

            DeploymentParameters.ApplicationPath =
                (DeploymentParameters.ServerType == ServerType.IISExpress ||
                 DeploymentParameters.ServerType == ServerType.IIS) ?
                Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "wwwroot") :
                DeploymentParameters.ApplicationPath;

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
                KillProcess(hostProcess.Id);
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

        private void KillProcess(int processId)
        {
            if (IsWindows)
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "taskkill",
                    Arguments = $"/T /F /PID {processId}",
                };
                var killProcess = Process.Start(startInfo);
                killProcess.WaitForExit();
            }
            else
            {
                var killSubProcessStartInfo = new ProcessStartInfo
                {
                    FileName = "pkill",
                    Arguments = $"-TERM -P {processId}",
                };
                var killSubProcess = Process.Start(killSubProcessStartInfo);
                killSubProcess.WaitForExit();
                
                var killProcessStartInfo = new ProcessStartInfo
                {
                    FileName = "kill",
                    Arguments = $"-TERM {processId}",
                };
                var killProcess = Process.Start(killProcessStartInfo);
                killProcess.WaitForExit();
            }
        }

        protected void AddEnvironmentVariablesToProcess(ProcessStartInfo startInfo)
        {
            var environment =
#if NET451
                startInfo.EnvironmentVariables;
#elif NETSTANDARDAPP1_5
                startInfo.Environment;
#endif

            SetEnvironmentVariable(environment, "ASPNET_ENV", DeploymentParameters.EnvironmentName);

            foreach (var environmentVariable in DeploymentParameters.EnvironmentVariables)
            {
                SetEnvironmentVariable(environment, environmentVariable.Key, environmentVariable.Value);
            }
        }

#if NET451
        protected void SetEnvironmentVariable(System.Collections.Specialized.StringDictionary environment, string name, string value)
        {
#elif NETSTANDARDAPP1_5
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
            switch (PlatformServices.Default.Runtime.OperatingSystemPlatform)
            {
                case Platform.Linux: return "linux";
                case Platform.Darwin: return "darwin";
                case Platform.Windows: return "win";
                default:
                    throw new InvalidOperationException("Unrecognized operating system");
            }
        }
    }
}