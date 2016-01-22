// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.AspNet.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;

namespace Microsoft.AspNet.Server.Testing
{
    /// <summary>
    /// Abstract base class of all deployers with implementation of some of the common helpers.
    /// </summary>
    public abstract class ApplicationDeployer : IApplicationDeployer
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public ApplicationDeployer(DeploymentParameters deploymentParameters, ILogger logger)
        {
            DeploymentParameters = deploymentParameters;
            Logger = logger;
            DnuCommandName = TestPlatformHelper.IsWindows ? "dnu.cmd" : "dnu";
            DnxCommandName = TestPlatformHelper.IsWindows ? "dnx.exe" : "dnx";
        }

        protected DeploymentParameters DeploymentParameters { get; }

        protected ILogger Logger { get; }

        protected string DnuCommandName { get; }

        protected string DnxCommandName { get; }

        protected string TargetRuntimeName { get; private set; }

        protected string TargetRuntimeBinPath { get; private set; }

        protected string ToolingRuntimeBinPath { get; private set; }

        public abstract DeploymentResult Deploy();

        protected void PickRuntime()
        {
            var currentRuntimeBinPath = PlatformServices.Default.Runtime.RuntimePath;
            Logger.LogInformation($"Current runtime path is : {currentRuntimeBinPath}");

            var currentRuntimeFullName = new DirectoryInfo(currentRuntimeBinPath).Parent.Name;
            var currentRuntimeVersionParts = currentRuntimeFullName.Split(new char[] { '.' }, 2);
            if (currentRuntimeVersionParts.Length < 2)
            {
                throw new ArgumentNullException($"The current runtime bin path points to a runtime name doesn't indicate a version: {currentRuntimeBinPath}.");
            }
            var currentRuntimeVersion = currentRuntimeVersionParts[1];

            var runtimeHome = new DirectoryInfo(currentRuntimeBinPath).Parent.Parent.FullName;
            Logger.LogInformation($"Runtime home folder: {runtimeHome}");

            if (DeploymentParameters.RuntimeFlavor == RuntimeFlavor.Mono)
            {
                // TODO: review on mono
                TargetRuntimeName = $"dnx-mono.{currentRuntimeVersion}";
                TargetRuntimeBinPath = Path.Combine(runtimeHome, TargetRuntimeName, "bin");
                ToolingRuntimeBinPath = TargetRuntimeBinPath;
            }
            else
            {
                var flavor = DeploymentParameters.RuntimeFlavor == RuntimeFlavor.CoreClr ? "coreclr" : "clr";
                var architecture = DeploymentParameters.RuntimeArchitecture == RuntimeArchitecture.x86 ? "x86" : "x64";

                // tooling runtime will stick to coreclr so as to prevent long path issue during publishing
                ToolingRuntimeBinPath = Path.Combine(runtimeHome, $"dnx-coreclr-{GetOSPrefix()}-{architecture}.{currentRuntimeVersion}", "bin");

                TargetRuntimeName = $"dnx-{flavor}-{GetOSPrefix()}-{architecture}.{currentRuntimeVersion}";
                TargetRuntimeBinPath = Path.Combine(runtimeHome, TargetRuntimeName, "bin");
            }

            if (!Directory.Exists(ToolingRuntimeBinPath) || !Directory.Exists(TargetRuntimeBinPath))
            {
                throw new Exception($"Requested runtime '{ToolingRuntimeBinPath}' or '{TargetRuntimeBinPath}; does not exist. Please make sure it is installed before running test.");
            }

            Logger.LogInformation($"Pick target runtime {TargetRuntimeBinPath}");
            Logger.LogInformation($"Pick tooling runtime {ToolingRuntimeBinPath}");

            // Work around win7 search path issues.
            var newPath = TargetRuntimeBinPath + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH");
            DeploymentParameters.EnvironmentVariables.Add(new KeyValuePair<string, string>("PATH", newPath));
        }

        protected void DnuPublish(string publishRoot = null)
        {
            DeploymentParameters.PublishedApplicationRootPath = publishRoot ?? Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var noSource = DeploymentParameters.PublishWithNoSource ? "--no-source" : string.Empty;
            var command = DeploymentParameters.Command ?? "web";
            var parameters = $"publish {DeploymentParameters.ApplicationPath} -o {DeploymentParameters.PublishedApplicationRootPath}"
                + $" --runtime {TargetRuntimeName} {noSource} --iis-command {command}";

            var dnuPath = Path.Combine(ToolingRuntimeBinPath, DnuCommandName);
            Logger.LogInformation($"Executing command {dnuPath} {parameters}");

            var startInfo = new ProcessStartInfo
            {
                FileName = dnuPath,
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
                throw new Exception(string.Format("dnu publish exited with exit code : {0}", hostProcess.ExitCode));
            }

            DeploymentParameters.ApplicationPath =
                (DeploymentParameters.ServerType == ServerType.IISExpress ||
                 DeploymentParameters.ServerType == ServerType.IIS) ?
                Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "wwwroot") :
                Path.Combine(DeploymentParameters.PublishedApplicationRootPath, "approot", "src", new DirectoryInfo(DeploymentParameters.ApplicationPath).Name);

            Logger.LogInformation($"dnu publish finished with exit code : {hostProcess.ExitCode}");
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
                hostProcess.Kill();
                hostProcess.WaitForExit(5 * 1000);
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
#if DNX451
                startInfo.EnvironmentVariables;
#elif DNXCORE50
                startInfo.Environment;
#endif

            SetEnvironmentVariable(environment, "ASPNET_ENV", DeploymentParameters.EnvironmentName);

            // Work around for https://github.com/aspnet/dnx/issues/1515
            if (DeploymentParameters.PublishWithNoSource)
            {
                SetEnvironmentVariable(environment, "DNX_PACKAGES", null);
            }

            SetEnvironmentVariable(environment, "DNX_DEFAULT_LIB", null);

            foreach (var environmentVariable in DeploymentParameters.EnvironmentVariables)
            {
                SetEnvironmentVariable(environment, environmentVariable.Key, environmentVariable.Value);
            }
        }

#if DNX451
        protected void SetEnvironmentVariable(System.Collections.Specialized.StringDictionary environment, string name, string value)
        {
#elif DNXCORE50
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